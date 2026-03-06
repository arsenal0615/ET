## 新增需求

### Requirement: UnitInfo to CombatUnitState Conversion
战斗初始化 SHALL 将每个玩家的棋盘 UnitInfo 转换为 CombatUnitState，应用星级倍率和羁绊修改器。

#### Scenario: Star multiplier applied
- **WHEN** 一个 2★ 单位（baseHp=100, baseAtk=50）初始化
- **THEN** CombatUnitState.MaxHp = 100×2=200, Atk = 50×2=100

#### Scenario: TraitSnapshot modifiers applied
- **WHEN** 单位有 Brawler level=1（HpMultiplier=1.5）
- **THEN** MaxHp = baseHp × starMultiplier × HpMultiplier

#### Scenario: Superstar mana precharge
- **WHEN** 单位有 Superstar level=1（ManaPrecharge=33）
- **THEN** CombatUnitState.Mana 初始化为 33

### Requirement: Board Mirroring
右方（R）玩家的单位 MUST 做 Y 轴镜像（y' = 4 - y），使双方从棋盘两端对撞。

#### Scenario: Mirror right side units
- **WHEN** R 方单位原始位置 (3, 0)
- **THEN** 战斗中位置为 (3, 4)

### Requirement: Side Assignment
配对中 playerId 较小者为 L（下方），较大者为 R（上方）。

#### Scenario: Side determination
- **WHEN** playerA(id=1) vs playerB(id=3) 配对
- **THEN** playerA 为 L，playerB 为 R
