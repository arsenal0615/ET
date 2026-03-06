## 新增需求

### Requirement: Ace Captain Bonus
Ace 羁绊 SHALL 选出一名队长（最高星级→最高费用→位置→instId），队长获得额外伤害和吸血。

#### Scenario: Ace level 1
- **WHEN** Ace level=1
- **THEN** 队长 damageMultiplier += 0.40, 每次造成伤害回血 damage×0.40

#### Scenario: Ace level 2
- **WHEN** Ace level=2
- **THEN** 队长 damageMultiplier += 0.70, 吸血 70%

### Requirement: Clan Low HP Burst
Clan 单位 HP <= maxHp×50% 时 SHALL 触发治疗+攻速爆发，每单位每战最多一次。

#### Scenario: Clan trigger
- **WHEN** Clan 单位 HP 从 60% 降到 45%
- **THEN** 治疗 maxHp×35%（level=1），攻速+35% 持续 5 秒

### Requirement: P.E.K.K.A Kill Reward
P.E.K.K.A 单位击杀后 SHALL 回血+增伤。

#### Scenario: Kill reward
- **WHEN** P.E.K.K.A 单位击杀一个敌人
- **THEN** 回血 maxHp×60%, damageMultiplier += 0.40（持续到战斗结束）

### Requirement: Ranger Attack Speed Stacking
Ranger 单位每次普攻 SHALL 叠加攻速。

#### Scenario: Ranger stacking level 1
- **WHEN** Ranger level=1 单位第 3 次普攻
- **THEN** 攻速已叠加 +30%（3×10%），上限 6 层 = +60%

### Requirement: Undead Kill Bonus
击杀被亡灵诅咒的目标后，该方所有 Undead 单位 SHALL 获得 +30% 额外伤害（一次性）。

#### Scenario: Undead kill trigger
- **WHEN** L 方单位击杀一个被 L 方亡灵诅咒的敌人
- **THEN** L 方所有 Undead 单位 damageMultiplier += 0.30
