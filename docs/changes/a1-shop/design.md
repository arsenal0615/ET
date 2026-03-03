## Context

a1-foundation 已建立 Entity 树（MatchRoom/MatchPlayer/DeterministicRngComponent/RoundFSMComponent）和 RoundPhase 流转机制。a1-economy 已实现 EconomyService（圣水变更单一入口）和 PrngPurpose 枚举（ShopReroll=1, FirstGift=6 均已预留）。

当前 AutoChessConfigLoader 定义 24 个 UnitTemplateDef（IDs 1–24），费用分布：
- 2 费：IDs 1–7（7 种）
- 3 费：IDs 8–14（7 种）
- 4 费：IDs 15–20（6 种）
- 5 费：IDs 21–24（4 种）

## Goals / Non-Goals

**Goals:**
- 实现共享卡池（SharedPool），跟踪 24 种单位的全局库存（含预扣机制）
- 实现商店（Shop），3 槽位 Offer 生成、购买触发、全行重刷
- 实现首回合赠送（FirstRoundGift），确定性选取 2 费单位，不扣卡池
- 与现有 EconomyService 集成（买/卖的圣水变化）
- 通过 AutoChessTestHelper 集成验证

**Non-Goals:**
- 实际 UnitInstance 创建（E4 单位系统负责）
- 网络消息处理（E9 负责）
- 板凳/棋盘位置管理（E4 负责）
- 出售回流时 `copiesOfStar` 的实际单位星级查询（由 E4 或 E9 传入 star 参数）

---

## Decisions

### Decision 1: SharedPoolComponent 位置

**Choice:** `ComponentOf(MatchRoom)`，字段 `int[] Remaining`（length=24，按 templateId-1 索引）

**Alternatives considered:**
- A) ChildOf MatchRoom → 需要 Entity 生命周期管理，但池是值语义，没必要做独立 Entity
- B) 全局单例组件 → 无法支持多房间并发（MVP 阶段可能有多局测试）

**Rationale:** 池是 MatchRoom 的一个状态，ComponentOf 是正确语义；按 (templateId-1) 索引数组访问是 O(1)。

---

### Decision 2: ShopOffer 为纯 C# 类（非 Entity）

**Choice:**
```csharp
public class ShopOffer
{
    public int TemplateId; // -1 表示空位
}
```

**Alternatives considered:**
- A) ChildOf ShopComponent Entity → 每个 Offer 都是子 Entity，生命周期复杂
- B) struct → 赋值语义，存数组时刷新操作需特别处理

**Rationale:** ShopOffer 是槽位的瞬时状态，3 个固定槽，无需 Entity 生命周期；class 引用语义便于就地修改。

`isGift` 属于 UnitInstance 属性（E4 责任），不放在 ShopOffer 中。TrySell 接收 `bool isGift` 参数，由调用方（E9 Handler 或测试）提供。

---

### Decision 3: shopRollIndex 全局递增（跨回合不重置）

**Choice:** `ShopComponent.ShopRollIndex` 每次 3 槽生成后 +3，全场景生命周期内单调递增

**Alternatives considered:**
- A) 按回合重置 → 不同回合同一位置可能使用相同 rollIndex，导致出货顺序相同
- B) 使用 MatchRoom 全局计数器 → 多玩家共用计数器，影响彼此 PRNG 序列

**Rationale:** `DeriveSubSeed(PrngPurpose.ShopReroll, rollIndex)` 是纯函数（XxHash32），全局递增确保每次刷新的子种子唯一。每位玩家独立的 `ShopComponent.ShopRollIndex` 不互相影响。

---

### Decision 4: 购买后全行 3 槽重刷

**Choice:** TryBuy 成功后，先归还当前所有 Offer（预扣）到池，再重新生成 3 个 Offer

**Alternatives considered:**
- A) 只清空被购买的槽 → 不符合产品规格（"整行 3 槽重随"）

**Rationale:** 整行重刷使博弈感更强（买一个后其他位置也变化）。归还已有 Offer 再重新生成，确保池的一致性。

---

### Decision 5: MatchPlayer.PlayerIndex 字段（新增）

**Choice:** MatchPlayer 新增 `int PlayerIndex`（0-3，工厂创建时按序号赋值），用于 FirstRoundGift 的 per-player 确定性

