## 新增需求

### Requirement: 触发条件判定

系统 SHALL 为每种 SkillTriggerType 提供确定性的条件判定方法，接收战斗单位运行时状态和触发事件类型，返回 bool 表示是否应触发技能。

#### Scenario: CombatStart 触发

- **WHEN** 战斗开始（tick=0），单位拥有 TriggerType=CombatStart 的技能
- **THEN** 该技能触发一次，且仅触发一次

#### Scenario: Interval 触发

- **WHEN** 单位拥有 TriggerType=Interval 的技能，IntervalSeconds=N
- **THEN** 每隔 N 秒（N * tickRate ticks）触发一次，首次触发在 N 秒后

#### Scenario: OnHitCount 触发

- **WHEN** 单位普攻命中累计达到 HitCountRequired 次
- **THEN** 技能触发一次，命中计数器重置为 0，继续累计下一轮

#### Scenario: OnKill 触发

- **WHEN** 单位击杀一个敌方单位
- **THEN** 技能触发一次
- **WHEN** ChainLimit > 0 且已触发次数达到 ChainLimit
- **THEN** 不再触发

#### Scenario: OnHpBelow 触发

- **WHEN** 单位当前 HP 首次降至 maxHp * HpThresholdPct / 100 以下
- **THEN** 技能触发一次
- **WHEN** HP 恢复后再次降至阈值以下
- **THEN** 不再触发（单场战斗仅触发一次）

#### Scenario: ManaFull 触发

- **WHEN** 单位法力值达到 100（manaMax）
- **THEN** 技能触发一次，法力值清零

#### Scenario: AttackTrait 触发

- **WHEN** 单位执行普攻
- **THEN** 技能效果替代或增强普攻（每次普攻都触发，无冷却）

#### Scenario: OnDeath 触发

- **WHEN** 单位死亡
- **THEN** 技能触发一次（在死亡清理之前执行效果）

### Requirement: 触发器状态追踪

系统 MUST 为每个战斗单位维护触发器相关的运行时状态，包括：
- Interval 计时器（上次触发的 tick）
- OnHitCount 命中计数器
- OnKill 已触发次数（用于 ChainLimit）
- OnHpBelow 是否已触发标记
- ManaFull 已由 mana-system 处理

#### Scenario: 触发器状态初始化

- **WHEN** 战斗单位进入战场（包括被召唤的单位）
- **THEN** 所有触发器状态重置为初始值（计数器=0，标记=false，计时器=当前tick）

#### Scenario: 触发器确定性

- **WHEN** 相同的战斗状态输入和相同的触发事件
- **THEN** 触发判定结果 MUST 完全相同（无随机性）
