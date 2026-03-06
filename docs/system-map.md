# 系统关系图

> 中粒度（Component/System 级别）。由 `/qx-compound` 执行时检查并更新。
>
> 最后更新：2026-03-06（a1-skill：SkillExecutor、TriggerChecker、TargetSelector、EffectApplier、ManaService、CombatUnitState、HexUtil）

---

## 核心基础设施（cn.etetet.core）

- **核心实体**: World（全局单例管理）、Fiber（调度单元）、Scene（场景根）、Entity（数据容器基类）
- **全局单例**: ObjectPool、EventSystem、FiberManager、MessageQueue、NetServices、IdGenerater
- **Fiber 调度器**: Main（主线程）、Thread（独占线程）、ThreadPool（线程池）
- **消息基础**: MessageQueue（ConcurrentQueue 跨 Fiber 通信）、MailBoxComponent（Actor 邮箱）、Mailboxes
- **网络层**: Session（连接包装）、AService/AChannel（TCP/KCP/WebSocket/UDP）
- **定时器**: TimerComponent（ComponentOf: Scene）— OnceTimer / OnceWaitTimer / RepeatedTimer
- **异步**: ETTask、ETCancellationToken、CoroutineLockComponent、ObjectWait
- **ID 生成**: EntityId（14bit Process + 30bit Time + 20bit Value）、InstanceId（32bit Time + 32bit Value）、ActorId（Address + InstanceId）
- **依赖**: 无（最底层）

## 内部网络通信（cn.etetet.netinner）

- **Component**: ProcessInnerSender（同进程跨 Fiber RPC）、MessageSender（跨进程 RPC）
- **消息**: A2NetInner_Message (Opcode 1)、A2NetInner_Request (Opcode 2)、A2NetInner_Response (Opcode 3)
- **超时**: 40 秒 RPC 超时
- **依赖**: core（MessageQueue、Fiber）

---

## 登录系统（cn.etetet.login）

- **Component（客户端）**: ClientSenderComponent (Scene)、PingComponent (Session)、PlayerComponent (Scene)、SessionComponent (Scene)、RouterAddressComponent (Scene)、RouterCheckComponent (Session)、RouterConnector (NetComponent)、FiberParentComponent (Scene)
- **Component（服务端 Gate）**: PlayerComponent (Scene)、Player (ChildOf: PlayerComponent)、PlayerSessionComponent (Player)、SessionPlayerComponent (Session)、GateSessionKeyComponent (Scene)、GateMapComponent (Player)
- **System**: ClientSenderComponentSystem、PingComponentSystem、RouterAddressComponentSystem、RouterCheckComponentSystem、PlayerComponentSystem、GateSessionKeyComponentSystem
- **消息（外部）**: C2R_Login / R2C_Login（Realm 认证）、C2G_LoginGate / G2C_LoginGate（Gate 登录）、C2G_Ping / G2C_Ping（心跳）、Main2NetClient_Login / NetClient2Main_Login（跨 Fiber）
- **消息（内部）**: R2G_GetLoginKey / G2R_GetLoginKey（Realm→Gate 密钥申请）、G2M_SessionDisconnect（会话断开通知）
- **Handler**: C2R_LoginHandler (SceneType.Realm)、C2G_LoginGateHandler (SceneType.Gate)、C2G_PingHandler (SceneType.Gate)、R2G_GetLoginKeyHandler (SceneType.Gate)
- **事件**: LoginFinish → 触发 EnterMap
- **辅助**: LoginHelper、RouterHelper、RealmGateAddressHelper
- **SceneType**: Realm (9001)、Gate (9002)、Router (9003)、RouterManager (9004)
- **关键流程**: 客户端 → Router 握手 → C2R_Login(Realm) → 获取 Gate 地址+Key → C2G_LoginGate(Gate) → 返回 PlayerId
- **依赖**: core（Fiber/Session/Timer）、netinner（跨进程通信）

## 单位系统（cn.etetet.unit）

- **Component**: UnitComponent (ComponentOf: Scene, 单位容器)、Unit (ChildOf: UnitComponent, 游戏角色/怪物/NPC)
- **System**: UnitSystem（Config/Type 访问）、UnitComponentSystem（Add/Get/Remove）
- **事件**: ChangePosition（Unit 位置变更）、ChangeRotation（Unit 旋转变更）
- **常量**: UnitType — Player (5001)、Monster (5002)、NPC (5003)
- **配置**: UnitConfig（Excel 生成，Id/Type/Name/Position/Height/Weight）
- **消息**: 无（由 statesync 包定义）
- **依赖**: core（Entity 基类）、excel（UnitConfig）

