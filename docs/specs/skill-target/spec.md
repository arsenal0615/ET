## 新增需求

### Requirement: 目标选取算法

系统 SHALL 为每种 SkillTargetType 提供确定性的目标选取方法，接收施法者、战场状态和技能参数，返回目标单位列表。

#### Scenario: Self 选取

- **WHEN** TargetType=Self
- **THEN** 返回施法者自身

#### Scenario: NearestEnemy 选取

- **WHEN** TargetType=NearestEnemy
- **THEN** 返回六边形距离最近的存活敌方单位
- **WHEN** 多个敌方单位距离相同
- **THEN** 按 instId 升序选取第一个（确定性 tiebreaker）

#### Scenario: FarthestInRadius 选取

- **WHEN** TargetType=FarthestInRadius，半径参数为 R 格
- **THEN** 返回半径 R 内六边形距离最远的存活敌方单位
- **WHEN** 多个敌方单位距离相同
- **THEN** 按 instId 升序选取第一个
- **WHEN** 半径内无敌方单位
- **THEN** 返回空列表，技能不执行效果

#### Scenario: FarthestInRange 选取

- **WHEN** TargetType=FarthestInRange
- **THEN** 返回单位射程（UnitTemplateDef.Range）内六边形距离最远的存活敌方单位
- **WHEN** 射程内无敌方单位
- **THEN** 返回空列表

#### Scenario: LowestHp 选取

- **WHEN** TargetType=LowestHp
- **THEN** 返回当前 HP 最低的存活敌方单位
- **WHEN** 多个敌方单位 HP 相同
- **THEN** 按 instId 升序选取第一个

#### Scenario: MultiTargets 选取

- **WHEN** TargetType=MultiTargets，目标数量参数为 N
- **THEN** 返回最多 N 个存活敌方单位，按六边形距离最近优先
- **WHEN** 距离相同
- **THEN** 按 instId 升序排列
- **WHEN** 存活敌方单位少于 N
- **THEN** 返回全部存活敌方单位

#### Scenario: ClusterLargest 选取

- **WHEN** TargetType=ClusterLargest，半径参数为 R 格
- **THEN** 遍历所有存活敌方单位位置，计算以该位置为中心半径 R 内的敌方单位数量，返回计数最多的中心位置及其范围内所有单位
- **WHEN** 多个位置密集度相同
- **THEN** 按中心单位坐标（row ASC → col ASC → instId ASC）选取

#### Scenario: AreaRadius 选取

- **WHEN** TargetType=AreaRadius，中心为施法者位置，半径参数为 R 格
- **THEN** 返回半径 R 内所有存活敌方单位

#### Scenario: LinePierce 选取

- **WHEN** TargetType=LinePierce，距离参数为 D 格
- **THEN** 从施法者朝当前目标方向，沿直线选取 D 格内所有存活敌方单位

#### Scenario: Cone 选取

- **WHEN** TargetType=Cone
- **THEN** 从施法者朝当前面向方向，选取锥形区域内所有存活敌方单位

### Requirement: 六边形距离计算

系统 MUST 使用 axial 坐标系计算六边形距离：`distance = (|q1-q2| + |r1-r2| + |q1+r1-q2-r2|) / 2`。棋盘使用 odd-r offset 坐标系，选取前 MUST 先转换为 axial 坐标。

#### Scenario: 坐标转换正确性

- **WHEN** 使用 odd-r offset 坐标 (col, row) 进行距离计算
- **THEN** 先转换为 axial (q, r)，再计算距离
- **THEN** 相邻格距离 = 1，对角格距离 = 2

### Requirement: 隐身单位排除

目标选取 MUST 排除处于隐身不可选状态的敌方单位（EffectType=Invisibility 激活中）。

#### Scenario: 隐身排除

- **WHEN** 敌方单位处于隐身状态
- **THEN** 该单位不出现在任何 TargetType 的选取结果中（Self 除外）
