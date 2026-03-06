## 新增需求

### Requirement: HP Deduction
败者 SHALL 扣血 = 对面剩余有效单位数 + 1。

#### Scenario: Normal loss
- **WHEN** L 方胜利，剩余有效单位 3 个
- **THEN** R 方扣血 3 + 1 = 4

#### Scenario: Summon/Clone not counted
- **WHEN** 胜方剩余 2 个普通单位 + 1 个 Skeleton（isEffectiveForDamageCount=false）
- **THEN** 败方扣血 2 + 1 = 3

### Requirement: Draw HP Deduction
平局时双方 SHALL 各扣 1 HP。

#### Scenario: Timeout draw
- **WHEN** 战斗超时（DRAW, timeUp=true）
- **THEN** 双方各扣 1 HP

#### Scenario: Mutual destruction draw
- **WHEN** 同归于尽（DRAW, timeUp=false）
- **THEN** 双方各扣 1 HP

### Requirement: Win Against Ghost
赢 Ghost SHALL 不扣血。

#### Scenario: Beat ghost
- **WHEN** 存活玩家胜 Ghost
- **THEN** 无人扣血

#### Scenario: Lose to ghost
- **WHEN** 存活玩家负 Ghost
- **THEN** 该玩家正常扣血

### Requirement: Elimination
HP <= 0 的玩家 SHALL 被淘汰。

#### Scenario: Elimination triggers card pool recovery
- **WHEN** 玩家被淘汰
- **THEN** 调用 MatchRoomSystem.EliminatePlayer（含 Offer 归还 + 卡池回收）

### Requirement: Same-Round Elimination Tiebreaker
同回合多人淘汰排名 MUST 按：hp 更高 → hpBefore 更高 → 剩余有效单位更多 → playerId 更小。

#### Scenario: Tiebreaker ranking
- **WHEN** A(hp=-1, hpBefore=2) 和 B(hp=-2, hpBefore=3) 同回合淘汰
- **THEN** A 排名优于 B（hp 更高）
