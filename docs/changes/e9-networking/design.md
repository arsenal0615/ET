## 背景

E1-E8 的所有游戏逻辑运行在 Map Fiber 内，通过 PhaseChangedEvent 事件驱动。现在需要将客户端操作接入服务端，并将服务端状态推送给客户端。

ET 框架已有完整的 Client → Gate → Map 消息通道和 Actor Location 机制。本变更利用这些基础设施为 autochess 建立网络层。

## 目标 / 非目标

**目标：**
- 客户端能发送操作意图（Buy/Sell/Place/Swap）到服务端
- 服务端能推送状态（阶段切换、战斗事件、结算）到客户端
- 对局加入流程（匹配后进入 Map 对局）

**非目标：**
- 匹配系统（假设匹配逻辑由外部触发，本变更只处理"匹配成功后"）
- UI 展示（E10）
- 断线重连（E11）
- 客户端乐观更新/预测（首版不做，等 E10 UI 时再加）

## 决策

### 决策 1: 操作消息类型 — ILocationMessage

**选择：** C2M 操作使用 ILocationMessage（单向），不用 ILocationRequest/ILocationResponse。

**考虑过的替代方案：**
- A) ILocationRequest/ILocationResponse（RPC）→ 客户端 await 等结果。但操作结果可能影响其他玩家（如商店刷新），需要广播。RPC 只能返回给发送者，广播逻辑与 RPC 响应重复。
- B) ILocationMessage（Send）→ 服务端处理后通过 MailBox 广播结果给所有相关玩家。

**理由：** 自走棋操作天然需要广播（购买后商店刷新、位置变更等），用 Send + 广播比 RPC + 额外广播更简洁。客户端通过 M2C_AutoChessOpResult 统一接收操作反馈。

### 决策 2: 状态推送 — MailBox GateSession

**选择：** 服务端通过 Map Unit 的 MailBox (GateSession) 向客户端推送消息。

**理由：** 这是 ET 框架标准模式。每个玩家在 Map 上有一个 Unit Entity，挂载 MailBoxComponent(GateSession)。服务端调用 `MapMessageHelper.SendToClient(unit, message)` 即可。已有 login 包中的 Gate 端实现支持。

### 决策 3: 对局实体绑定

**选择：** Map Unit 通过新增 AutoChessComponent 持有 MatchPlayer 引用，实现 Unit ↔ MatchPlayer 双向关联。

**考虑过的替代方案：**
- A) MatchPlayer 直接作为 Actor → 但 MatchPlayer 没有 MailBox，且 ET 标准模式是 Unit 作为 Actor
- B) Unit 上挂 AutoChessComponent(matchRoomId, playerIndex) → 标准做法

**理由：** Unit 是 Map 上的标准 Actor 载体，autochess 逻辑通过 Component 挂载。Handler 收到消息后通过 unit.GetComponent<AutoChessComponent>() 找到 MatchRoom 和 MatchPlayer。

### 决策 4: Proto 数据结构 — 扁平化

**选择：** Proto 中的复杂结构（单位、商店 Offer、羁绊）用扁平的 repeated message 定义，不嵌套过深。

**理由：** Proto 序列化效率和可读性。CombatEvent 直接映射现有 CombatEvent 类的字段。单位状态用 UnitInfoProto 包含核心字段（instId, templateId, star, col, row, location）。

### 决策 5: Handler 与现有 Service 的关系

**选择：** Handler 薄层调用现有 Service（ShopService/UnitService/RosterService），只做校验和消息转换。

**理由：** 所有业务逻辑已在 Service 层实现且经过测试。Handler 只负责：解包消息 → PhaseGate 检查 → 调用 Service → 打包结果 → 广播。

### 决策 6: 广播范围

**选择：**
- 操作结果：只推送给操作发起者（Buy/Sell/Place/Swap 的结果只影响自己）
- 阶段切换/结算：广播给所有存活玩家
- 战斗事件：推送给参战双方
- 对局结束：广播给所有玩家（含已淘汰）

**理由：** 自走棋中玩家看不到对手的实时操作（只看到战斗结果），所以操作结果不需要广播。这减少了不必要的网络流量。

## 框架特定决策

- **程序集分布**：Proto 在 Model/Share、Handler 在 Hotfix/Server（服务端）和 Hotfix/Client（客户端）、AutoChessComponent 在 Model/Server
- **SceneType**：Handler 标记 `SceneType.Map`（服务端），客户端 Handler 标记 `SceneType.AutoChess`（需新增 SceneType）或复用 `SceneType.StateSync`
- **命名空间**：服务端 `ET.Server`，客户端 `ET.Client`，共享 `ET`
- **消息处理基类**：服务端用 `MessageLocationHandler<Unit, TMessage>`，客户端用 `MessageHandler<Scene, TMessage>`

## 风险 / 权衡

| 风险 | 缓解措施 |
|------|----------|
| CombatEvent 序列化体积大（30秒战斗可能数百事件） | 首版不压缩，E10 UI 时评估是否需要二进制压缩 |
| 操作无 RPC 回执，客户端不知道服务端是否收到 | OpResult 作为异步回执；E11 断线检测兜底 |
| 客户端 SceneType 选择 | 优先复用 StateSync，如果冲突再新增 AutoChess |
| Proto Opcode 冲突 | 选择 12001/22001 段，远离现有的 1000/11001/20001/21001 |