## 移动系统（cn.etetet.move）

- **Component**: MoveComponent (ComponentOf: Unit) — Targets 路径点、Speed、StartPos、NeedTime、MoveTimer、tcs
- **System**: MoveComponentSystem — MoveToAsync(targets, speed)、ChangeSpeed、Stop、FlashTo、IsArrived
- **定时器**: MoveTimer (TimerInvokeType 4001) — 每帧推进移动插值
- **事件**: MoveStart（移动开始）、MoveStop（移动结束）
- **消息**: 无（由 statesync 包定义）
- **依赖**: core（TimerComponent/EventSystem）、unit（Unit 实体）

## AOI 系统（cn.etetet.aoi）

- **Component**: AOIManagerComponent (ComponentOf: Scene, 仅服务端)、AOIEntity (ComponentOf: Unit)、Cell (ChildOf: AOIManagerComponent)
- **System**: AOIManagerComponentSystem（Add/Remove/Move）、AOIEntitySystem（SubEnter/SubLeave/EnterSight/LeaveSight）、CellSystem
- **事件**: UnitEnterSightRange（单位进入视野）、UnitLeaveSightRange（单位离开视野）
- **算法**: 九宫格划分（CellSize=10m），Player leaveR = enterR + 1（防闪烁）
- **辅助**: AOIHelper（CellId 合成/视野计算）、AOISeeCheckHelper（可见性检查，可扩展）
- **关键数据**: SeeUnits（我看见的）、BeSeeUnits（看见我的）、SeePlayers/BeSeePlayers
- **依赖**: core（EventSystem）、unit（Unit/UnitType）

## AI 系统（cn.etetet.ai）

- **Component**: AIComponent (ComponentOf: Scene，服务端 Unit 上)、AIDispatcherComponent（全局单例处理器分发）
- **System**: AIComponentSystem — Awake(aiConfigId)、Check()（每 1s 定时检查）
- **Handler 框架**: [AIHandler] 属性 + AAIHandler 抽象类（Check + Execute）
- **定时器**: AITimer (TimerInvokeType 31001) — 周期 1000ms
- **配置**: AIConfig（Excel 生成，AIConfigId/Order/Name/NodeParams）
- **依赖**: core（TimerComponent/ETCancellationToken）、unit（Unit）

## 状态同步演示（cn.etetet.statesync）

### 进入地图 / 场景切换

- **Component**: CurrentScenesComponent (ComponentOf: Scene, Share)、GateMapComponent (Player, Server)、OperaComponent (ComponentOf: Scene, Client)
- **消息**: C2G_EnterMap / G2C_EnterMap（进入地图）、C2M_TransferMap / M2C_TransferMap（跨场景转移）、M2C_StartSceneChange（场景切换通知）、M2C_CreateMyUnit / M2C_CreateUnits / M2C_RemoveUnits（单位同步）
- **Handler（服务端）**: C2G_EnterMapHandler (SceneType.Gate)、C2M_TransferMapHandler (SceneType.Map)、M2M_UnitTransferRequestHandler
- **Handler（客户端）**: M2C_CreateMyUnitHandler / M2C_CreateUnitsHandler / M2C_RemoveUnitsHandler / M2C_StartSceneChangeHandler (SceneType.StateSync)
- **事件**: SceneChangeStart、SceneChangeFinish、EnterMapFinish、AfterUnitCreate
- **Wait**: Wait_SceneChangeFinish、Wait_CreateMyUnit
- **辅助**: EnterMapHelper、SceneChangeHelper、TransferHelper、UnitFactory（Client/Server 两版）
- **关键流程**: C2G_EnterMap → Gate 创建 GateMap → UnitFactory 创建 Unit → TransferHelper 转移到 Map → M2C_StartSceneChange → M2C_CreateMyUnit

### 移动同步

- **消息（客户端→服务端）**: C2M_PathfindingResult (ILocationMessage)、C2M_MoveForward (ILocationMessage)、C2M_Stop (ILocationMessage)
- **消息（服务端→客户端）**: M2C_PathfindingResult (IMessage)、M2C_Stop (IMessage)
- **Handler（服务端）**: C2M_PathfindingResultHandler / C2M_MoveForwardHandler / C2M_StopHandler (SceneType.Map)
- **Handler（客户端）**: M2C_PathfindingResultHandler / M2C_StopHandler (SceneType.StateSync)
- **辅助**: MoveHelper（Client/Server 两版）
- **关键流程**: 客户端输入 → C2M_PathfindingResult → 服务端寻路 → MoveComponent.MoveToAsync → M2C_PathfindingResult 广播 → M2C_Stop

