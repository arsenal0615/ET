## Context

a1-foundation 建立了 Entity 树（MatchRoom → MatchPlayer）、PRNG（xoshiro256** + xxHash32）、RoundFSM（5 阶段循环 + PhaseGate）。a1-economy 实现了 EconomyService（圣水统一入口，含 GiveMerge）。a1-shop 实现了 SharedPoolComponent（24 种 × 8 份）和 ShopService（Offer 生成/购买/出售/回收）。

当前状态：
- ShopService.TryBuy 返回 templateId，不创建任何单位实例
- ShopService.TrySell 接收 templateId + starLevel + isGift，不查找实际单位
- MatchRoomSystem.EliminatePlayer 只回收 Shop Offer，没有回收棋盘/板凳单位
- HexCoord 已存在：col/row + IsValid() + IsBench(row==-1) + Distance()
- AutoChessDefine 已有：BoardWidth=8, BoardHeight=5, BenchSize=5, MergeCount=2, MaxStarLevel=3, PopCapByRound[], MergeElixirReturn=1
- OperationType 已有：Buy=1, Sell=2, Place=3, Swap=4, Merge=5（PhaseGate 已配置）

## Goals / Non-Goals

**Goals:**
- 实现单位实例数据模型（UnitInfo），追踪 templateId/star/isGift/位置
- 实现 Roster（棋盘 + 板凳）格位管理和人口统计
- 实现摆位操作（Place/Move/Swap）含阶段门控
- 实现确定性自动合成（2 合 1 升星 + 连锁 + 圣水返还）
- 实现 PreBattle 校验与自动修正（超人口下板凳、板凳满强制出售）
- 集成 ShopService：购买时创建 UnitInfo 入板凳，出售时移除 UnitInfo
- 淘汰时回收所有棋盘+板凳单位到卡池
- 通过 AutoChessTestHelper 集成验证

**Non-Goals:**
- 战斗中的单位行为/移动/攻击（E7）
- 羁绊统计和效果（E5，但 RosterService 提供 GetBoardUnits 查询接口）
- 网络消息处理（E9）
- 客户端 UI（E10）
- 首回合赠送的 UnitInfo 创建（E3 的 FirstRoundGiftService 已选好 templateId，本 change 在 PhaseChangedEventHandler_Unit 中实际创建 UnitInfo）

---

## Decisions

### Decision 1: UnitInfo 为纯 C# 类（非 Entity）

**Choice:**
```csharp
[EnableClass]
public class UnitInfo
{
    public int InstId;       // 自增 ID（per-player 唯一，用于确定性排序）
    public int TemplateId;   // 单位模板 ID（1-24）
    public int Star;         // 星级（1/2/3）
    public bool IsGift;      // 是否礼品（出售/淘汰不回流卡池）
    public int Col;          // 列坐标（棋盘 0-7，板凳 0-4）
    public int Row;          // 行坐标（棋盘 0-4，板凳 -1）
}
```

**Alternatives considered:**
- A) ChildOf(RosterComponent) Entity → 每个单位都是子 Entity，频繁创建/销毁时对象池压力大（合成时消耗+创建），且排序/扫描需要遍历 Children Dictionary
- B) struct → 需要存在数组/List 中，合成时就地修改需要按索引操作，引用传递不方便

**Rationale:** UnitInfo 是逻辑层数据载体，不需要 Component/System 的生命周期。class 引用语义便于就地修改（合成时 star++ 直接生效）。与 ShopOffer 同模式。`[EnableClass]` 标注满足 ET 分析器要求。

---

### Decision 2: RosterComponent 的内部存储

**Choice:** `List<UnitInfo> Units`（平坦列表）+ `int NextInstId` 自增计数器

```csharp
[ComponentOf(typeof(MatchPlayer))]
public class RosterComponent : Entity, IAwake, IDestroy
{
    public List<UnitInfo> Units;   // 所有单位（棋盘+板凳）
    public int NextInstId;         // 自增，下一个 UnitInfo.InstId
}
```