```
rng.DeriveSubSeed(PrngPurpose.FirstGift, player.PlayerIndex)
```

**Alternatives considered:**
- A) 所有玩家相同礼包 → 简化测试，但规格要求"随机赠送"暗示 per-player 差异
- B) 用 PlayerId 为子种子参数 → PlayerId 可能超过 int 范围或过大，导致哈希分布不理想

**Rationale:** PlayerIndex 是 0-3 的小整数，每局固定，确保礼包选取确定性且各玩家不同。

---

### Decision 6: EliminatePlayer 时回收 Shop Offer

**Choice:** 在 MatchRoomSystem.EliminatePlayer 末尾调用 `ShopService.ReturnOffersToPool(player, pool)`

**Alternatives considered:**
- A) 另建 Event 监听淘汰事件 → 多一层间接调用，不必要
- B) 不回收 → 卡池库存泄漏，单测会检测到问题

**Rationale:** 直接调用比事件监听更简单直接；ShopService 有 [FriendOf(typeof(ShopComponent))]，访问合法。

---

## Framework-Specific Decisions

### 数据模型

| 类型 | 归属 | 位置 |
|------|------|------|
| `SharedPoolComponent` | ComponentOf: MatchRoom | Model/Server |
| `ShopOffer` | 纯 C# 类（非 Entity） | Model/Server |
| `ShopComponent` | ComponentOf: MatchPlayer | Model/Server |
| `ShopService` | 静态服务类 + [FriendOf] | Hotfix/Server |
| `PhaseChangedEventHandler_Shop` | AEvent (Server) | Hotfix/Server |
| `FirstRoundGiftService` | 静态服务类 + [FriendOf] | Hotfix/Server |

### 关键 [FriendOf] 声明

```csharp
[FriendOf(typeof(ShopComponent))]
[FriendOf(typeof(SharedPoolComponent))]
[FriendOf(typeof(MatchPlayer))]
public static class ShopService { ... }

[FriendOf(typeof(ShopComponent))]
[FriendOf(typeof(SharedPoolComponent))]
[FriendOf(typeof(MatchPlayer))]
public static class FirstRoundGiftService { ... }
```

### Offer 生成算法（确定性选 3 不重复）

```
candidates = [templateId | Remaining[templateId-1] > 0]
for i in 0..2:
    if candidates is empty: Slots[i].TemplateId = -1; continue
    uint sub = rng.DeriveSubSeed(PrngPurpose.ShopReroll, player.ShopRollIndex + i)
    int idx = (int)(sub % candidates.Count)
    Slots[i].TemplateId = candidates[idx]
    Remaining[candidates[idx] - 1] -= 1   // 预扣
    candidates.RemoveAt(idx)
ShopRollIndex += 3
```

### 回合开始时商店刷新顺序

```
RoundStart Event → PhaseChangedEventHandler_Shop:
  foreach alive player:
    1. ReturnCurrentOffersToPool(player, pool)  // 归还上回合预扣
    2. GenerateOffersForPlayer(player, pool, rng)  // 生成新 Offer + 预扣

Round 1 额外：
  foreach alive player:
    3. SelectFirstGiftTemplate(player, rng)  // 返回 templateId（E4 创建 UnitInstance）
```

### MatchPlayer.PlayerIndex 新增字段

在 MatchRoomFactory.CreateMatch 中：
```csharp
for (int i = 0; i < playerIds.Count; i++)
{
    MatchPlayer player = room.AddChild<MatchPlayer, long>(playerIds[i]);
    player.PlayerIndex = i;
    ...
}
```

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 候选模板数 < 3（池枯竭）| GenerateOffersForPlayer 在候选不足时生成空槽（-1），不报错 |
| 归还 Offer 时模板已被其他玩家购买 | 无关，各玩家 Offer 互相独立；归还只影响 Remaining 计数 |
| E4 单位系统尚未完成 | ShopService.TryBuy 返回 templateId，不创建 Entity；集成测试通过池/圣水状态验证，不依赖 UnitInstance |
| copiesOfStar 由调用方传入 | MVP 阶段 TrySell 接收 `int starLevel` 参数；测试覆盖 1★/2★/3★ 三种情况 |
