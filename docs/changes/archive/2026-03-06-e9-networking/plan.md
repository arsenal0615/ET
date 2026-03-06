# E9 网络与同步 Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 为自走棋建立客户端↔服务端通信层，使客户端能发送操作意图、接收状态推送。

**Architecture:** 基于 ET 框架 Actor Location 机制。客户端通过 ILocationMessage 发送操作到 Map Unit，服务端通过 MailBox GateSession 推送状态。每个玩家在 Map 上有一个 Unit，通过 AutoChessUnitComponent 绑定 MatchPlayer。

**Tech Stack:** ET Proto 消息、MessageLocationHandler、MapMessageHelper、MailBox GateSession

**Impact:**
- Modules/Assemblies: cn.etetet.autochess（Model/Share, Model/Server, Hotfix/Server, Hotfix/Client）
- Code generation changes: 是 — 需要 Proto2CS 生成消息类
- New data models: AutoChessUnitComponent（ComponentOf: Unit）、AutoChessOpError 枚举
- New messages/protocols: AutoChessOuter_C_12001.proto（C2M + M2C）、AutoChessInner_S_22001.proto

**Rules:** messaging-network, ecs-patterns, code-templates, architecture

**Design Ref:** docs/changes/e9-networking/design.md

---

## 1. Proto 定义与共享类型

- [x] 1.1 创建操作错误码枚举 AutoChessOpError

**Context:**
- Why: 所有 Handler 和客户端都需要统一的错误码定义，放在 Model/Share 中双端共享

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessOpError.cs`

**Steps:**
1. 创建枚举文件，包含：OK=0, PHASE_NOT_ALLOWED=1, INVALID_SLOT=2, INSUFFICIENT_ELIXIR=3, BENCH_FULL=4, UNIT_NOT_FOUND=5, CANNOT_SELL_BOARD_IN_BATTLE=6, POS_OCCUPIED=7, POPCAP_EXCEEDED=8, INVALID_POSITION=9, MATCH_NOT_FOUND=10
2. 编译检查
3. 提交

- [x] 1.2 创建 AutoChessOuter Proto 文件

**Context:**
- Depends: 1.1
- Reads: `Packages/cn.etetet.statesync/Proto/StateSyncOuter_C_11001.proto`（参考格式）
- Why: 定义客户端↔服务端的所有消息类型

**Files:**
- Create: `Packages/cn.etetet.autochess/Proto/AutoChessOuter_C_12001.proto`

**Steps:**
1. 创建 Proto 文件，起始 Opcode 12001，包含：
   - 辅助 message：`AutoChessUnitInfoProto`（instId, templateId, star, col, row, location）、`AutoChessShopOfferProto`（slotIndex, templateId, cost）、`AutoChessPlayerPublicInfoProto`（playerId, hp, isAlive, rank）、`AutoChessSynergyProto`（synergyId, count, level）、`AutoChessCombatEventProto`（映射 CombatEvent 全字段）、`AutoChessPlayerRoundResultProto`（playerId, opponentId, won, hpChange, hpAfter）
   - C2M 操作（ILocationMessage）：`C2M_AutoChessBuy`（RpcId, slotIndex）、`C2M_AutoChessSell`（RpcId, instId）、`C2M_AutoChessPlace`（RpcId, instId, col, row）、`C2M_AutoChessSwap`（RpcId, instId1, instId2）
   - C2M 进入（ILocationRequest + Response）：`C2M_AutoChessEnterGame` / `M2C_AutoChessEnterGame`（初始状态快照）
   - M2C 推送（IMessage）：`M2C_AutoChessOpResult`（opType, errorCode, 增量数据）、`M2C_AutoChessPhaseChange`（round, newPhase, phaseEndTime）、`M2C_AutoChessRoundState`（完整状态快照）、`M2C_AutoChessCombatEvents`（repeated events）、`M2C_AutoChessRoundResult`（repeated playerResults, repeated eliminatedPlayerIds）、`M2C_AutoChessMatchResult`（repeated rankings）
2. 提交

- [x] 1.3 创建 AutoChessInner Proto 文件

**Context:**
- Reads: `Packages/cn.etetet.login/Proto/LoginInner_S_20001.proto`（参考格式）
- Why: 匹配服务→Map 的对局创建消息

**Files:**
- Create: `Packages/cn.etetet.autochess/Proto/AutoChessInner_S_22001.proto`

**Steps:**
1. 创建 Proto 文件，起始 Opcode 22001，包含：
   - `Match2Map_CreateAutoChessGame` (IRequest)：repeated int64 PlayerIds, uint32 MatchSeed
   - `Map2Match_CreateAutoChessGameResponse` (IResponse)：RpcId, Error, Message, int64 MatchRoomId
   - `Map2Match_AutoChessGameOver` (IMessage)：int64 MatchRoomId, repeated AutoChessPlayerRankProto Rankings
   - `AutoChessPlayerRankProto`：int64 PlayerId, int32 Rank
2. 提交

- [x] 1.4 运行 Proto2CS 代码生成 + 编译验证

**Context:**
- Depends: 1.2, 1.3
- Why: Proto 文件必须经过代码生成才能在 C# 中使用

**Steps:**
1. 运行 `dotnet Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./`
2. 验证生成文件存在（搜索 AutoChessBuy 等类名）
3. 运行项目编译（`dotnet build DotNet/ET.sln`）验证通过
4. 提交生成的文件

---

## 2. 对局绑定基础设施

- [x] 2.1 MatchPlayer 添加 MapUnitId 字段

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs`
- Why: MatchPlayer 需要知道对应的 Map Unit Id，以便通过 MapMessageHelper.SendToClient 向客户端推送消息

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs`

**Steps:**
1. 给 MatchPlayer 添加 `public long MapUnitId;` 字段
2. 编译检查
3. 提交

- [x] 2.2 创建 AutoChessUnitComponent

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchRoom.cs`
- Why: Map Unit 需要通过 Component 关联到 MatchRoom 和 MatchPlayer，Handler 才能找到游戏逻辑实体

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/AutoChessUnitComponent.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessUnitComponentSystem.cs`

**Steps:**
1. 创建 AutoChessUnitComponent（ComponentOf: Unit），字段：long MatchRoomId, int PlayerIndex, long PlayerId。实现 IAwake<long, int, long>
2. 创建对应的 EntitySystemOf System 类（IAwake 实现赋值字段）
3. 添加辅助方法：GetMatchRoom()（通过 Scene.GetComponent<MatchComponent>() 查找）、GetMatchPlayer()
4. 编译检查
5. 提交

- [x] 2.3 创建 AutoChessBroadcastHelper

**Context:**
- Depends: 2.1, 2.2
- Reads: `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Map/MapMessageHelper.cs`
- Why: 封装向单个玩家和所有存活玩家推送消息的逻辑，避免每个广播点重复查找 Unit

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessBroadcastHelper.cs`

