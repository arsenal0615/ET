## 背景

自走棋 autochess 包已完成 E1-E4（基础框架、经济、商店、单位）。13 种羁绊定义（SynergyDef）和单位标签（UnitTemplateDef.Tags）数据已存在，但缺少统计、激活、效果应用的运行时逻辑。

现有架构模式：
- **静态服务类 + [FriendOf]** 处理横切逻辑（EconomyService、ShopService、RosterService 等）
- **PhaseChangedEvent + Handler** 驱动阶段逻辑
- **纯 C# 数据类 + [EnableClass]** 作为非 Entity 的值对象（UnitInfo、ShopOffer）

## 目标 / 非目标

**目标：**
- 实现羁绊统计引擎（count + level）
- PreBattle 生成 TraitSnapshot（冻结快照供 E7 战斗读取）
- Deployment 阶段实时统计（供 UI 读取）
- 静态羁绊属性加成（Brawler/Giant/Noble/Blaster/Brutalist）
- Goblin 经济羁绊（RoundStart 赠送单位）
- 校准 SynergyDef 参数与 GDD 一致

**非目标：**
- 动态羁绊运行时逻辑（Assassin 跳后排等，E7 实现）
- 战斗属性系统（BattleUnitData 结构体，E7 定义）
- 网络同步（E9 实现）

## 决策

### 决策 1: SynergyComponent 挂载位置

**选择：** ComponentOf: MatchPlayer

**考虑过的替代方案：**
- A) ComponentOf: MatchRoom（全局统一管理） → 每个玩家有独立的羁绊状态，挂 MatchRoom 需要 Dictionary<playerId, snapshot> 额外映射
- B) 不用 Component，纯临时计算 → 需要跨回合持久化 lastGoblinLevel，临时计算不够

**理由：** 羁绊是 per-player 状态，与 RosterComponent/ShopComponent 同级。挂 MatchPlayer 自然生命周期一致，查找方便。

### 决策 2: 快照数据模型 — 纯 C# 类 vs Entity

**选择：** 纯 C# 类 + [EnableClass]

**考虑过的替代方案：**
- A) Entity 子类 → TraitSnapshot 是临时数据（每回合重建），不需要 Entity 的 InstanceId/生命周期管理，用 Entity 过重

**理由：** 与 UnitInfo、ShopOffer 一致的模式。TraitSnapshot/SynergyEntry 是数据容器，引用语义，无需序列化。

### 决策 3: 静态效果应用方式 — 直接修改 UnitInfo vs 独立战斗属性

**选择：** 写入 TraitSnapshot 的 UnitBattleModifiers 字典，不修改 UnitInfo 本身

**考虑过的替代方案：**
- A) 直接修改 UnitInfo 的 Hp/Atk 等 → UnitInfo 是持久数据（跨回合存在），修改后需要回合结束时恢复，易出错
- B) 创建 BattleUnitData 结构体 → 这是 E7 的工作，本变更不应定义战斗运行时的数据结构

**理由：** TraitSnapshot 本身是战斗前的输入快照。将属性修改值（modifiers）存在快照中，E7 初始化战斗单位时读取 UnitInfo 基础值 + modifiers 合并。这样 UnitInfo 始终保持"干净"的持久状态，不需要回退逻辑。

### 决策 4: SynergyDef 扩展 — SynergyType 枚举

**选择：** 在 SynergyDef 上新增 `SynergyType` 字段（枚举: Static/Dynamic/Economic）

**考虑过的替代方案：**
- A) 硬编码 tag 名字判断类型 → 不可维护，每次加羁绊都要改代码
- B) 用 bit flag → 过度设计，3 种类型用枚举足够

**理由：** SynergyService 需要知道哪些羁绊在 PreBattle 应用静态效果、哪些只标记给 E7、哪些触发经济效果。类型信息放在配置中最自然。

### 决策 5: Goblin 跨回合状态 — SynergyComponent.LastGoblinLevel

**选择：** 在 SynergyComponent 中持久化 `LastGoblinLevel` (int)

**考虑过的替代方案：**
- A) 每回合 RoundStart 重新遍历上一回合的 TraitSnapshot → TraitSnapshot 在 PreBattle 生成，但 Goblin 需要在 RoundStart 使用，时序正确（上一回合的快照还在）。但是如果未来有其他经济羁绊需要类似机制，抽象一个"上回合效果"会更好
- B) 使用 MatchRoom 级别的 Dictionary → 挂在 SynergyComponent 更自然

**理由：** 简单直接。SynergyComponent 在 PreBattle 时更新 LastGoblinLevel = 当前 Goblin level，下回合 RoundStart 读取。

### 决策 6: SynergyDef 参数重新定义

**选择：** 保留 SynergyEffectParam 的 Param1/Param2/Param3 通用字段，但为每种 SynergyType 赋予明确语义