### AI 巡逻（演示）

- **Component**: XunLuoPathComponent (ComponentOf: Unit, Client 巡逻路径)
- **System**: XunLuoPathComponentSystem — GetCurrent/MoveNext
- **Handler**: AI_XunLuo (AAIHandler) — 每 15s 检查，循环路径点移动

### 机器人测试

- **Component**: RobotManagerComponent (ComponentOf: Scene, Server)
- **System**: RobotManagerComponentSystem — NewRobot()
- **消息**: C2M_TestRobotCase / M2C_TestRobotCase、C2M_TestRobotCase2 / M2C_TestRobotCase2、C2G_Benchmark / G2C_Benchmark
- **Handler**: CreateRobotConsoleHandler、C2M_TestRobotCaseHandler
- **SceneType**: Robot (10003)

### UI 视图（客户端）

- **Component**: UILoginComponent、UILobbyComponent、UILoadingComponent、UIHelpComponent、AnimatorComponent (Unit)、GameObjectComponent (Unit)
- **事件处理器**: AppStartInitFinish_CreateLoginUI、LoginFinish_CreateLobbyUI、LoginFinish_RemoveLoginUI、SceneChangeFinishEvent_CreateUIHelp、AfterUnitCreate_CreateUnitView
- **依赖**: FairyGUI

### SceneType 常量

| SceneType | 值 | 说明 |
|-----------|-----|------|
| Http | 10001 | HTTP 服务 |
| Map | 10002 | 地图场景（服务端） |
| Robot | 10003 | 机器人测试 |
| StateSync | 10020 | 客户端根场景 |
| Current | 10021 | 客户端当前场景 |

### 全局依赖

- **依赖**: core、netinner、login、unit、move、aoi、ai、excel

---

## 自走棋系统（cn.etetet.autochess）

### Entity 树结构

- **MatchComponent** (ComponentOf: Scene，服务端) — 比赛容器，管理所有 MatchRoom
- **MatchRoom** (ChildOf: MatchComponent) — 一局比赛实体
  - **DeterministicRngComponent** (ComponentOf: MatchRoom) — xoshiro256** PRNG，保证确定性
  - **RoundFSMComponent** (ComponentOf: MatchRoom) — 回合状态机（None→RoundStart→Deployment→PreBattle→Battle→RoundEnd）
  - **SharedPoolComponent** (ComponentOf: MatchRoom) — 全局共享卡池（int[] Remaining，length=24，按 templateId-1 索引，初始 8 份/种）
  - **MatchPlayer** (ChildOf: MatchRoom) — 玩家实体（Elixir、HP、PopCap、IsAlive、PlayerIndex 等）
    - **EconomyLogComponent** (ComponentOf: MatchPlayer) — 圣水流水账（List<EconomyDelta>）
    - **ShopComponent** (ComponentOf: MatchPlayer) — 玩家商店（ShopOffer[3] 槽位、ShopRollIndex 全局递增、FirstRoundGiftTemplateId）
    - **RosterComponent** (ComponentOf: MatchPlayer) — 单位列表（List\<UnitInfo\> Units、NextInstId 自增）
    - **SynergyComponent** (ComponentOf: MatchPlayer) — 羁绊状态（LiveSynergies 实时统计、Snapshot 冻结快照、LastGoblinLevel 跨回合持久化）

### 经济系统

- **EconomyService** (静态服务类, Hotfix/Server) — 所有圣水变更的唯一入口
  - `GiveRoundIncome` / `GiveCommanderPassive` / `TryDeductBuy` / `GiveSell` / `GiveMerge`
  - 私有 `ApplyDelta` — clamp(0, MaxElixir=999) + log → EconomyLogComponent + publish ElixirChangedEvent
  - `GiveCommanderPassive` 使用 `DeriveSubSeed(PrngPurpose.CommanderPassive, round)` 独立子种子
- **EconomyDelta** (struct, Model/Share) — 单条流水记录（Type/Amount/Round/Context）
- **EconomyDeltaType** (enum, Model/Share) — RoundIncome=1, CommanderPassive=2, Buy=3, Sell=4, Merge=5

### 商店系统（a1-shop）