**Steps:**
1. 创建静态类，包含：
   - `SendToPlayer(Scene scene, MatchPlayer player, IMessage message)` — 通过 player.MapUnitId 找到 Unit，调用 MapMessageHelper.SendToClient
   - `BroadcastToAlive(Scene scene, MatchRoom room, IMessage message)` — 遍历存活玩家，逐个 SendToPlayer
   - `BroadcastToAll(Scene scene, MatchRoom room, IMessage message)` — 遍历所有玩家（含已淘汰），逐个 SendToPlayer
2. SendToPlayer 中 MapUnitId == 0 时跳过（未绑定 Unit 的玩家，如测试环境）
3. 编译检查
4. 提交

- [x] 2.4 创建 PhaseGateHelper

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: 所有操作 Handler 需要统一的阶段门控检查

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseGateHelper.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper_Networking.cs`（追加测试方法到 AutoChessTestHelper 或新建）

**Steps:**
1. 创建静态类 PhaseGateHelper，方法：
   - `CanBuy(RoundPhase phase) → bool`：Deployment=true, Battle=true, 其他=false
   - `CanSell(RoundPhase phase) → bool`：Deployment=true, Battle=true, 其他=false
   - `CanSellFromBoard(RoundPhase phase) → bool`：Deployment=true, 其他=false
   - `CanPlace(RoundPhase phase) → bool`：Deployment=true, 其他=false
   - `CanSwap(RoundPhase phase) → bool`：Deployment=true, 其他=false
2. 编写测试：验证每个操作在每个阶段的门控结果
3. 运行测试验证通过
4. 编译检查
5. 提交

---

## 3. 对局创建与加入

- [x] 3.1 创建对局创建 Handler（Match→Map）

**Context:**
- Depends: 1.4, 2.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`、`Packages/cn.etetet.login/Scripts/Hotfix/Server/Gate/C2G_LoginGateHandler.cs`（参考 Unit 创建模式）
- Why: 匹配成功后需要在 Map 上创建对局、Unit 和绑定

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/Match2Map_CreateAutoChessGameHandler.cs`

**Steps:**
1. 创建 Handler（MessageHandler SceneType.Map），处理 Match2Map_CreateAutoChessGame 请求：
   - 创建 MatchRoom + 4 个 MatchPlayer（复用现有 MatchRoomSystem 逻辑）
   - 为每个玩家创建 Map Unit（从 UnitComponent 添加），挂载 MailBoxComponent(MailBoxType.GateSession) + AutoChessUnitComponent
   - 设置 MatchPlayer.MapUnitId = unit.Id
   - 在 Location Server 注册 Unit（LocationProxyComponent.Add）
   - 启动 RoundFSM
   - 返回 MatchRoomId
2. 编译检查
3. 提交

- [x] 3.2 创建进入对局 Handler（Client→Map）

**Context:**
- Depends: 3.1, 2.3
- Why: 客户端登录后请求进入对局，服务端返回初始状态快照

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/C2M_AutoChessEnterGameHandler.cs`

