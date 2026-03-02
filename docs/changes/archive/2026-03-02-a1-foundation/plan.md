# Implementation Plan: a1-foundation

> **Change:** a1-foundation
> **Source:** proposal.md + design.md
> **Stories:** 1.1 ~ 1.4

## Task List

### Phase 1: 包骨架与常量定义（Story 1.1 前半）

- [x] **1.1** 创建 `cn.etetet.autochess` 包目录结构（package.json + asmdef）
  - 创建 Packages/cn.etetet.autochess/ 骨架
  - Model/Share, Model/Server, Model/Client, Hotfix/Share, Hotfix/Server, Hotfix/Client 目录
  - 创建对应的 .asmdef 文件（参考 cn.etetet.statesync 的 asmdef 配置）
  - 验证：dotnet build 不报错

- [x] **1.2** 定义游戏常量 `AutoChessDefine`
  - 文件：Model/Share/AutoChess/AutoChessDefine.cs
  - 内容：棋盘(8×5)、板凳(5)、商店(3)、初始HP(12)、阶段时长(2s/20s/3s/30s/3s)、圣水上限(999)、人口递增表、SceneType(10030)
  - 验证：编译通过

- [x] **1.3** 定义回合阶段枚举 `RoundPhase`
  - 文件：Model/Share/AutoChess/RoundPhase.cs
  - 枚举值：None=0, RoundStart=1, Deployment=2, PreBattle=3, Battle=4, RoundEnd=5
  - 验证：编译通过

- [x] **1.4** 定义操作类型枚举 `OperationType`
  - 文件：Model/Share/AutoChess/OperationType.cs
  - 枚举值：Buy, Sell, Place, Swap, Merge（用于 PhaseGate）
  - 验证：编译通过

- [x] **1.5** 定义 PRNG Purpose 枚举 `PrngPurpose`
  - 文件：Model/Share/AutoChess/PrngPurpose.cs
  - 枚举值：ShopReroll=1, Pairing=2, Combat=3, CommanderPassive=4, GoblinGift=5, FirstGift=6
  - 验证：编译通过

- [x] **1.6** 定义棋盘坐标结构 `HexCoord`
  - 文件：Model/Share/AutoChess/HexCoord.cs
  - readonly struct，字段：Col(int), Row(int)
  - 方法：IsValid()（检查 0≤Col<8, 0≤Row<5）、Distance(other)（曼哈顿距离）、Equals/GetHashCode/ToString
  - 静态：BoardWidth=8, BoardHeight=5, BenchSize=5, IsBench(row)（row == -1 表示板凳）
  - 纯数据结构，后续 E4 单位摆位和 E7 战斗寻路均依赖此结构
  - 验证：编译通过

### Phase 2: 数据模型定义（Story 1.1 后半）

- [x] **2.1** 定义单位模板 `UnitTemplateDef`
  - 文件：Model/Share/AutoChess/Defs/UnitTemplateDef.cs
  - 字段：Id, Name, Cost, Rarity, Hp, Atk, AtkSpeed, Range, MoveSpeed, CritChance, ManaGainOnAttack, ManaGainOnHit, Tags(string[2]), SkillDefId
  - 不是 Entity，纯数据类（POCO）
  - 验证：编译通过

- [x] **2.2** 定义羁绊定义 `SynergyDef`
  - 文件：Model/Share/AutoChess/Defs/SynergyDef.cs
  - 字段：Id, Name, Tag, Thresholds(int[]), Effects（结构化效果参数）
  - 验证：编译通过

- [x] **2.3** 定义技能定义 `SkillDefData`
  - 文件：Model/Share/AutoChess/Defs/SkillDefData.cs
  - 字段：Id, TriggerType, TargetType, Effects[], TimingParams, ScalingParams
  - 技能 Cell 的数据表达，不含执行逻辑
  - 验证：编译通过

- [x] **2.4** 实现配置加载器 `AutoChessConfigLoader`
  - 文件：Hotfix/Share/AutoChess/AutoChessConfigLoader.cs
  - 硬编码 24 个 UnitTemplateDef + 13 个 SynergyDef + 16 个 SkillDefData
  - 提供 GetUnit(id)/GetSynergy(tag)/GetSkill(id) 查询接口
  - 验证：加载后 Assert 数量正确（24/13/16）

### Phase 3: 确定性 PRNG（Story 1.3）

- [x] **3.1** 实现 xxHash32 纯 C# 版本
  - 文件：Hotfix/Share/AutoChess/XxHash32.cs
  - 静态方法：uint Hash(uint seed, int a, int b)
  - 验证：已知输入对应已知输出（查表验证）

- [x] **3.2** 实现 DeterministicRngComponent（Entity + State）
  - 文件：Model/Share/AutoChess/DeterministicRngComponent.cs
  - [ComponentOf(typeof(MatchRoom))]，IAwake<uint>
  - 字段：State0~3 (ulong×4), InitialSeed (uint)
  - 验证：编译通过

- [x] **3.3** 实现 DeterministicRngComponentSystem（xoshiro256**）
  - 文件：Hotfix/Share/AutoChess/DeterministicRngComponentSystem.cs
  - 方法：Awake(seed), NextUlong(), NextInt(min,max), NextFloat(), Shuffle<T>()
  - SplitMix64 初始化
  - 验证：同 seed 100 次 NextInt 序列完全一致（金标测试）

- [x] **3.4** 实现子种子派生方法
  - 在 DeterministicRngComponentSystem 中添加 DeriveSubSeed(PrngPurpose, round)
  - 返回新的独立 PRNG 实例或 seed 值
  - 验证：不同 purpose 产生不同序列，同 purpose+round 产生相同序列