**Alternatives considered:**
- A) 二维数组 `UnitInfo[8,5]` + `UnitInfo[5]` → 稀疏数组浪费内存，且遍历/排序不直观
- B) Dictionary<(int,int), UnitInfo> → 哈希开销不值得（最多 8+5=13 个单位）
- C) 分两个 Component（BoardComponent + BenchComponent）→ 合成扫描需要跨两个组件，不便

**Rationale:** 平坦列表最简单。单位数量极小（棋盘最多 8 个 + 板凳 5 个 = 13），线性扫描性能足够。所有查询（按位置、按 templateId、按星级）都是 O(n) 遍历，n<=13。通过 Col/Row 区分棋盘（Row>=0）和板凳（Row==-1）。

---

### Decision 3: InstId 生成策略

**Choice:** per-player 自增整数（`RosterComponent.NextInstId++`），从 1 开始

**Alternatives considered:**
- A) 全局 MatchRoom 级计数器 → 跨玩家 InstId 不够直观，且不需要全局唯一（单位始终属于某个玩家）
- B) 用 `matchSeed-round-spawnIndex` 公式 → 过于复杂，InstId 只用于同一玩家内的排序 tiebreaker

**Rationale:** InstId 的唯一用途是合成扫描时的确定性 tiebreaker。per-player 自增最简单，保证同一玩家内单位有确定的创建顺序。首回合赠送的 UnitInfo 也走 NextInstId++，不影响确定性（赠送发生在 RoundStart，购买在 Deployment 之后）。

---

### Decision 4: 合成扫描的确定性顺序

**Choice:** 按 GDD 规格实现：低星先扫 → templateId 升序 → 棋盘优先板凳 → Row 升序 → Col 升序 → InstId 升序

```
排序 key: (star ASC, templateId ASC, locationPriority ASC [board=0, bench=1], row ASC, col ASC, instId ASC)
```

扫描算法：
1. 将所有 Units 按排序 key 排序
2. 从头开始找第一对 (templateId 相同 && star 相同) 的连续两个
3. 合并：primary（第一个）保留，star++，hp=maxHp；secondary 消耗移除
4. 给 primary 玩家 +1 圣水（EconomyService.GiveMerge）
5. 从 1★ 重新扫描直到一轮无合并（连锁合成）

**Rationale:** 完全遵循 GDD 规格。"低星先扫"保证 1★→2★ 先发生，使得后续 2★→3★ 的连锁可以触发。"从 1★ 重新扫描"确保连锁不遗漏。

---

### Decision 5: 合成不改变卡池库存

**Choice:** 合成过程中 SharedPoolComponent.Remaining 不变。

**Rationale:** GDD 明确规定"合成不改变卡池库存"。这是因为星级与拷贝数守恒（1★=1份, 2★=2份, 3★=4份），出售时按 `1 << (star-1)` 回流。合成只是将 2 个 1★"合并"为 1 个 2★，总拷贝数不变（都是 2 份）。

---

### Decision 6: PreBattle 校验与修正策略

**Choice:** PreBattle 分两步：
1. **进入 Deployment 时**：触发合成链（处理 Battle 阶段买入的重复单位）
2. **进入 PreBattle 时**：执行超人口校验 + 修正

超人口修正规则（按优先级保留）：
```
排序 key（保留优先级，降序）: (star DESC, cost DESC, locationPriority ASC [board=0], row ASC, col ASC, instId ASC)
```
- 超出 popCap 的单位按反向优先级（保留优先级最低的）移到板凳
- 如果板凳满，最低优先级单位强制出售（返还圣水 + 回流卡池）

**Rationale:** 两步分离保证 Deployment 开始时先合成（可能降低 popUsed），再 PreBattle 时校验最终状态。保留优先级尊重"高星/高费值更高"的直觉。

---

### Decision 7: 购买流程集成（修改 ShopService）

**Choice:** **不修改 ShopService**。在新增的 `UnitService`（总调度静态类）中组合调用。