**Steps:**
1. 创建 Handler（MessageLocationHandler<Unit, C2M_AutoChessEnterGame, M2C_AutoChessEnterGame>），SceneType.Map：
   - 从 unit.GetComponent<AutoChessUnitComponent>() 获取 matchRoomId
   - 找到 MatchRoom 和 MatchPlayer
   - 构建初始状态快照（round、phase、elixir、popCap、popUsed、shopOffers、units、synergies、allPlayersPublicInfo）
   - 填充 response 字段
   - 如果 MatchRoom 不存在，设置 response.Error
2. 编译检查
3. 提交

---

## 4. 数据转换辅助

- [x] 4.1 创建 AutoChessProtoHelper

**Context:**
- Depends: 1.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/CombatEvent.cs`、`Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/ShopComponent.cs`
- Why: 服务端数据类型（UnitInfo、CombatEvent、ShopOffer 等）和 Proto 消息类型之间需要转换

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessProtoHelper.cs`

**Steps:**
1. 创建静态类，包含转换方法：
   - `ToUnitInfoProto(UnitInfo unit) → AutoChessUnitInfoProto`
   - `ToShopOfferProto(ShopOffer offer, int slotIndex) → AutoChessShopOfferProto`
   - `ToPlayerPublicInfoProto(MatchPlayer p) → AutoChessPlayerPublicInfoProto`
   - `ToSynergyProto(SynergyEntry s) → AutoChessSynergyProto`
   - `ToCombatEventProto(CombatEvent e) → AutoChessCombatEventProto`
   - `BuildRoundState(MatchRoom room, MatchPlayer player) → M2C_AutoChessRoundState`（构建完整快照）
2. 编写测试：构造 CombatEvent 实例，验证 ToCombatEventProto 字段映射正确
3. 运行测试验证通过
4. 编译检查
5. 提交

---

## 5. 服务端操作 Handler

- [x] 5.1 创建 C2M_AutoChessBuy Handler

**Context:**
- Depends: 2.3, 2.4, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`、`Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/UnitService.cs`
- Why: 处理客户端购买操作

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/C2M_AutoChessBuyHandler.cs`

**Steps:**
1. 创建 MessageLocationHandler<Unit, C2M_AutoChessBuy>（SceneType.Map）：
   - PhaseGate 检查 → 失败发 M2C_AutoChessOpResult(PHASE_NOT_ALLOWED)
   - 校验 slotIndex 0-2 → 失败发 INVALID_SLOT
   - 调用 UnitService.Buy（已有逻辑含圣水检查、板凳容量检查）
   - 成功：发 OpResult（errorCode=0）+ 增量数据（新单位、新圣水、新商店 offers）
   - 失败：发 OpResult（对应错误码）
2. 编译检查
3. 提交

- [x] 5.2 创建 C2M_AutoChessSell Handler

