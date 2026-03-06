# cn.etetet.autochess 开发模式详解

## 已实现变更

| 变更 | 日期 | 内容 |
|------|------|------|
| a1-foundation | 2026-03-02 | Entity树、PRNG、RoundFSM、PhaseGate、ConfigLoader |
| a1-economy | 2026-03-03 | EconomyService、EconomyLogComponent、PhaseChangedEventHandler |
| a1-shop | 2026-03-03 | SharedPoolComponent、ShopComponent、ShopService、FirstRoundGiftService、PhaseChangedEventHandler_Shop |
| a1-unit | 2026-03-04 | UnitInfo、RosterService、PlacementService、MergeService、PreBattleService、UnitService（门面）、PhaseChangedEventHandler_Unit |
| a1-synergy | 2026-03-06 | SynergyComponent、SynergyService、GoblinGiftService、PhaseChangedEventHandler_Synergy、TraitSnapshot、UnitBattleModifiers |

---

## ET9 框架在 autochess 中的应用模式

### 1. 静态服务类（跨 Entity 横切逻辑）

当业务逻辑需要在多处触发（非单一 Entity 生命周期）时，用带 `[FriendOf]` 的纯静态类：

```csharp
// 正确：EconomyService 作为圣水变更单一入口
[FriendOf(typeof(MatchPlayer))]
[FriendOf(typeof(EconomyLogComponent))]
public static class EconomyService
{
    private static void ApplyDelta(...) { /* clamp + log + event */ }
}

// 错误：不要在 MatchPlayerSystem 里分散实现各类经济操作
```

适用场景：经济系统、伤害计算、状态效果应用。

---

### 2. AEvent 处理器（[Event(SceneType.Map)]）

autochess 的 PhaseChangedEvent 等 ET 事件处理器使用 AEvent 基类，**不是静态类**（ET analyzer 例外）：

```csharp
// 正确：autochess 比赛运行在 Map Scene，事件注册用 SceneType.Map
[Event(SceneType.Map)]     // ← Map，不是 Server 或 Main！
public class PhaseChangedEventHandler_Economy : AEvent<Scene, PhaseChangedEvent>
{
    protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
    {
        if (args.NewPhase != RoundPhase.RoundStart)
        {
            await ETTask.CompletedTask;
            return;
        }
        // ... 同步逻辑 ...
        await ETTask.CompletedTask;
    }
}

// 错误
[Event(SceneType.Server)]     // ← 编译通过但不生效，Map 上的事件收不到
[Event(SceneType.Main)]       // ← 编译通过但不生效
```

PhaseChangedEvent 结构（不含 Room 引用）：
```csharp
// 正确：通过 MatchRoomId 获取房间
MatchComponent matchComp = scene.GetComponent<MatchComponent>();
MatchRoom room = matchComp.GetRoom(args.MatchRoomId);  // args.MatchRoomId
int round = args.Round;                                  // args.Round，不是 room.CurrentRound
RoundPhase newPhase = args.NewPhase;

// 错误：PhaseChangedEvent 没有 Room 字段
// MatchRoom room = args.Room;   // ← 编译错误
```

---

### 3a. ET0032 — 非 Entity 的 Model 类需要 [EnableClass]

非 Entity 的普通 C# 类如果放在 Model 程序集中，必须加 `[EnableClass]` 属性：

```csharp
// 正确：ShopOffer 是 Model/Server 中的普通类，需要 [EnableClass]
namespace ET.Server
{
    [EnableClass]
    public class ShopOffer
    {
        public int TemplateId; // -1 = 空位
    }
}

// 错误：没有 [EnableClass] 会触发 ET0032 编译错误
public class ShopOffer { ... }  // ← ET0032: non-Entity class must have [EnableClass]
```

规则：Model 程序集中只有继承 Entity 的类可以直接声明；其他辅助类必须标注 `[EnableClass]`。

---

### 3b. ET0013 循环依赖 — 内联逻辑规避

当两个静态服务类互相调用时会触发 ET0013 循环依赖错误，解决方案是**内联**被调用的逻辑：

