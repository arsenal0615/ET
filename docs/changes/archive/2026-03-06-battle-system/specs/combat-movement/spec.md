## 新增需求

### Requirement: Greedy Hex Movement
无目标在射程内时，单位 SHALL 向最近敌人方向贪心移动一格。

#### Scenario: Move towards nearest enemy
- **WHEN** 单位射程内无敌人
- **THEN** 选择 6 邻居中能使距离严格减少的候选格，优先距离减少最多

#### Scenario: Tiebreaker on equal distance reduction
- **WHEN** 多个邻居格距离减少量相同
- **THEN** 按固定方向序列选取：右→右下→左下→左→左上→右上

#### Scenario: All neighbors blocked
- **WHEN** 6 个邻居均已被占用或不能减少距离
- **THEN** 原地不动

### Requirement: Move Speed Interval
单位移动间隔 MUST 由 moveSpeed 决定：moveIntervalTicks = ceil(1/moveSpeed × 20)。

#### Scenario: Move cooldown
- **WHEN** moveSpeed=2, 上次移动在 tick=10
- **THEN** 下次可移动 tick = 10 + ceil(20/2) = 20

### Requirement: Stunned Units Cannot Move
眩晕状态的单位 MUST NOT 移动或攻击。

#### Scenario: Stunned skip action
- **WHEN** 单位有 Stun buff 且 RemainingTicks > 0
- **THEN** 跳过该单位的移动和攻击