静态羁绊参数语义:

| 羁绊 | Param1 | Param2 | Param3 |
|------|--------|--------|--------|
| Brawler | hpMultiplier (0.5/1.0) | — | — |
| Giant | damageReduction (0.4/0.4) | — | — |
| Noble | bonusPct (0.25/0.40) | frontRowMax (1) | — |
| Blaster | distDmgPerHex (0.10/0.15) | rangeBonus (1) | — |
| Brutalist | atkSpeedBonus (0.30/0.60) | — | — |

动态羁绊参数语义（E7 读取）:

| 羁绊 | Param1 | Param2 | Param3 |
|------|--------|--------|--------|
| Ace | dmgMultiplier (0.40/0.70) | lifestealPct (0.40/0.70) | — |
| Assassin | critBonus (0.30/0.60) | jumpBackRow (1) | — |
| Clan | healTriggerHpPct (0.35/0.70) | atkSpeedBurst (0.35/0.70) | burstDuration (5.0) |
| Ranger | atkSpeedPerHit (0.10/0.15) | maxStacks (5) | — |
| P.E.K.K.A | healOnKillPct (0.60/0.60) | dmgBonus (0.40/0.40) | — |
| Superstar | manaPrecharge (0.333/0.667) | doubleCastChance (0.50/0.50) | maxDoubleCasts (4) |
| Undead | maxHpReduction (0.25/0.50) | — | — |

经济羁绊参数语义:

| 羁绊 | Param1 | Param2 | Param3 |
|------|--------|--------|--------|
| Goblin | giftCount (1/2) | — | — |

### 决策 7: Deployment 实时统计的触发方式

**选择：** SynergyService.Recalculate 在摆位操作后显式调用

**考虑过的替代方案：**
- A) 用事件（如 BoardChangedEvent）→ 需要定义新事件 + 在每个操作点发布，间接层增加
- B) 在 PhaseChangedEventHandler_Synergy 中统一处理 → 只能在阶段切换时跑一次，不能实时

**理由：** 调用点明确（PlacementService 的 3 个操作 + UnitService.Sell 棋盘单位时），直接在操作成功后调 `SynergyService.Recalculate`。购买到板凳不触发（板凳不影响统计）。合成后如果结果在棋盘上也触发。简单直接，避免事件风暴。

## 框架特定决策

### 数据模型与所有权

```
MatchPlayer
  ├── RosterComponent (已有)
  ├── ShopComponent (已有)
  ├── EconomyLogComponent (已有)
  └── SynergyComponent (新增, ComponentOf: MatchPlayer)
        ├── LiveSynergies: List<SynergyEntry>     // 实时统计
        ├── Snapshot: TraitSnapshot                 // PreBattle 冻结快照
        └── LastGoblinLevel: int                    // 跨回合持久化
```

### 新增类型清单

| 类型 | 位置 | 属性 | 说明 |
|------|------|------|------|
| `SynergyComponent` | Model/Server | `[ComponentOf(typeof(MatchPlayer))]` | Entity |
| `SynergyEntry` | Model/Server | `[EnableClass]` | 单条羁绊统计 (Tag/Count/Level/SynergyType) |
| `TraitSnapshot` | Model/Server | `[EnableClass]` | 冻结快照 |
| `UnitBattleModifiers` | Model/Server | `[EnableClass]` | 单个单位的属性修改值 |
| `SynergyType` | Model/Share | enum | Static=1, Dynamic=2, Economic=3 |
| `SynergyChangedEvent` | Model/Share | struct | 事件 (MatchPlayerId) |
| `SynergyService` | Hotfix/Server | static + `[FriendOf]` | 核心逻辑 |
| `GoblinGiftService` | Hotfix/Server | static + `[FriendOf]` | Goblin 赠送逻辑 |
| `PhaseChangedEventHandler_Synergy` | Hotfix/Server | `[Event(SceneType.Map)]` | 阶段事件处理 |

### SynergyService 方法设计

```
SynergyService (static)
  [FriendOf(typeof(SynergyComponent))]
  [FriendOf(typeof(RosterComponent))]

  // 核心统计
  + Recalculate(player) → void
    - 遍历 roster.Units where Row >= 0
    - 统计每种 tag 的 count
    - 对比 SynergyDef.Thresholds 计算 level
    - 更新 SynergyComponent.LiveSynergies
    - 对比变化，有变则发布 SynergyChangedEvent

  // 快照生成
  + GenerateSnapshot(player) → void
    - 从 LiveSynergies 生成 TraitSnapshot.ActiveSynergies
    - 遍历棋盘单位，为每个单位生成 UnitSynergyMap 条目
    - 对静态羁绊：计算 UnitBattleModifiers（HP/AtkSpeed/Range 等）
    - 对动态羁绊：写入 TraitSnapshot 标记（type=Dynamic + params）
    - 更新 LastGoblinLevel
    - 冻结到 SynergyComponent.Snapshot

  // 静态效果计算
  - ApplyStaticEffects(unit, activeSynergies, modifiers) → void
    - Brawler: modifiers.HpMultiplier *= (1 + param1)
    - Giant: modifiers.DamageReduction = max(existing, param1)
    - Noble: 根据 unit.Row 决定 modifiers.DamageReduction 或 modifiers.DamageMultiplier
    - Blaster: modifiers.RangeBonus += param2; modifiers.DistanceDamagePerHex = param1
    - Brutalist: modifiers.AtkSpeedMultiplier *= (1 - param1) // 攻击间隔缩短
```

