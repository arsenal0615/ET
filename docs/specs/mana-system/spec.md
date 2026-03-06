## 新增需求

### Requirement: 法力值追踪

系统 SHALL 为每个战斗单位维护法力值（mana），范围 [0, 100]。

#### Scenario: 初始法力值

- **WHEN** 战斗单位进入战场
- **THEN** 法力值初始化为 0

#### Scenario: Superstar 预充

- **WHEN** 单位拥有 Superstar 羁绊效果
- **THEN** 法力值初始化为 TraitSnapshot.UnitBattleModifiers 中指定的预充值（level 1 = 33, level 2 = 66）

### Requirement: 法力获取

系统 SHALL 在以下事件发生时增加法力值。

#### Scenario: 普攻获取法力

- **WHEN** 单位成功执行一次普攻命中
- **THEN** 法力值增加 ManaGainOnAttack（默认 10），clamp 到 [0, 100]

#### Scenario: 受击获取法力

- **WHEN** 单位受到伤害（包括技能伤害和普攻伤害）
- **THEN** 法力值增加 ManaGainOnHit（默认 6），clamp 到 [0, 100]

#### Scenario: 眩晕期间受击

- **WHEN** 单位处于眩晕状态且受到伤害
- **THEN** 仍然获取法力值（眩晕不阻止法力获取）

### Requirement: 法力触发技能

系统 SHALL 在法力值达到 100 时触发 ManaFull 类技能。

#### Scenario: 法力满触发

- **WHEN** 法力值累积达到 100
- **THEN** 触发该单位的 ManaFull 技能
- **THEN** 法力值清零为 0

#### Scenario: Superstar 连发

- **WHEN** 单位拥有 Superstar 羁绊的连发效果，技能释放后
- **THEN** 有 50% 概率立即再次释放（使用战斗 PRNG）
- **THEN** 连发最多 4 次（含首次释放共 5 次）
- **THEN** 连发期间不消耗额外法力，每次连发后重新判定概率

### Requirement: 法力值不溢出

法力值 MUST clamp 到 [0, 100]，超过 100 的增量丢弃。

#### Scenario: 法力溢出

- **WHEN** 当前法力 = 95，受到伤害获取 +6
- **THEN** 法力值 = 100（不是 101），触发 ManaFull