- **ShopService** (静态服务类, Hotfix/Server) — 商店操作唯一入口
  - `GenerateOffersForPlayer(player, pool, rng)` — 归还上次预扣 → 从可用模板中确定性抽 3 个 → 立即预扣
  - `ReturnOffersToPool(player, pool)` — 归还当前所有 Offer（淘汰/结算时）
  - `TryBuy(player, slotIndex, pool, rng, currentPhase, round)` — 圣水校验 + 清空槽 + 全行重刷
  - `TrySell(player, templateId, starLevel, isGift, pool, round)` — 圣水返还 + 卡池回流（isGift=true 不回流）
- **FirstRoundGiftService** (静态服务类, Hotfix/Server) — 首回合 2 费礼包选取
  - `SelectGiftTemplate(player, rng)` — 用 `DeriveSubSeed(PrngPurpose.FirstGift, player.PlayerIndex)` 确定性选取 2 费单位
  - 结果写入 `ShopComponent.FirstRoundGiftTemplateId`；不扣卡池（isGift=true）
- **ShopOffer** (非 Entity 普通类, Model/Server) — 商店槽位值对象（TemplateId，-1=空）；需 `[EnableClass]`

### 单位系统（a1-unit）

- **UnitInfo** (纯 C# 类, Model/Server, `[EnableClass]`) — 单位实例数据（InstId、TemplateId、Star、IsGift、Col、Row）；Row=-1 表示板凳，Row>=0 表示棋盘
- **RosterService** (静态服务类, Hotfix/Server) — 单位增删查统计
  - `AddToBench(player, templateId, star, isGift)` — 分配 InstId，自动找最小空板凳列
  - `Remove(player, unit)` — 从 Units 列表移除
  - `FindAt(player, col, row)` / `FindByInstId(player, instId)` — 查找
  - `GetPopUsed(player)` / `GetBenchUsed(player)` — 棋盘人口 / 板凳占用统计
  - `RecoverAllToPool(player, pool)` — 淘汰时回收所有单位到卡池（1<<(star-1) 份/个，isGift 跳过）
- **PlacementService** (静态服务类, Hotfix/Server) — 摆位操作
  - `TryPlaceToBoard(player, instId, col, row, popCap)` — 板凳→棋盘（校验人口、坐标、目标空闲）
  - `TryMoveOnBoard(player, instId, col, row)` — 棋盘内移动（校验目标空闲）
  - `TrySwap(player, instIdA, instIdB)` — 任意两单位交换坐标
- **MergeService** (静态服务类, Hotfix/Server) — 确定性合成
  - `RunMergeChain(player, round)` — while 循环找同模板同星级对 → 升星 + GiveMerge(+1圣水) → 返回合成次数
  - 排序：star ASC → templateId ASC → locationPriority(board<bench) ASC → row ASC → col ASC → instId ASC
- **PreBattleService** (静态服务类, Hotfix/Server) — 战前校验与自动修正
  - `ValidateAndFix(player, pool, popCap, round)` — 超人口单位按 retain 优先级降序移到板凳，板凳满则强制出售
  - Retain 优先级：star DESC → cost DESC → row ASC → col ASC → instId ASC
- **UnitService** (门面类, Hotfix/Server) — 单位操作统一入口
  - `Buy(player, slotIndex, pool, rng, currentPhase, round)` — ShopService.TryBuy + AddToBench + RunMergeChain
  - `Sell(player, unit, pool, round)` — Remove + ShopService.TrySell
  - `CreateFirstRoundGift(player)` — 读取 FirstRoundGiftTemplateId → AddToBench(isGift=true)

### 羁绊系统（a1-synergy）

- **SynergyService** (静态服务类, Hotfix/Server) — 羁绊统计与快照生成
  - `Recalculate(player)` — 遍历棋盘单位标签统计 count/level，更新 LiveSynergies，有变化发布 SynergyChangedEvent
  - `GenerateSnapshot(player)` — 基于 LiveSynergies 生成冻结 TraitSnapshot（ActiveSynergies + UnitSynergyMap + UnitModifiers），应用静态效果，更新 LastGoblinLevel
  - `ApplyStaticEffects(unit, synergies, mods)` — 5 种静态羁绊属性加成（Brawler/Giant/Noble/Blaster/Brutalist）
- **GoblinGiftService** (静态服务类, Hotfix/Server) — Goblin 经济羁绊
  - `TryGiveGifts(player, rng, round)` — 根据 LastGoblinLevel 赠送哥布林单位（isGift=true，不扣卡池）
- **数据类** (Model/Server, `[EnableClass]`)
  - **SynergyEntry** — 单条羁绊统计（Tag/Count/Level/Type）
  - **TraitSnapshot** — 冻结快照（ActiveSynergies、UnitSynergyMap、UnitModifiers）
  - **UnitBattleModifiers** — 单位属性修改值（HpMultiplier/DamageReduction/AtkSpeedMultiplier 等），E7 战斗初始化时读取
- **SynergyType** (enum, Model/Share) — Static=1, Dynamic=2, Economic=3
- **集成点**：PlacementService（棋盘变化→Recalculate）、UnitService（Buy/Sell→Recalculate）、MatchRoomFactory（添加 SynergyComponent）

### 技能系统（a1-skill）

- **数据类** (Model/Share, namespace ET, `[EnableClass]`)
  - **CombatUnitState** — 战斗单位运行时状态（HP/Atk/Mana/位置/Buffs/TriggerState + 内联修改器字段）
  - **ActiveBuff** — Buff 运行时数据（Type/RemainingTicks/Value1/Value2）
  - **TriggerState** — 触发器运行时状态（HitCount/KillTriggerCount/HpBelowTriggered/LastIntervalTick）
  - **SkillEffectResult** — 单个效果结果（TargetInstId/EffectType/Value/Col/Row）
  - **SkillExecutionResult** — 技能执行结果（Status/CasterInstId/SkillId/EffectResults）
- **BuffType** (enum, Model/Share) — None=0, Stun=1, Invisibility=2, Reflect=3, HealOverTime=4, SpeedBuff=5
- **SkillExecutionStatus** (enum, Model/Share) — NotTriggered=0, NoTargets=1, Executed=2
- **HexUtil** (静态工具类, Model/Share) — 六边形距离/邻居/坐标转换（odd-r offset ↔ axial）
- **ManaService** (静态服务类, Hotfix/Server) — 法力累积/消耗（GainOnAttack/GainOnHit/IsFull/Consume）
- **TriggerChecker** (静态服务类, Hotfix/Server) — 8 种触发条件判定（CombatStart/Interval/OnHitCount/OnKill/OnHpBelow/ManaFull/AttackTrait/OnDeath）
- **TargetSelector** (静态服务类, Hotfix/Server) — 10 种目标选取算法（Self/NearestEnemy/FarthestInRadius/FarthestInRange/LowestHp/MultiTargets/AreaRadius/ClusterLargest/LinePierce/Cone）
- **EffectApplier** (静态服务类, Hotfix/Server) — 10 种效果应用（Damage/Stun/Knockback/Invisibility/SpeedBuff/Summon/Clone/Reflect/HealOverTime/Projectile）+ TickBuff 生命周期
- **SkillExecutor** (门面类, Hotfix/Server) — 唯一入口，编排 Trigger→Target→Effect 管线 + Superstar 连发（DeriveSubSeed(SkillChain)）
- **集成点**: E7 战斗模拟器每 tick 调用 SkillExecutor.TryExecute + TickBuffs；CombatUnitState 从 UnitInfo + UnitBattleModifiers 初始化
- **测试**: AutoChessTestHelper — HexUtil / Mana / TriggerChecker / TargetSelector / EffectApplier / SkillExecutor / SkillSystem（7+6 个测试）

### 事件

- **PhaseChangedEvent** — RoundFSMComponent 切相时发布；字段：`MatchRoomId`（long）、`Round`（int）、`NewPhase`（RoundPhase）
  - **PhaseChangedEventHandler_Economy** `[Event(SceneType.Map)]` — 订阅 RoundStart 发放收入 + 统帅被动
  - **PhaseChangedEventHandler_Shop** `[Event(SceneType.Map)]` — 订阅 RoundStart 刷新所有玩家商店 Offer；Round 1 额外执行首回合礼包 + UnitService.CreateFirstRoundGift
  - **PhaseChangedEventHandler_Unit** `[Event(SceneType.Map)]` — 订阅 Deployment（MergeService.RunMergeChain）+ PreBattle（PreBattleService.ValidateAndFix）
  - **PhaseChangedEventHandler_Synergy** `[Event(SceneType.Map)]` — 订阅 Deployment（Recalculate）+ PreBattle（GenerateSnapshot）+ RoundStart≥2（TryGiveGifts）
- **ElixirChangedEvent** — EconomyService.ApplyDelta 发布；供 E9 网络层订阅推送客户端
- **SynergyChangedEvent** — SynergyService.Recalculate 发布；供 E9 网络层订阅推送客户端

### 关键字段

- **MatchRoom.LastRoundLosers** (List<long>) — E5 战斗结算写入，E2 经济 Handler 读取（统帅被动）；Round 1 为空 → 无被动

### 配置

- **AutoChessConfigLoader** (静态) — 单位(24) / 协同(13) / 技能(16) 配置加载
- **AutoChessDefine** — 所有魔法数字（BoardWidth=8, BenchSize=5, MaxElixir=999, PopCapByRound[] 等）

### 工厂与辅助

- **MatchRoomFactory** — CreateMatch(matchComp, playerIds, seed)：创建 MatchRoom + 按序赋值 PlayerIndex + 添加 EconomyLogComponent + ShopComponent + RosterComponent + SynergyComponent + SharedPoolComponent + DeterministicRngComponent + RoundFSMComponent
- **MatchRoomSystem** — GetAlivePlayers()、FindPlayerById()、StartMatch()、EliminatePlayer()（含归还 Offer + RosterService.RecoverAllToPool）、EndMatch()
- **AutoChessTestHelper** — 集成测试：ConfigLoader / Prng / EntityTree / PhaseGate / Economy / Shop / Roster / Placement / Merge / PreBattle / UnitService / EliminationRecovery / SynergyCounting / SynergySnapshot / GoblinGift / HexUtil / Mana / TriggerChecker / TargetSelector / EffectApplier / SkillExecutor / SkillSystem（22 个测试）

### SceneType

- `SceneTypeAutoChessMatch = 10030`（含于 AutoChessDefine，Package 10 * 1000 + 30）

### 依赖

- **依赖**: core（Entity/EventSystem/TimerComponent）

---

## Proto 消息 Opcode 分配

| 范围 | 用途 | 文件 |
|------|------|------|
| 1000–1099 | 外部登录消息 | LoginOuter_C_1000.proto |
| 1100–1199 | 外部路由消息 | RouterProto_C_1100.proto |
| 11001–11999 | 外部状态同步 | StateSyncOuter_C_11001.proto |
| 20001–20099 | 内部登录消息 | LoginInner_S_20001.proto |
| 20100–20199 | Actor Location | ActorLocation_S_20100.proto |
| 21001–21999 | 内部状态同步 | StateSyncInner_S_21001.proto |

## 系统依赖全景

```
cn.etetet.core（基础设施）
  ├── cn.etetet.netinner（跨进程通信）
  ├── cn.etetet.unit（单位实体）
  │     └── cn.etetet.excel（配置表）
  ├── cn.etetet.move（移动系统）
  │     └── cn.etetet.unit
  ├── cn.etetet.aoi（视野系统，仅服务端）
  │     └── cn.etetet.unit
  ├── cn.etetet.ai（AI 系统）
  │     └── cn.etetet.unit
  ├── cn.etetet.login（登录系统）
  │     └── cn.etetet.netinner
  ├── cn.etetet.statesync（状态同步演示）
  │     ├── cn.etetet.login
  │     ├── cn.etetet.unit
  │     ├── cn.etetet.move
  │     ├── cn.etetet.aoi
  │     └── cn.etetet.ai
  └── cn.etetet.autochess（自走棋）
        └── cn.etetet.core
```

## 关键业务流程

### 登录 → 进入游戏

```
客户端 → HTTP /get_router → 获取 Realm/Router 列表
  → Router 握手 → C2R_Login(Realm) → R2C_Login(Gate地址+Key)
  → Router 握手 → C2G_LoginGate(Gate) → G2C_LoginGate(PlayerId)
  → LoginFinish 事件
  → C2G_EnterMap(Gate) → Gate 创建 Unit → TransferHelper → Map
  → M2C_StartSceneChange → M2C_CreateMyUnit → 场景加载完成
```

### 移动同步

```
客户端输入(WASD/鼠标) → OperaComponent
  → C2M_PathfindingResult/MoveForward → 服务端寻路
  → MoveComponent.MoveToAsync → ChangePosition 事件 → AOI 更新
  → M2C_PathfindingResult 广播 → 客户端本地插值
  → M2C_Stop 广播停止
```

### AOI 视野同步

```
Unit 移动 → ChangePosition 事件 → ChangePosition_NotifyAOI
  → AOIManager.Move → Cell 变化检测 → 订阅/取消订阅
  → UnitEnterSightRange → MapMessageHelper.NoticeUnitAdd → M2C_CreateUnits
  → UnitLeaveSightRange → MapMessageHelper.NoticeUnitRemove → M2C_RemoveUnits
```
