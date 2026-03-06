## 新增需求

### Requirement: CombatEvent Stream
战斗模拟 SHALL 输出完整的 CombatEvent 事件流，每条事件有严格递增的序号 i 和非递减的 tick。

#### Scenario: Event sequence numbering
- **WHEN** 战斗产生 N 条事件
- **THEN** i 从 0 到 N-1 严格递增

### Requirement: Event Types
MUST 支持 12 种事件类型：ROUND_START, SPAWN, MOVE, ATTACK, DAMAGE, HEAL, CAST, BUFF_ADD, BUFF_REMOVE, DEATH, FRENZY_START, ROUND_END。

#### Scenario: SPAWN events at tick 0
- **WHEN** 战斗开始
- **THEN** 为每个参战单位生成 SPAWN 事件（含 instId, templateId, star, side, pos, hp, maxHp）

#### Scenario: ROUND_END event
- **WHEN** 战斗结束
- **THEN** ROUND_END 包含 winner(L/R/DRAW), timeUp, leftAliveEffective, rightAliveEffective

### Requirement: Same-Tick Event Ordering
同一 tick 内事件排序 MUST 为：MOVE → ATTACK → CAST → DAMAGE/HEAL/BUFF_ADD/BUFF_REMOVE → DEATH。

#### Scenario: Death after damage in same tick
- **WHEN** tick=50 内单位受到致命伤害
- **THEN** DAMAGE 事件在 DEATH 事件之前

#### Scenario: Multiple deaths same tick
- **WHEN** 同一 tick 多个单位死亡
- **THEN** DEATH 事件按 instId 升序排列
