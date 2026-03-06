## 新增需求

### Requirement: Deterministic Tick Loop
战斗模拟器 SHALL 以固定 20 ticks/s（dt=50ms）驱动确定性战斗，最大 600 ticks（30秒）。

#### Scenario: Normal battle completes before timeout
- **WHEN** 一方所有单位死亡
- **THEN** 立即结束循环，输出 ROUND_END 事件（winner=L|R）

#### Scenario: Battle reaches timeout
- **WHEN** tick 达到 600 且双方仍有存活单位
- **THEN** 结束循环，输出 ROUND_END（winner=DRAW, timeUp=true）

#### Scenario: Mutual destruction same tick
- **WHEN** 同一 tick 内双方最后单位同时死亡
- **THEN** 输出 ROUND_END（winner=DRAW, timeUp=false）

### Requirement: Fixed Tick Processing Order
每 tick 内处理顺序 MUST 为：1.到期计划任务 → 2.Buff/状态到期 → 3.单位行动（instId 升序） → 4.死亡清理 → 5.Frenzy 检测。

#### Scenario: Unit action order
- **WHEN** 多个单位在同一 tick 行动
- **THEN** 按 instId 升序逐个处理

#### Scenario: Death collected after all actions
- **WHEN** 某单位在 tick 中途 HP<=0
- **THEN** 该单位本 tick 内不立即移除，死亡在步骤4统一清理

### Requirement: All Randomness From Combat Seed
战斗模拟的所有随机 MUST 来自 DeriveSubSeed(PrngPurpose.Combat, round) 派生的子 PRNG。

#### Scenario: Same seed produces identical result
- **WHEN** 使用相同 matchSeed、round、双方阵容
- **THEN** 运行 100 次，CombatEvent 序列逐条一致