```csharp
// 场景：ShopService.TryBuy 需要判断阶段门控
// 错误（产生 ET0013 循环依赖）
bool canBuy = RoundFSMComponentSystem.CanOperate(currentPhase, OperationType.Buy);
// ShopService → RoundFSMComponentSystem → (可能调回 ShopService 的某个路径)

// 正确：内联逻辑，避免调用另一个可能形成环的静态类
bool canBuy = currentPhase == RoundPhase.Deployment ||
              currentPhase == RoundPhase.Battle;
```

同样适用于 `MatchRoomSystem.EliminatePlayer` 调用 `ShopService.ReturnOffersToPool`（也形成循环），解决方案是直接在 EliminatePlayer 中内联归还逻辑。

---

### 3c. 预扣机制（Pre-Deduction Pool Pattern）

共享卡池的标准用法：生成时立即预扣，下次生成开始时归还上次预扣：

```csharp
// GenerateOffersForPlayer 的正确顺序：
// 1. 先归还当前所有预扣（空出位置让本次抽取有更多选择）
ReturnCurrentOffers(shop, pool);

// 2. 从剩余库存（Remaining > 0）中抽取
// 3. 立即预扣（Remaining--）

// shop.ShopRollIndex 全局递增（不按回合重置），保证每次抽取使用唯一子种子
shop.ShopRollIndex += AutoChessDefine.ShopSlotCount;
```

**等效不变量**：`pool.TotalInPool + pool.TotalInOffers = TotalUnits * CopiesPerUnit`，即未出售的单位份数总和恒定。

---

### 3d. DeriveSubSeed 第二参数扩展用法

`DeriveSubSeed(PrngPurpose purpose, int index)` 的第二参数不只是 `round`，可以是任意区分值：

```csharp
// 商店刷新：以 rollIndex + slotOffset 确保每槽独立
uint sub = rng.DeriveSubSeed(PrngPurpose.ShopReroll, shop.ShopRollIndex + slot);

// 首回合礼包：以 PlayerIndex（0-3）确保同局玩家礼包各异
uint sub = rng.DeriveSubSeed(PrngPurpose.FirstGift, player.PlayerIndex);

// 经济：以 round 区分每回合
uint sub = rng.DeriveSubSeed(PrngPurpose.CommanderPassive, round);
```

---

### 4. DeriveSubSeed — 多用途 RNG 隔离

同一局比赛内不同随机用途（商店/战斗/统帅被动）应使用独立子种子，避免消费顺序影响结果：

```csharp
// 正确：子种子独立，不消耗全局 RNG 序列
uint subSeed = rng.DeriveSubSeed(PrngPurpose.CommanderPassive, round);
int bonus = (int)(subSeed % (uint)range) + min;

// 错误：直接调 rng.NextInt() 消耗全局序列
// → 当某回合战败人数不同时，后续所有随机结果都会偏移
```

`DeriveSubSeed` 内部 = `XxHash32.Hash(InitialSeed, round, (int)purpose)`，纯函数，幂等。

---

### 4. IEnumerable + yield return（GC 友好迭代）

ET 项目避免 LINQ 以减少 GC 分配：

```csharp
// 正确：yield return 延迟求值
public static IEnumerable<MatchPlayer> GetAlivePlayers(this MatchRoom self)
{
    foreach (Entity child in self.Children.Values)
        if (child is MatchPlayer player && player.IsAlive)
            yield return player;
}

// 错误：ToList() 每帧分配堆内存
// public static List<MatchPlayer> GetAlivePlayers(...)
//     => self.Children.Values.OfType<MatchPlayer>().Where(p => p.IsAlive).ToList();
```

---

### 5. ApplyDelta 三联动模式

经济/战斗等数值变更需要原子性执行 clamp + 日志 + 事件：

