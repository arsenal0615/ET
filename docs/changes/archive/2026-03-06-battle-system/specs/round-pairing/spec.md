## 新增需求

### Requirement: Deterministic Pairing
每回合 SHALL 用 Fisher-Yates 洗牌（pairSeed）确定性配对存活玩家。

#### Scenario: 4 players alive
- **WHEN** 4 名玩家存活
- **THEN** 洗牌后产生 2 场 1v1

#### Scenario: 3 players alive — Ghost
- **WHEN** 3 名玩家存活
- **THEN** 1 场 1v1 + 1 场 vs Ghost

#### Scenario: 2 players alive
- **WHEN** 2 名玩家存活
- **THEN** 固定对打

### Requirement: Ghost Definition
Ghost SHALL 使用最近被淘汰玩家的 PreBattle 阵容快照。

#### Scenario: Ghost selection
- **WHEN** 玩家 C 在 Round 5 被淘汰
- **THEN** Round 6 的 Ghost 使用玩家 C 的 Round 5 PreBattle 快照

#### Scenario: Multiple eliminated same round
- **WHEN** 同回合多人淘汰
- **THEN** Ghost 选 playerId 字典序最大者

### Requirement: L/R Side Assignment
配对内 playerId 较小者 MUST 为 L。

#### Scenario: Side assignment
- **WHEN** player(id=2) vs player(id=5)
- **THEN** player 2 为 L，player 5 为 R
