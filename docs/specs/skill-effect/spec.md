## 新增需求

### Requirement: 效果应用器

系统 SHALL 为每种 SkillEffectType 提供效果应用方法，接收施法者、目标列表、效果参数和战斗上下文，修改目标运行时状态并返回效果结果（供事件流使用）。

#### Scenario: Damage 效果

- **WHEN** EffectType=Damage，Value1=倍率
- **THEN** 对每个目标造成 `floor(caster.atk * Value1 * damageMultiplier)` 点伤害
- **THEN** 伤害计算 MUST 考虑 UnitBattleModifiers（DamageMultiplier、DamageReduction）
- **THEN** 护盾 HP 优先扣除，剩余扣除本体 HP
- **THEN** HP 降至 0 的单位标记为待死亡

#### Scenario: Stun 效果

- **WHEN** EffectType=Stun，Duration=N 秒
- **THEN** 目标进入眩晕状态，持续 N * tickRate ticks
- **THEN** 眩晕期间单位不执行普攻、不移动、不触发 Interval/AttackTrait 技能
- **THEN** 眩晕不阻止 OnDeath/OnHpBelow 触发

#### Scenario: Knockback 效果

- **WHEN** EffectType=Knockback，Value1=击退距离（格数）
- **THEN** 目标沿施法者→目标方向后退 Value1 格
- **WHEN** 击退路径被其他单位或棋盘边界阻挡
- **THEN** 目标停在被阻挡位置前一格

#### Scenario: Invisibility 效果

- **WHEN** EffectType=Invisibility，Duration=N 秒
- **THEN** 施法者进入隐身不可选状态，持续 N * tickRate ticks
- **THEN** 隐身期间不被任何 TargetCell 选中（Self 除外）
- **THEN** 隐身单位仍可正常移动和攻击
- **WHEN** Frenzy Time 激活
- **THEN** 隐身持续时间减半（Duration * 0.5）

#### Scenario: Summon 效果

- **WHEN** EffectType=Summon，Value1=召唤数量，Value2=召唤物星级
- **THEN** 在施法者附近的空格生成 Value1 个召唤单位
- **THEN** 召唤物 MUST 标记 isEffectiveForDamageCount=false（不计入有效单位数）
- **THEN** 召唤物继承施法者的阵营（Left/Right）
- **WHEN** 附近无空格
- **THEN** 召唤数量按可用空格减少

#### Scenario: Clone 效果

- **WHEN** EffectType=Clone
- **THEN** 在施法者附近空格生成一个克隆体
- **THEN** 克隆体 HP = 施法者当前 HP * Value2（克隆体血量比例）
- **THEN** 施法者 HP 减少至 HP * Value1（施法者剩余比例）
- **THEN** 克隆体 MUST 标记 isEffectiveForDamageCount=false

#### Scenario: Reflect 效果

- **WHEN** EffectType=Reflect，Value1=减伤比例，Duration=N 秒
- **THEN** 施法者获得反伤护盾，持续 N * tickRate ticks
- **THEN** 护盾期间受到的伤害减少 Value1 比例
- **THEN** 减少的伤害量反弹给攻击者

#### Scenario: HealOverTime 效果

- **WHEN** EffectType=HealOverTime，Value1=每次治疗百分比，Duration=N 秒
- **THEN** 施法者每秒恢复 maxHp * Value1 的 HP，持续 N 秒
- **THEN** HP 不超过 maxHp

#### Scenario: Projectile 效果

- **WHEN** EffectType=Projectile，Value1=投射物数量
- **THEN** 向 Value1 个不同目标各发射一枚投射物
- **THEN** 每枚投射物造成独立伤害（使用 Damage 效果的计算公式）
- **THEN** 目标选取按距离最远优先（确定性 tiebreaker: instId ASC）

#### Scenario: SpeedBuff 效果

- **WHEN** EffectType=SpeedBuff，Value1=攻速倍率，Duration=N 秒
- **THEN** 目标攻速乘以 Value1，持续 N * tickRate ticks

### Requirement: 效果确定性

所有效果计算 MUST 使用整数运算或 floor 取整，不得引入浮点精度差异。

#### Scenario: 伤害取整

- **WHEN** 伤害计算结果为小数
- **THEN** MUST 使用 `floor()` 向下取整，最终伤害为整数

### Requirement: 效果结果结构

每次效果应用 MUST 返回结构化结果，包含：受影响单位 instId、效果类型、数值变化（伤害值/治疗值/状态变更），供 E7 生成 CombatEvent。

#### Scenario: 效果结果输出

- **WHEN** Damage 效果对目标造成 50 点伤害
- **THEN** 返回 SkillEffectResult{targetInstId, effectType=Damage, value=50}
