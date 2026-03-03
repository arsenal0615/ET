# 系统关系图

> 中粒度（Component/System 级别）。由 `/qx-compound` 执行时检查并更新。
>
> 最后更新：2026-03-03

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
  - **MatchPlayer** (ChildOf: MatchRoom) — 玩家实体（Elixir、HP、PopCap、IsAlive 等）
    - **EconomyLogComponent** (ComponentOf: MatchPlayer) — 圣水流水账（List<EconomyDelta>）

### 经济系统

- **EconomyService** (静态服务类, Hotfix/Server) — 所有圣水变更的唯一入口
  - `GiveRoundIncome` / `GiveCommanderPassive` / `TryDeductBuy` / `GiveSell` / `GiveMerge`
  - 私有 `ApplyDelta` — clamp(0, MaxElixir=999) + log → EconomyLogComponent + publish ElixirChangedEvent
  - `GiveCommanderPassive` 使用 `DeriveSubSeed(PrngPurpose.CommanderPassive, round)` 独立子种子
- **EconomyDelta** (struct, Model/Share) — 单条流水记录（Type/Amount/Round/Context）
- **EconomyDeltaType** (enum, Model/Share) — RoundIncome=1, CommanderPassive=2, Buy=3, Sell=4, Merge=5

### 事件

- **PhaseChangedEvent** — RoundFSMComponent 切相时发布；PhaseChangedEventHandler_Economy 订阅 RoundStart 发放收入
- **ElixirChangedEvent** — EconomyService.ApplyDelta 发布；供 E9 网络层订阅推送客户端

### 关键字段

- **MatchRoom.LastRoundLosers** (List<long>) — E5 战斗结算写入，E2 经济 Handler 读取（统帅被动）；Round 1 为空 → 无被动

### 配置

- **AutoChessConfigLoader** (静态) — 单位(24) / 协同(13) / 技能(16) 配置加载
- **AutoChessDefine** — 所有魔法数字（BoardWidth=8, BenchSize=5, MaxElixir=999, PopCapByRound[] 等）

### 工厂与辅助

- **MatchRoomFactory** — CreateMatch(matchComp, playerIds, seed)：创建 MatchRoom + MatchPlayer + 组件初始化
- **MatchRoomSystem** — GetAlivePlayers()、FindPlayerById()、StartMatch()、EliminatePlayer()、EndMatch()
- **AutoChessTestHelper** — 集成测试：ConfigLoader / Prng / EntityTree / PhaseGate / Economy

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