```csharp
private static void ApplyDelta(MatchPlayer player, ..., int amount, int round)
{
    int oldValue = player.Elixir;
    player.Elixir = Math.Clamp(player.Elixir + amount, 0, AutoChessDefine.MaxElixir);
    int actualAmount = player.Elixir - oldValue;  // 含截断后的真实值

    // 1. 记录日志（用 actualAmount，非 amount）
    player.GetComponent<EconomyLogComponent>()?.AddDelta(...);

    // 2. 只在值真正变化时发布事件（failed buy 不触发）
    if (player.Elixir != oldValue)
        EventSystem.Instance.Publish(player.Scene(), new ElixirChangedEvent {...});
}
```

关键点：日志记录 `actualAmount`（clamp 后），事件只在值变时发布。

---

### 6. LastRoundLosers 跨变更解耦模式

避免提前实现未来依赖，用 List<long> 承载跨变更的数据流：

```
MatchRoom.LastRoundLosers (List<long>)
  ← E5（战斗结算）写入：Clear() + Add(loserId)
  → E2（经济Handler）读取：发放统帅被动
  → Round 1 为空 → 自动跳过被动（首回合无战斗）
```

---

### 7. Modifiers 快照模式（不修改持久数据）

羁绊效果不直接修改 UnitInfo，而是写入 TraitSnapshot 的 UnitBattleModifiers：

```csharp
// 正确：效果写入独立快照，UnitInfo 保持"干净"
TraitSnapshot snapshot = new TraitSnapshot();
UnitBattleModifiers mods = new UnitBattleModifiers(); // 默认值全为 1.0f/0
ApplyStaticEffects(unit, unitSynergies, mods);
snapshot.UnitModifiers[unit.InstId] = mods;

// E7 战斗初始化时合并：battleHp = baseHp * starMultiplier * mods.HpMultiplier
// 回合结束不需要任何回退逻辑 — snapshot 整体丢弃

// 错误：直接修改 UnitInfo
// unit.Hp = (int)(unit.Hp * 1.5f);  // ← 跨回合持久化，需要记录原值并恢复
```

---

### 8. EntitySystemOf 配对规则

声明了 IAwake/IDestroy/IDeserialize 的 Entity **必须** 有对应的 `[EntitySystemOf]` System 类：

```csharp
// Model/Server — Entity 声明
[ComponentOf(typeof(MatchPlayer))]
public class SynergyComponent : Entity, IAwake, IDestroy
{
    public List<SynergyEntry> LiveSynergies;
    // ...
}

// Hotfix/Server — 必须有此文件！否则 Awake/Destroy 静默不触发
[EntitySystemOf(typeof(SynergyComponent))]
[FriendOf(typeof(SynergyComponent))]
public static partial class SynergyComponentSystem
{
    [EntitySystem]
    private static void Awake(this SynergyComponent self) { self.LiveSynergies = new List<SynergyEntry>(); }

    [EntitySystem]
    private static void Destroy(this SynergyComponent self) { self.LiveSynergies = null; }
}

// 危险：编译不报错，运行时 Awake 不触发 → LiveSynergies 为 null → NullReferenceException
```

---

## 集成测试模式（AutoChessTestHelper）

所有 A1 功能通过 `AutoChessTestHelper.RunAllTests(scene)` 集成验证：

```
TestConfigLoader    → 24 units, 13 synergies, 16 skills
TestPrngDeterminism → 100 次相同 seed 结果一致 + subSeed 派生
TestEntityTree      → 结构验证 + 淘汰 + 排名
TestPhaseGate       → 操作门控矩阵（Deployment/Battle/其他阶段）
TestEconomy         → 5 个收支方法 + clamp + log + LastRoundLosers
TestShop            → 卡池初始化 / Offer生成 / 预扣 / 购买 / 出售 / 礼包 / 淘汰回收（10个断言）
TestSynergyCounting → 标签统计 + level 计算 + 棋盘变化实时更新
TestSynergySnapshot → 快照生成 + ActiveSynergies + UnitModifiers（Brawler HpMultiplier=1.5f）
TestGoblinGift      → LastGoblinLevel 读取 + 赠送 + isGift=true + Goblin 标签验证
```

触发方式：服务端启动后手动调用（无自动运行机制）。