### GoblinGiftService 方法设计

```
GoblinGiftService (static)
  [FriendOf(typeof(SynergyComponent))]
  [FriendOf(typeof(RosterComponent))]

  + TryGiveGifts(player, pool, rng, round) → int (实际赠送数)
    - 读 SynergyComponent.LastGoblinLevel
    - level == 0 → return 0
    - giftCount = SynergyDef["Goblin"].Effects[level-1].Param1
    - 循环 giftCount 次:
      - 检查板凳是否满 → 满则跳过
      - 从 Goblin 标签模板中随机选一个 (DeriveSubSeed(GoblinGift, round * 10 + i))
      - RosterService.AddToBench(player, templateId, 1, isGift=true)
    - return 实际赠送数
```

### PhaseChangedEventHandler_Synergy

```
[Event(SceneType.Map)]  // 重要：autochess 在 Map Scene 运行
PhaseChangedEventHandler_Synergy : AEvent<Scene, PhaseChangedEvent>

Handle:
  Deployment 阶段开始:
    - 对所有存活玩家 SynergyService.Recalculate
    - （此时棋盘从上回合延续，需要初始化本回合的 LiveSynergies）

  PreBattle 阶段开始:
    - 对所有存活玩家 SynergyService.GenerateSnapshot

  RoundStart 阶段开始 (Round >= 2):
    - 对所有存活玩家 GoblinGiftService.TryGiveGifts
```

### 修改现有代码的集成点

| 文件 | 修改 |
|------|------|
| `MatchRoomFactory.cs` | 创建 MatchPlayer 时添加 `SynergyComponent` |
| `PlacementService.cs` | `TryPlaceToBoard`/`TryMoveOnBoard`/`TrySwap` 成功后调 `SynergyService.Recalculate` |
| `UnitService.cs` | `Sell` 棋盘单位后调 `SynergyService.Recalculate`；`Buy` 合成后如果有棋盘变化也触发 |
| `AutoChessConfigLoader.cs` | InitSynergies 参数按决策 6 的表格校准；SynergyDef 增加 SynergyType 字段 |
| `SynergyDef.cs` | 增加 `SynergyType Type` 字段 |
| `AutoChessTestHelper.cs` | 增加羁绊系统集成测试 |

### UnitBattleModifiers 结构

```csharp
[EnableClass]
public class UnitBattleModifiers
{
    public float HpMultiplier = 1.0f;           // Brawler
    public float DamageReduction = 0f;          // Giant, Noble(前排)
    public float DamageMultiplier = 1.0f;       // Noble(后排)
    public float AtkSpeedMultiplier = 1.0f;     // Brutalist
    public int RangeBonus = 0;                  // Blaster
    public float DistanceDamagePerHex = 0f;     // Blaster
}
```

E7 战斗初始化时：`battleHp = floor(baseHp * starMultiplier * modifiers.HpMultiplier)`

### TraitSnapshot 结构

```csharp
[EnableClass]
public class TraitSnapshot
{
    public List<SynergyEntry> ActiveSynergies;                   // level > 0 的羁绊
    public Dictionary<int, List<SynergyEntry>> UnitSynergyMap;   // instId → 适用的已激活羁绊
    public Dictionary<int, UnitBattleModifiers> UnitModifiers;   // instId → 属性修改值
}
```

## 风险 / 权衡

| 风险 | 缓解措施 |
|------|----------|
| SynergyDef 参数语义不够明确（Param1/2/3 是通用名字） | 在 ConfigLoader 初始化代码中用注释标注每个参数的语义；后续可考虑类型安全的效果参数类 |
| Noble 前排/后排判定基于 Row，但棋盘布局可能变 | Row 分界线（0-1=前排，2-4=后排）定义为常量 `AutoChessDefine.FrontRowMax = 1` |
| Goblin 赠送可能触发合成（板凳上已有同模板单位） | 本变更 RoundStart 赠送不触发合成（合成仅在 Deployment 阶段），与现有 GDD 规则一致 |
| 修改 PlacementService/UnitService 增加 Recalculate 调用引入耦合 | Recalculate 是纯计算+事件发布，无副作用，调用开销极低。如果未来耦合变重再考虑事件解耦 |