### Phase 4: Entity 树与 Match 生命周期（Story 1.4）

- [x] **4.1** 实现 MatchComponent
  - Model 文件：Model/Server/AutoChess/MatchComponent.cs
  - [ComponentOf(typeof(Scene))], IAwake, IDestroy
  - 字段：Dictionary<long, MatchRoom> Rooms
  - Hotfix 文件：Hotfix/Server/AutoChess/MatchComponentSystem.cs
  - 方法：CreateRoom/GetRoom/RemoveRoom

- [x] **4.2** 实现 MatchRoom
  - Model 文件：Model/Server/AutoChess/MatchRoom.cs
  - [ChildOf(typeof(MatchComponent))], IAwake
  - 字段：MatchSeed(uint), CurrentRound(int), MatchState(enum: Waiting/InGame/Settling/Finished), FinalResults(List<(long PlayerId, int Rank)>)
  - Hotfix 文件：Hotfix/Server/AutoChess/MatchRoomSystem.cs
  - 方法：Init(playerIds), StartMatch(), NextRound(), EndMatch()

- [x] **4.3** 实现 MatchPlayer
  - Model 文件：Model/Server/AutoChess/MatchPlayer.cs
  - [ChildOf(typeof(MatchRoom))], IAwake
  - 字段：PlayerId(long), Hp(int), Elixir(int), PopCap(int), IsAlive(bool), Rank(int)
  - Hotfix 文件：Hotfix/Server/AutoChess/MatchPlayerSystem.cs

- [x] **4.4** 实现 MatchRoomFactory
  - 文件：Hotfix/Server/AutoChess/MatchRoomFactory.cs
  - CreateMatch(MatchComponent, playerIds) → 创建 MatchRoom + 4×MatchPlayer + RoundFSM + PRNG
  - 验证：创建后 Entity 树结构正确

### Phase 5: 回合状态机（Story 1.2）

- [x] **5.1** 实现 RoundFSMComponent
  - Model 文件：Model/Server/AutoChess/RoundFSMComponent.cs
  - [ComponentOf(typeof(MatchRoom))], IAwake, IDestroy
  - 字段：CurrentPhase, PreviousPhase, PhaseStartTime, PhaseDuration, CurrentRound, ETCancelToken

- [x] **5.2** 实现 RoundFSMComponentSystem — 阶段切换核心
  - 文件：Hotfix/Server/AutoChess/RoundFSMComponentSystem.cs
  - SwitchPhaseAsync(nextPhase)：Cancel 旧 token → 创建新 token → 更新状态 → 发布 PhaseChangedEvent → RunPhaseAsync
  - RunPhaseAsync：按阶段 Wait 对应时长，到时自动切换下一阶段
  - StartRoundLoop()：从 RoundStart 开始循环

- [x] **5.3** 实现操作门控 PhaseGate
  - 在 RoundFSMComponentSystem 中添加 CanOperate(phase, operationType)
  - 操作矩阵：
    - Deployment: Buy/Sell/Place/Swap 全允许
    - Battle: Buy/Sell 允许（受限），Place/Swap 禁止
    - 其他阶段: 全禁止
  - 验证：所有阶段×操作组合的 bool 结果正确

- [x] **5.4** 定义 PhaseChangedEvent
  - 文件：Model/Share/AutoChess/Events/PhaseChangedEvent.cs
  - struct：MatchRoomId, OldPhase, NewPhase, Round
  - 验证：事件可被订阅和触发

- [x] **5.5** 实现回合循环与终局判定
  - 在 MatchRoomSystem 中：每轮 RoundEnd 后检查存活人数
  - 存活 > 1：NextRound()，回合数+1，更新 popCap
  - 存活 == 1：EndMatch()，设置排名
  - **排名输出**：EndMatch 时按淘汰顺序反向赋 Rank（最后存活=1st，倒数第二淘汰=2nd，依次类推），将结果写入 MatchRoom.FinalResults（List<(long PlayerId, int Rank)>）
  - 验证：4 玩家手动设 HP=0 可触发淘汰和终局，FinalResults 包含 4 条按 Rank 排序的结果

### Phase 6: 集成验证

- [x] **6.1** 编写 MatchIntegrationTest
  - 创建 Match → 4 玩家 → StartMatch → 回合自动推进
  - 验证：阶段按正确顺序和时长切换
  - 验证：手动淘汰玩家后回合循环正确处理（4人→3人→2人→1人→结束）
  - 验证：PRNG 确定性（同 seed 两次运行，回合序列一致）

- [x] **6.2** 验证 ET 框架合规
  - dotnet build 无错误
  - 分析器无违规（Entity 无方法、无 new Entity、异步返回 ETTask 等）
  - 所有 Component 有正确的 [ComponentOf]/[ChildOf] 声明

## Completion Criteria

- [x] `cn.etetet.autochess` 包结构建立，dotnet build 通过
- [x] HexCoord 坐标结构可用（IsValid/Distance/Bench 判定）
- [x] 24 个 UnitTemplateDef + 13 个 SynergyDef + 16 个 SkillDefData 可加载
- [x] PRNG 金标测试通过（同 seed 100 次一致）
- [x] 回合状态机可自动循环（5 阶段 × N 轮）
- [x] 操作门控矩阵正确
- [x] Match 生命周期完整（创建→运行→终局→清理）
- [x] 4 玩家淘汰流程正确，FinalResults 排名输出正确
