## 背景

自走棋 E1-E6 已完成所有构筑阶段系统。E7 战斗模拟器和 E8 回合结算是 MVP 关键路径上的最后核心后端模块。

已有基础设施：
- **CombatUnitState** — 战斗单位运行时状态（完整字段）
- **HexUtil** — 六边形距离/邻居/坐标转换
- **SkillExecutor** — Trigger→Target→Effect 管线门面
- **ManaService** — 法力累积/消耗
- **TraitSnapshot + UnitBattleModifiers** — 羁绊静态效果预计算
- **DeterministicRngComponent** — 确定性 PRNG
- **RoundFSMComponent** — 回合状态机（Battle 阶段触发战斗）

## 目标 / 非目标

**目标：**
- 实现完整的确定性战斗 Tick 循环
- 集成已有技能引擎，不重复实现
- 生成可回放的 CombatEvent 事件流
- 实现配对、扣血、淘汰完整结算

**非目标：**
- 网络协议和消息定义（E9）
- 客户端战斗播放和 UI（E10）
- 性能优化和对象池（tech-debt）

## 决策

### TD-1: CombatSimulator 作为纯静态服务类

**选择：** CombatSimulator 为 Hotfix/Server 的纯静态类，不持有 Entity 状态。

**考虑过的替代方案：**
- A) Entity + Component 模式（CombatComponent） → 战斗是短暂的一次性计算，不需要生命周期管理，Entity 开销不值得
- B) 实例类 → 需要创建/销毁实例，静态类与现有 Service 模式一致

**理由：** 与 EconomyService/ShopService/SynergyService 等已有服务类模式一致。战斗输入（CombatUnitState[]）输出（CombatResult），无需中间状态持久化。

### TD-2: CombatEvent 用纯 C# 数据类

**选择：** CombatEvent 为 Model/Share 的纯 C# 类（`[EnableClass]`），含 EventType 枚举 + 通用字段。

**考虑过的替代方案：**
- A) 每种事件一个类（EvSpawn/EvDamage/EvMove...） → 12 种事件 12 个类文件膨胀，序列化复杂
- B) struct → 需要包含 List 字段（EffectResults），struct 不适合

**理由：** 单一 CombatEvent 类用 EventType 枚举区分，通用字段覆盖所有事件类型。客户端按 EventType 读取对应字段，简洁高效。

### TD-3: CombatResult 封装战斗输出

**选择：** 新建 CombatResult 纯 C# 类，包含 Winner/TimeUp/Events/AliveEffective 等。

**理由：** 战斗模拟是纯函数：输入（双方 CombatUnitState[]）→ 输出（CombatResult）。CombatResult 封装所有输出，供结算和网络层使用。

### TD-4: PairingService 独立于 CombatSimulator

**选择：** PairingService 为独立静态服务类，职责是配对+Ghost 选取。CombatSimulator 只负责单场战斗模拟。

**考虑过的替代方案：**
- A) 合并到 CombatSimulator → 违反单一职责，配对逻辑与 Tick 循环无关

**理由：** 配对 → 模拟 → 结算是三个独立步骤，由 PhaseChangedEventHandler_Battle 编排调用。

### TD-5: SettlementService 处理扣血和淘汰

**选择：** SettlementService 为独立静态服务类，接收 CombatResult 列表，执行扣血和淘汰。

**理由：** 结算需要全局视角（同回合多场战斗的结果），不适合放在单场战斗内部。

### TD-6: 动态羁绊集成到 CombatSimulator 内部

**选择：** 动态羁绊效果（Ace/Clan/P.E.K.K.A/Ranger/Undead）在 CombatSimulator 的 Tick 循环中内联处理，不抽取到独立 Service。

**考虑过的替代方案：**
- A) DynamicTraitService 独立类 → 需要传递大量战斗上下文，接口复杂
- B) 扩展 EffectApplier → 这些是羁绊效果不是技能效果，混入会污染 E6

**理由：** 动态羁绊与战斗流程深度耦合（攻击时叠加、击杀时触发、受伤时检测）。内联处理保持 Tick 循环的完整性和可读性。独立辅助方法但不跨类。

### TD-7: Ghost 快照存储在 MatchRoom

**选择：** MatchRoom 新增 GhostSnapshot 字段（List<UnitInfo> + TraitSnapshot），在玩家淘汰时记录。

**理由：** Ghost 数据生命周期绑定 MatchRoom，不需要独立 Entity。淘汰时写入，配对时读取。

### TD-8: CombatStartProcessor 处理开战特殊效果

**选择：** CombatStartProcessor 为独立静态类，处理 tick=0 的刺客跳后排和亡灵诅咒。

**理由：** 开战处理逻辑复杂（刺客逐个跳+落点搜索+亡灵诅咒目标选取），内联到 CombatSimulator 会使主循环过于臃肿。独立类保持清晰分工。

## 框架特定决策

### 新增类型清单

| 类型 | 位置 | 性质 |
|------|------|------|
| CombatEvent | Model/Share | 纯 C# 类 `[EnableClass]` |
| CombatEventType | Model/Share | enum |
| CombatResult | Model/Share | 纯 C# 类 `[EnableClass]` |
| PairingResult | Model/Server | 纯 C# 类 `[EnableClass]` |
| GhostSnapshot | Model/Server | 纯 C# 类 `[EnableClass]` |
| CombatSimulator | Hotfix/Server | 静态服务类 |
| CombatStartProcessor | Hotfix/Server | 静态服务类 |
| PairingService | Hotfix/Server | 静态服务类 |
| SettlementService | Hotfix/Server | 静态服务类 |
| PhaseChangedEventHandler_Battle | Hotfix/Server | 事件处理器 `[Event(SceneType.Map)]` |

### PrngPurpose 新增

- `PrngPurpose.Combat` — 战斗模拟随机（暴击、技能）
- `PrngPurpose.Pairing` — 配对洗牌

### MatchRoom 新增字段

- `GhostSnapshot LastGhost` — 最近淘汰者快照
- `List<long> LastRoundLosers` — 已有，E8 结算写入

## 风险 / 权衡

| 风险 | 缓解措施 |
|------|----------|
| Tick 循环复杂度高（动态羁绊+技能+移动） | 分离 CombatStartProcessor；每种行为独立辅助方法 |
| 事件流体积大（单场数百条） | 通用 CombatEvent 单类减少类型开销；E9 可做压缩 |
| 动态羁绊内联可能使 CombatSimulator 过大 | 拆分为独立私有方法，必要时提取到辅助类 |
| Ghost 快照内存 | 仅保存 UnitInfo 列表 + TraitSnapshot，轻量 |