```
UnitService.Buy(player, slotIndex, pool, rng, phase, round):
  1. templateId = ShopService.TryBuy(player, slotIndex, pool, rng, phase, round)
  2. if templateId <= 0: return false
  3. RosterService.AddToBench(player, templateId, star=1, isGift=false)
  4. if phase == Deployment: MergeService.RunMergeChain(player, round)
  5. return true
```

**Alternatives considered:**
- A) 修改 ShopService.TryBuy 直接调用 RosterService → ShopService 和 RosterService 产生耦合，违反 E3/E4 分层
- B) 让调用方（E9 Handler）组合调用 → Handler 会膨胀，且测试也需要手动组合

**Rationale:** UnitService 作为"门面"统一协调 ShopService + RosterService + MergeService，保持各服务类职责单一。E9 的 Handler 只需要调用 UnitService 一个入口。

---

### Decision 8: 出售流程集成

**Choice:** 同样由 `UnitService` 协调。

```
UnitService.Sell(player, unitInfo, pool, round):
  1. RosterService.Remove(player, unitInfo)
  2. ShopService.TrySell(player, unitInfo.TemplateId, unitInfo.Star, unitInfo.IsGift, pool, round)
```

战斗阶段出售限制：只允许出售板凳单位（Row == -1）。由 UnitService 在调用前检查。

---

### Decision 9: 淘汰回收集成

**Choice:** 扩展 MatchRoomSystem.EliminatePlayer，在现有 Shop Offer 回收之后，增加 Roster 单位回收。

```
EliminatePlayer(room, playerId):
  ... 现有 IsAlive=false, Rank 赋值 ...
  ... 现有 Shop Offer 回收 ...
  ++ RosterService.RecoverAllToPool(player, pool)  // 新增：遍历所有 Units，按星级回流卡池（isGift 不回流），清空列表
```

**Rationale:** 保持淘汰逻辑集中在 EliminatePlayer。内联调用避免事件间接层。

---

### Decision 10: 首回合赠送的 UnitInfo 创建

**Choice:** 在 PhaseChangedEventHandler_Unit 订阅 RoundStart，Round 1 时读取 `ShopComponent.FirstRoundGiftTemplateId`，创建 UnitInfo（isGift=true）入板凳。

**Rationale:** FirstRoundGiftService 已在 PhaseChangedEventHandler_Shop 中选好 templateId 并写入 ShopComponent。Handler_Unit 在 Handler_Shop 之后执行（同一事件，ET 不保证顺序——因此改用 PhaseChangedEventHandler_Shop 末尾直接调用 RosterService，避免顺序依赖）。

**修正：** 不新增 PhaseChangedEventHandler_Unit 处理首回合赠送。改为在 PhaseChangedEventHandler_Shop 末尾直接调用 `RosterService.AddToBench(player, giftTemplateId, star=1, isGift=true)`。这消除了事件处理器之间的顺序依赖问题。

PhaseChangedEventHandler_Unit 仅处理 **Deployment 阶段合成触发** 和 **PreBattle 阶段校验触发**。

---

## Framework-Specific Decisions

### 数据模型

| 类型 | 归属 | 位置 |
|------|------|------|
| `UnitInfo` | 纯 C# 类 [EnableClass] | Model/Server |
| `RosterComponent` | ComponentOf: MatchPlayer | Model/Server |
| `RosterService` | 静态服务类 + [FriendOf] | Hotfix/Server |
| `PlacementService` | 静态服务类 + [FriendOf] | Hotfix/Server |
| `MergeService` | 静态服务类 + [FriendOf] | Hotfix/Server |
| `PreBattleService` | 静态服务类 + [FriendOf] | Hotfix/Server |
| `UnitService` | 静态门面类 + [FriendOf] | Hotfix/Server |
| `PhaseChangedEventHandler_Unit` | AEvent [Event(SceneType.Map)] | Hotfix/Server |

### 关键 [FriendOf] 声明

