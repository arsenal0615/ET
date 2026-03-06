## 新增需求

### Requirement: 对局创建流程

匹配成功后，匹配服务 SHALL 向 Map 发送 Match2Map_CreateAutoChessGame 请求创建对局。Map 收到后：
1. 创建 MatchRoom + 4 个 MatchPlayer
2. 为每个玩家创建 Map Unit（承载 MailBox GateSession，用于下发消息）
3. 绑定 MatchPlayer 与 Map Unit

#### Scenario: 对局创建
- **WHEN** Map 收到 Match2Map_CreateAutoChessGame
- **THEN** 创建 MatchRoom，4 个 MatchPlayer，4 个 Map Unit，启动 RoundFSM

### Requirement: 玩家进入对局

客户端 SHALL 通过 C2M_AutoChessEnterGame（ILocationRequest）请求进入已创建的对局。服务端返回：
- 对局基本信息（matchId、playerIndex、matchSeed）
- 当前阶段和时间戳
- 玩家初始状态

#### Scenario: 首次进入对局
- **WHEN** 客户端发送 EnterGame 且对局存在
- **THEN** 返回完整的初始状态快照

#### Scenario: 对局不存在
- **WHEN** 客户端发送 EnterGame 但对局已结束
- **THEN** 返回错误码 MATCH_NOT_FOUND

### Requirement: Map Unit 消息转发

每个玩家的 Map Unit SHALL 配置 MailBox 类型为 GateSession，使得服务端可以通过 Unit.SendToClient() 向客户端推送消息。

#### Scenario: 服务端推送消息
- **WHEN** 服务端调用 unit.SendToClient(message)
- **THEN** 消息通过 Gate Session 转发到客户端
