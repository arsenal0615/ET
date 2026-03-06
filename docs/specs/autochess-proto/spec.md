## 新增需求

### Requirement: Proto 文件结构

autochess 包 SHALL 定义两个 Proto 文件：
- `AutoChessOuter_C_12001.proto` — 客户端↔服务端消息（C2M + M2C）
- `AutoChessInner_S_22001.proto` — 服务端间消息（如匹配→Map 创建对局）

起始 Opcode 为 12001（Outer）和 22001（Inner），避免与现有消息冲突。

#### Scenario: Proto 文件位置
- **WHEN** 查看 `Packages/cn.etetet.autochess/Proto/` 目录
- **THEN** 存在 AutoChessOuter_C_12001.proto 和 AutoChessInner_S_22001.proto

### Requirement: 操作意图消息（C2M）

客户端操作意图消息 SHALL 使用 ILocationMessage（单向，无需 Response），前缀 `C2M_AutoChess`：
- `C2M_AutoChessBuy` — 购买（slotIndex）
- `C2M_AutoChessSell` — 出售（instId）
- `C2M_AutoChessPlace` — 放置到棋盘（instId, col, row）
- `C2M_AutoChessSwap` — 交换位置（instId1, instId2）

每条消息 SHALL 包含 `int32 RpcId = 90` 以支持 ILocationMessage 寻址。

#### Scenario: 购买消息字段完整
- **WHEN** 客户端发送 C2M_AutoChessBuy
- **THEN** 消息包含 slotIndex（0-2）

#### Scenario: 出售消息字段完整
- **WHEN** 客户端发送 C2M_AutoChessSell
- **THEN** 消息包含 instId（要出售的单位实例 ID）

#### Scenario: 放置消息字段完整
- **WHEN** 客户端发送 C2M_AutoChessPlace
- **THEN** 消息包含 instId、col、row

#### Scenario: 交换消息字段完整
- **WHEN** 客户端发送 C2M_AutoChessSwap
- **THEN** 消息包含 instId1 和 instId2

### Requirement: 操作结果消息（M2C）

服务端操作反馈 SHALL 使用 IMessage（单向推送），前缀 `M2C_AutoChess`：
- `M2C_AutoChessOpResult` — 操作结果（opType, errorCode, 可选 payload）

errorCode = 0 表示成功，非 0 为错误码。

#### Scenario: 操作成功反馈
- **WHEN** 操作校验通过
- **THEN** 推送 M2C_AutoChessOpResult，errorCode = 0

#### Scenario: 操作失败反馈
- **WHEN** 操作校验失败（如圣水不足）
- **THEN** 推送 M2C_AutoChessOpResult，errorCode 为对应错误码

### Requirement: 状态推送消息（M2C）

服务端 SHALL 定义以下主动推送消息：
- `M2C_AutoChessPhaseChange` — 阶段切换（round, newPhase, phaseEndTime）
- `M2C_AutoChessRoundState` — 回合开始完整状态快照（elixir, shopOffers, roster, synergies, allPlayersHp）
- `M2C_AutoChessCombatEvents` — 战斗事件流（repeated CombatEventProto events）
- `M2C_AutoChessRoundResult` — 回合结算（repeated PlayerRoundResult results）
- `M2C_AutoChessMatchResult` — 对局结束（repeated PlayerFinalResult rankings）

#### Scenario: 阶段切换推送
- **WHEN** RoundFSM 切换阶段
- **THEN** 所有存活玩家收到 M2C_AutoChessPhaseChange

#### Scenario: 回合开始状态快照
- **WHEN** 进入 RoundStart 阶段
- **THEN** 每个玩家收到自己的完整状态快照（M2C_AutoChessRoundState）

#### Scenario: 战斗事件流推送
- **WHEN** 战斗模拟完成
- **THEN** 参战双方收到对应的 CombatEvent 列表

#### Scenario: 对局结束推送
- **WHEN** 只剩 1 人存活
- **THEN** 所有玩家收到 M2C_AutoChessMatchResult 含最终排名

### Requirement: 服务端间消息（Inner）

服务端间 SHALL 定义：
- `Match2Map_CreateAutoChessGame` (IRequest) — 匹配服务通知 Map 创建对局
- `Map2Match_AutoChessGameOver` (IMessage) — Map 通知匹配服务对局结束

#### Scenario: 创建对局消息
- **WHEN** 匹配成功（4 人齐）
- **THEN** 发送 Match2Map_CreateAutoChessGame 包含 4 个 playerId 和 matchSeed