**Context:**
- Depends: 2.3, 2.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/UnitService.cs`
- Why: 处理客户端出售操作

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/C2M_AutoChessSellHandler.cs`

**Steps:**
1. 创建 MessageLocationHandler<Unit, C2M_AutoChessSell>（SceneType.Map）：
   - PhaseGate 检查
   - 查找 instId 对应的单位 → 不存在发 UNIT_NOT_FOUND
   - Battle 阶段只能卖板凳单位 → 棋盘单位发 CANNOT_SELL_BOARD_IN_BATTLE
   - 调用 UnitService.Sell
   - 发 OpResult（成功：移除的 instId + 更新后 elixir）
2. 编译检查
3. 提交

- [x] 5.3 创建 C2M_AutoChessPlace Handler

**Context:**
- Depends: 2.3, 2.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PlacementService.cs`
- Why: 处理客户端摆位操作

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/C2M_AutoChessPlaceHandler.cs`

**Steps:**
1. 创建 MessageLocationHandler<Unit, C2M_AutoChessPlace>（SceneType.Map）：
   - PhaseGate 检查（仅 Deployment）
   - 校验坐标合法（在棋盘范围内）→ INVALID_POSITION
   - 校验目标位置未被占用 → POS_OCCUPIED
   - 校验人口上限 → POPCAP_EXCEEDED
   - 调用 PlacementService 或 RosterService 移动单位
   - 发 OpResult（成功：instId + 新位置 + popUsed）
2. 编译检查
3. 提交

- [x] 5.4 创建 C2M_AutoChessSwap Handler

**Context:**
- Depends: 2.3, 2.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PlacementService.cs`
- Why: 处理客户端交换位置操作

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Handler/C2M_AutoChessSwapHandler.cs`

**Steps:**
1. 创建 MessageLocationHandler<Unit, C2M_AutoChessSwap>（SceneType.Map）：
   - PhaseGate 检查（仅 Deployment）
   - 查找两个 instId → 不存在发 UNIT_NOT_FOUND
   - 交换位置
   - 发 OpResult（成功：两个 instId 和各自新位置）
2. 编译检查
3. 提交

---

## 6. 服务端状态广播

- [x] 6.1 新增 PhaseChangedEventHandler_Broadcast

**Context:**
- Depends: 2.3, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs`（参考事件处理器模式）
- Why: 每次阶段切换时向所有存活玩家广播 PhaseChange 和 RoundState

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Broadcast.cs`

**Steps:**
1. 创建 `[Event(SceneType.Map)]` 事件处理器，监听 PhaseChangedEvent：
   - 所有阶段切换：BroadcastToAlive → M2C_AutoChessPhaseChange（round, newPhase, phaseEndTime = now + duration）
   - RoundStart 完成后（使用较高 EventOrder 确保在 Economy/Shop 之后执行）：给每个存活玩家 SendToPlayer → M2C_AutoChessRoundState（个人完整快照，通过 AutoChessProtoHelper.BuildRoundState）
2. 注意：需要确认 ET 事件系统是否支持同一事件类型多个 Handler 的执行顺序。如果不支持 Order，考虑在现有 Handler 末尾添加广播调用
3. 编译检查
4. 提交

- [x] 6.2 修改 PhaseChangedEventHandler_Battle 增加战斗广播

**Context:**
- Depends: 2.3, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Battle.cs`
- Why: 战斗完成后需要向参战玩家推送 CombatEvents，向所有人推送 RoundResult/MatchResult

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Battle.cs`

**Steps:**
1. 在战斗模拟完成后、结算后，添加广播逻辑：
   - 每场战斗结果 → 向参战双方 SendToPlayer M2C_AutoChessCombatEvents（转换 CombatResult.Events）
   - Ghost 对战 → 只给活人发
   - 结算后 → BroadcastToAlive M2C_AutoChessRoundResult（每个玩家的本回合结果）
   - 对局结束 → BroadcastToAll M2C_AutoChessMatchResult（最终排名）
2. 编译检查
3. 提交

---

## 7. 客户端 Handler

- [x] 7.1 创建客户端数据组件 AutoChessClientComponent

