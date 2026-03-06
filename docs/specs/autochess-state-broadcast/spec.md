## 新增需求

### Requirement: 阶段切换广播

当 RoundFSM 切换阶段时，服务端 SHALL 向所有存活玩家广播 M2C_AutoChessPhaseChange。

广播内容：round、newPhase、phaseEndTime（服务端时间戳）。

#### Scenario: 部署阶段开始
- **WHEN** 进入 Deployment 阶段
- **THEN** 所有存活玩家收到 PhaseChange（round=N, newPhase=Deployment, phaseEndTime=now+20s）

#### Scenario: 已淘汰玩家不收广播
- **WHEN** 玩家 hp<=0 已淘汰
- **THEN** 不收到后续阶段切换广播

### Requirement: 回合状态快照

RoundStart 阶段完成所有处理（发钱、刷商店、首回合赠送等）后，服务端 SHALL 向每个存活玩家推送该玩家的完整状态快照 M2C_AutoChessRoundState。

快照内容：round、elixir、popCap、popUsed、shopOffers（3 槽）、boardUnits、benchUnits、activeSynergies、allPlayersPublicInfo（所有玩家的 hp/isAlive/rank）。

#### Scenario: 快照包含商店信息
- **WHEN** 玩家收到 RoundState
- **THEN** shopOffers 包含 3 个 slot 的 templateId 和 cost（空槽为 -1）

#### Scenario: 快照包含其他玩家 HP
- **WHEN** 玩家收到 RoundState
- **THEN** allPlayersPublicInfo 包含所有 4 位玩家的 hp 和 isAlive 状态

### Requirement: 操作后增量更新

操作成功后，服务端 SHALL 通过 M2C_AutoChessOpResult 携带增量数据：
- Buy 成功：新单位信息 + 更新后的 elixir + 新商店 offers
- Sell 成功：被移除的 instId + 更新后的 elixir
- Place 成功：单位 instId + 新位置 + 更新后的 popUsed
- Swap 成功：两个单位 instId + 各自新位置

#### Scenario: 购买后增量更新
- **WHEN** Buy 操作成功
- **THEN** OpResult 包含新单位的完整信息和刷新后的 3 个商店槽位

### Requirement: 战斗事件流广播

战斗模拟完成后，服务端 SHALL 向参战双方推送 M2C_AutoChessCombatEvents。

事件流按 tick 排序，包含战斗中发生的所有事件（SPAWN/MOVE/ATTACK/CAST/DAMAGE/HEAL/BUFF/DEATH/FRENZY/ROUND_END）。

#### Scenario: 正常对战事件推送
- **WHEN** 玩家 A vs 玩家 B 战斗结束
- **THEN** A 和 B 各收到相同的 CombatEvents 列表

#### Scenario: Ghost 对战事件推送
- **WHEN** 玩家 A vs Ghost（已淘汰的 B 的快照）
- **THEN** 只有 A 收到 CombatEvents（B 已淘汰不在线）

### Requirement: 回合结算广播

RoundEnd 阶段，服务端 SHALL 向所有存活玩家广播 M2C_AutoChessRoundResult：
- 每个玩家的本回合结果（opponent、win/lose/draw、hpChange、hpAfter）
- 本回合淘汰的玩家列表

#### Scenario: 结算后玩家淘汰
- **WHEN** 玩家 hp 降到 0
- **THEN** 该玩家收到最后一次 RoundResult（含自己的淘汰信息），之后不再收到消息

### Requirement: 对局结束广播

当对局结束（仅剩 1 人或所有人淘汰）时，服务端 SHALL 向所有玩家（包括已淘汰的）广播 M2C_AutoChessMatchResult：
- 最终排名（1-4 名，含 playerId 和 rank）

#### Scenario: 对局正常结束
- **WHEN** 只剩 1 人存活
- **THEN** 所有 4 个玩家都收到 MatchResult
