## 新增需求

### Requirement: 客户端消息接收

客户端 SHALL 为每种 M2C 消息实现对应的 MessageHandler（SceneType.AutoChess 或复用 StateSync）：
- M2C_AutoChessPhaseChange → 更新本地阶段状态
- M2C_AutoChessRoundState → 刷新完整本地状态
- M2C_AutoChessOpResult → 处理操作反馈
- M2C_AutoChessCombatEvents → 缓存战斗事件供播放
- M2C_AutoChessRoundResult → 更新 HP 和淘汰状态
- M2C_AutoChessMatchResult → 触发结算界面

#### Scenario: 接收阶段切换
- **WHEN** 客户端收到 PhaseChange
- **THEN** 更新本地 currentPhase 和 phaseEndTime

#### Scenario: 接收完整状态
- **WHEN** 客户端收到 RoundState
- **THEN** 本地状态完全替换为服务端快照（无合并，全量覆盖）

### Requirement: 操作反馈处理

客户端 SHALL 根据 OpResult 的 errorCode 执行不同逻辑：
- errorCode = 0：应用增量更新到本地状态
- errorCode != 0：显示错误提示，回滚乐观更新（如有）

#### Scenario: 操作成功
- **WHEN** 收到 errorCode=0 的 OpResult
- **THEN** 使用 OpResult 中的增量数据更新本地状态

#### Scenario: 操作失败回滚
- **WHEN** 收到 errorCode!=0 的 OpResult
- **THEN** 回滚之前的乐观更新，显示错误码对应的文案

### Requirement: 战斗事件缓存

客户端 SHALL 缓存收到的 CombatEvents，供 UI 按 tick 时间线播放。

#### Scenario: 事件流缓存
- **WHEN** 收到 M2C_AutoChessCombatEvents
- **THEN** 将事件列表存入本地 CombatReplay 组件，等待 UI 播放

### Requirement: 客户端操作发送

客户端 SHALL 提供统一的操作发送接口，封装 ILocationMessage 的寻址：
- SendBuy(slotIndex)
- SendSell(instId)
- SendPlace(instId, col, row)
- SendSwap(instId1, instId2)

#### Scenario: 发送购买操作
- **WHEN** 玩家点击商店槽位
- **THEN** 客户端发送 C2M_AutoChessBuy（slotIndex）到 Map 上的 Unit
