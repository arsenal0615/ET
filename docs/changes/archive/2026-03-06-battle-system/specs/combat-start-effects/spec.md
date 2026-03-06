## 新增需求

### Requirement: Assassin Jump to Backline
开战（tick=0）时，拥有 Assassin 羁绊的单位 SHALL 跳到敌方后排。

#### Scenario: Normal assassin jump
- **WHEN** 刺客为 L 方，敌方 R 方后排行 y∈{0,1}
- **THEN** 选择后排最近敌人，落点为 (target.x, target.y+1)

#### Scenario: Preferred landing occupied
- **WHEN** 期望落点被占
- **THEN** 先在同行找最近空格（x 升序 tiebreak），再找目标 6 邻居空格，全满则不跳

#### Scenario: Assassin order
- **WHEN** 多个刺客需要跳
- **THEN** 按 instId 升序逐个处理

### Requirement: Undead Curse
开战（tick=0，刺客跳后）时，Undead 羁绊 SHALL 诅咒敌方高血单位。

#### Scenario: Undead level 1
- **WHEN** Undead level=1
- **THEN** 诅咒敌方 maxHp 最高的 2 个单位，maxHp × 0.75

#### Scenario: Undead level 2
- **WHEN** Undead level=2
- **THEN** 诅咒敌方 maxHp 最高的 3 个单位，maxHp × 0.50

#### Scenario: Curse target tiebreaker
- **WHEN** 多个敌人 maxHp 相同
- **THEN** 按 instId 升序选取