**Context:**
- Why: 客户端需要缓存从服务端接收的状态数据，供 UI 层读取

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Client/AutoChess/AutoChessClientComponent.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/AutoChessClientComponentSystem.cs`

**Steps:**
1. 创建 AutoChessClientComponent（ComponentOf: Scene），字段：
   - int CurrentRound, RoundPhase CurrentPhase, long PhaseEndTime
   - int Elixir, int PopCap, int PopUsed
   - List<AutoChessShopOfferProto> ShopOffers
   - List<AutoChessUnitInfoProto> BoardUnits, BenchUnits
   - List<AutoChessSynergyProto> Synergies
   - List<AutoChessPlayerPublicInfoProto> AllPlayers
   - List<AutoChessCombatEventProto> PendingCombatEvents（待播放）
   - AutoChessOpError LastOpError
2. 创建 System 类（IAwake 初始化列表）
3. 编译检查
4. 提交

- [x] 7.2 创建客户端 M2C Handler（全部）

**Context:**
- Depends: 7.1
- Reads: `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/Main/Unit/M2C_CreateUnitsHandler.cs`（参考客户端 Handler 模式）
- Why: 客户端需要处理服务端推送的每种消息

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessPhaseChangeHandler.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessRoundStateHandler.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessOpResultHandler.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessCombatEventsHandler.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessRoundResultHandler.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/Handler/M2C_AutoChessMatchResultHandler.cs`

**Steps:**
1. 每个 Handler 使用 `[MessageHandler(SceneType.StateSync)]`（复用现有 SceneType），继承 `MessageHandler<Scene, M2C_AutoChessXxx>`
2. 每个 Handler 的 Run 方法：获取 AutoChessClientComponent，更新对应字段
   - PhaseChange → 更新 CurrentRound/CurrentPhase/PhaseEndTime
   - RoundState → 全量覆盖所有状态字段
   - OpResult → 如 errorCode=0 应用增量数据，否则设置 LastOpError
   - CombatEvents → 写入 PendingCombatEvents
   - RoundResult → 更新 AllPlayers 中的 hp/isAlive
   - MatchResult → 触发结算事件（发布本地事件供 UI 监听）
3. 编译检查
4. 提交

---

## 8. 客户端操作发送辅助

- [x] 8.1 创建 AutoChessOperationHelper（客户端）

**Context:**
- Depends: 1.4, 7.1
- Why: 封装客户端发送操作消息的接口，供 UI 层调用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Client/AutoChess/AutoChessOperationHelper.cs`

**Steps:**
1. 创建静态类，方法使用 ActorLocationSender 发送 ILocationMessage：
   - `SendBuy(Scene scene, int slotIndex)`
   - `SendSell(Scene scene, int instId)`
   - `SendPlace(Scene scene, int instId, int col, int row)`
   - `SendSwap(Scene scene, int instId1, int instId2)`
   - `SendEnterGame(Scene scene)` → Call（RPC，等待初始状态）
2. 内部通过 scene.GetComponent<PlayerComponent>() 获取 UnitId，构造 ActorLocationSender.Send
3. 编译检查
4. 提交

---

## 9. 集成测试

- [x] 9.1 编写网络层集成测试

**Context:**
- Depends: 2.4, 4.1, 5.1-5.4, 6.1-6.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`
- Why: 验证 PhaseGate、Proto 转换、广播辅助的正确性

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`（追加测试方法）

**Steps:**
1. 添加测试方法：
   - `TestPhaseGate()` — 验证每个操作在每个阶段的门控结果（5 阶段 × 4 操作 = 20 种组合）
   - `TestProtoConversion()` — 构造 CombatEvent / UnitInfo，验证转换到 Proto 字段映射正确
   - `TestBroadcastHelper()` — 创建 MatchRoom + MatchPlayer（不绑定 Unit），验证 MapUnitId=0 时不崩溃（优雅跳过）
2. 运行测试验证通过
3. 编译检查
4. 提交

---

## 10. 编译验证与提交

- [x] 10.1 全量编译 + 最终提交

**Context:**
- Depends: 所有前置任务
- Why: 确保所有代码编译通过，无遗漏

**Steps:**
1. 运行 `dotnet build DotNet/ET.sln` 全量编译
2. 修复任何编译错误
3. 运行所有 AutoChessTestHelper 测试
4. 提交最终状态