```csharp
[FriendOf(typeof(RosterComponent))]
public static class RosterService { ... }

[FriendOf(typeof(RosterComponent))]
public static class PlacementService { ... }

[FriendOf(typeof(RosterComponent))]
public static class MergeService { ... }

[FriendOf(typeof(RosterComponent))]
public static class PreBattleService { ... }

// UnitService 作为门面，通过公开方法调用各 Service，不需要额外 FriendOf
// 但需要 FriendOf(ShopComponent) 来读取 FirstRoundGiftTemplateId
[FriendOf(typeof(RosterComponent))]
[FriendOf(typeof(ShopComponent))]
public static class UnitService { ... }
```

### RosterService 核心 API

```csharp
// 添加单位到板凳（自动选最小空位）
public static UnitInfo AddToBench(MatchPlayer player, int templateId, int star, bool isGift)

// 移除单位
public static void Remove(MatchPlayer player, UnitInfo unit)

// 查找
public static UnitInfo FindAt(MatchPlayer player, int col, int row)
public static UnitInfo FindByInstId(MatchPlayer player, int instId)
public static List<UnitInfo> GetBoardUnits(MatchPlayer player)     // Row >= 0
public static List<UnitInfo> GetBenchUnits(MatchPlayer player)     // Row == -1

// 统计
public static int GetPopUsed(MatchPlayer player)    // 棋盘上单位数
public static int GetBenchUsed(MatchPlayer player)  // 板凳上单位数

// 淘汰回收
public static void RecoverAllToPool(MatchPlayer player, SharedPoolComponent pool)
```

### PlacementService 核心 API

```csharp
// 板凳→棋盘（检查人口上限 + 目标格空）
public static bool TryPlaceToBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow, int popCap)

// 棋盘内移动（检查目标格空）
public static bool TryMoveOnBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow)

// 任意两位置互换（棋盘↔棋盘、棋盘↔板凳、板凳↔板凳）
public static bool TrySwap(MatchPlayer player, int unitInstId1, int unitInstId2)
```

### MergeService 核心 API

```csharp
// 执行一次完整的合成链（从 1★ 扫描到稳定）
public static int RunMergeChain(MatchPlayer player, int round)  // 返回合成次数
```

### PreBattleService 核心 API

```csharp
// PreBattle 校验 + 自动修正
public static void ValidateAndFix(MatchPlayer player, SharedPoolComponent pool, int popCap, int round)
```

### 事件处理时序

```
RoundStart:
  PhaseChangedEventHandler_Economy → 发放收入 + 统帅被动
  PhaseChangedEventHandler_Shop → 刷新商店 + 首回合赠送入板凳（调用 RosterService）

Deployment:
  PhaseChangedEventHandler_Unit → 触发 MergeService.RunMergeChain（处理上回合 Battle 阶段买入）

PreBattle:
  PhaseChangedEventHandler_Unit → 触发 PreBattleService.ValidateAndFix

Battle:
  （无 Unit 事件处理，E7 接管）
```

### MatchRoomFactory 扩展

```csharp
// 在每个 MatchPlayer 创建循环中添加：
player.AddComponent<RosterComponent>();
```

### PopCap 来源

使用已有的 `MatchRoomSystem.GetPopCap()`（基于 `AutoChessDefine.PopCapByRound[round-1]`），每次需要时从 MatchRoom 读取。MatchPlayer.PopCap 字段保留但不再作为权威来源——改为由 GetPopCap() 动态计算。

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 合成连锁可能导致无限循环 | MaxStarLevel=3 限制了最大星级；每次合成至少消耗 1 个单位，Units 数量单调递减 |
| PhaseChangedEvent 处理器执行顺序不保证 | 首回合赠送改为 Handler_Shop 直接调用 RosterService，不依赖 Handler_Unit 执行顺序 |
| PreBattle 强制出售可能让玩家意外丢失高价值单位 | 优先级排序保留高星/高费单位，出售最低价值的 |
| UnitService 门面类增加间接层 | 间接层很薄（只是组合调用），换来各 Service 职责清晰 |
| List<UnitInfo> 线性扫描性能 | n<=13，O(n) 等于 O(1)，不需要优化 |
| EliminatePlayer 扩展时需要额外 [FriendOf(RosterComponent)] | 改为调用 RosterService.RecoverAllToPool（公开方法），不需要新增 FriendOf |
