## 新增需求

### Requirement: 技能执行管线

系统 SHALL 提供 SkillExecutor 静态服务类，编排 Trigger→Target→Effect 的完整技能执行流程，供 E7 战斗模拟器在每个 Tick 中调用。

#### Scenario: 标准执行流程

- **WHEN** E7 调用 SkillExecutor 检查某单位在某触发事件下是否应执行技能
- **THEN** 按以下顺序执行：
  1. 查找单位的 SkillDefData（通过 UnitTemplateDef.SkillDefId）
  2. 调用触发判定器检查条件
  3. 如果触发，调用目标选取器获取目标列表
  4. 如果目标列表非空，调用效果应用器执行所有 EffectData
  5. 返回 SkillExecutionResult（包含所有效果结果）

#### Scenario: 技能未触发

- **WHEN** 触发条件不满足
- **THEN** 返回 SkillExecutionResult.NotTriggered，不执行后续步骤

#### Scenario: 无有效目标

- **WHEN** 触发条件满足但目标选取返回空列表
- **THEN** 返回 SkillExecutionResult.NoTargets，不执行效果
- **THEN** 对于 ManaFull 触发器，法力值仍然清零（技能已"释放"但未命中）

### Requirement: 战斗单位运行时数据

系统 SHALL 定义 CombatUnitState 数据结构，作为技能系统与战斗系统之间的接口。CombatUnitState MUST 包含技能系统所需的所有运行时字段。

#### Scenario: CombatUnitState 字段完整性

- **WHEN** 创建 CombatUnitState
- **THEN** MUST 包含以下字段：
  - instId（单位实例 ID）
  - templateId（模板 ID）
  - star（星级）
  - hp / maxHp（当前/最大生命值）
  - atk（攻击力）
  - atkSpeed（攻速）
  - range（射程）
  - mana（当前法力值）
  - col / row（当前位置）
  - isAlive（是否存活）
  - side（阵营: Left/Right）
  - skillDefId（技能定义 ID）
  - modifiers（UnitBattleModifiers 引用）
  - triggerState（触发器运行时状态）
  - buffs（活跃 Buff 列表：眩晕/隐身/反伤等）

### Requirement: SkillExecutionResult 结构

系统 SHALL 定义 SkillExecutionResult，包含技能执行的完整结果信息。

#### Scenario: 结果结构

- **WHEN** 技能成功执行
- **THEN** SkillExecutionResult MUST 包含：
  - status（NotTriggered / NoTargets / Executed）
  - casterInstId（施法者）
  - skillId（技能 ID）
  - effectResults（List<SkillEffectResult>）：每个效果的具体结果

### Requirement: 多效果顺序执行

一个 SkillDefData 可包含多个 EffectData（Effects 数组），系统 MUST 按数组顺序依次执行，前一个效果的状态变更对后续效果可见。

#### Scenario: 顺序依赖

- **WHEN** 技能有 [Damage, Stun] 两个效果
- **THEN** 先执行 Damage（可能导致目标死亡），再执行 Stun
- **WHEN** Damage 导致目标死亡
- **THEN** Stun 不对该目标执行（跳过死亡单位）

### Requirement: 星级缩放

技能参数 MUST 支持星级缩放。SkillDefData 的数值参数（Value1/Value2/Duration 等）为 1 星基础值，高星级按以下规则缩放：

#### Scenario: 伤害/治疗缩放

- **WHEN** 效果为 Damage 或 HealOverTime
- **THEN** 伤害/治疗值通过 caster.atk（已含星级倍率）自然缩放，无需额外处理

#### Scenario: 召唤物星级缩放

- **WHEN** 效果为 Summon，Value2=召唤物星级
- **THEN** 使用 SkillDefData 中定义的固定星级值（不随施法者星级变化）

### Requirement: 程序集放置

技能执行相关类 MUST 按以下规则放置：

#### Scenario: Model/Share 放置

- **WHEN** 类为纯数据结构（CombatUnitState、SkillExecutionResult、SkillEffectResult、TriggerState）
- **THEN** 放置在 `Scripts/Model/Share/AutoChess/` 下

#### Scenario: Hotfix/Server 放置

- **WHEN** 类为执行逻辑（SkillExecutor、TriggerChecker、TargetSelector、EffectApplier）
- **THEN** 放置在 `Scripts/Hotfix/Server/AutoChess/` 下
- **THEN** 使用纯静态类 + `[EnableClass]`（如需要）或无状态静态方法
