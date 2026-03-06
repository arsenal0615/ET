## 为什么

E6 技能系统是 A1 自走棋关键路径上的下一个 Epic（E5 羁绊已完成）。它是 E7 战斗模拟器的直接前置依赖——没有技能执行框架，战斗模拟器无法调用技能。

技能赋予 24 个单位个性化的战斗表现，是"强复盘可解释"核心柱的关键支撑：玩家需要能看到技能触发、目标选取、效果应用的完整因果链。

**当前状态：** 技能配置层已经存在（a1-foundation 中创建）：
- `SkillDefData` 数据结构 + 所有枚举（8 TriggerType、10 TargetType、10 EffectType）
- `AutoChessConfigLoader.InitSkills()` — 16 个 SkillDef 完整定义
- `UnitTemplateDef.SkillDefId` — 24 个单位的技能关联
- `UnitTemplateDef.ManaGainOnAttack/ManaGainOnHit` — 法力增长参数

**缺失的部分：** 技能执行引擎——触发判定、目标选取算法、效果应用逻辑、法力追踪。这些是纯逻辑层，不依赖战斗 Tick 循环的具体实现，但为 E7 提供可调用的接口。

## 变更内容

- 实现技能触发判定器（8 种 TriggerType 的条件检查逻辑）
- 实现目标选取算法（9 种 TargetType 的六边形棋盘目标筛选）
- 实现效果应用器（10 种 EffectType 的数值/状态变更逻辑）
- 实现法力追踪模型（法力值累积、满值触发、释放后清零）
- 定义技能执行的输入/输出接口，供 E7 战斗模拟器调用
- 确保所有逻辑确定性（无浮点漂移、无随机源泄漏）

**不包含（E7 范围）：**
- 战斗 Tick 循环本身
- 单位移动/普攻/死亡清理
- CombatEvent 事件流生成
- 技能在 Tick 循环中的调度集成（Story 7.4）

## Capabilities

### 新增 Capabilities

- `skill-trigger`: 8 种触发条件的判定逻辑（CombatStart/Interval/OnHitCount/OnKill/OnHpBelow/ManaFull/AttackTrait/OnDeath）
- `skill-target`: 9 种目标选取算法（基于六边形距离、血量、聚合度等）
- `skill-effect`: 10 种效果应用逻辑（伤害/眩晕/击退/隐身/召唤/克隆/反伤/投射物等）
- `mana-system`: 法力追踪模型（累积/触发/清零）
- `skill-execution`: 技能执行管线（Trigger→Target→Effect 的编排接口）

## 影响

### 框架影响检查清单

- [x] **受影响的模块/程序集**: `cn.etetet.autochess` — Model/Share（数据模型）+ Hotfix/Server（执行逻辑）
- [ ] **需要新的协议/消息定义？** 否（E9 网络层在后续 Epic）
- [ ] **需要新的配置/数据文件？** 否（SkillDefData 和 16 个配置已存在）
- [ ] **跨进程/跨线程通信？** 否（纯逻辑层，同 Fiber 内执行）
- [x] **新的数据模型类型？** 技能执行上下文（CombatUnit 或战斗单位快照）、法力追踪状态、效果结果类型
- [x] **影响 system-map.md？** 新增技能执行引擎相关类

### 与现有系统的交互

| 系统 | 交互方式 |
|------|---------|
| SkillDefData（已有） | 读取技能配置，驱动执行逻辑 |
| UnitTemplateDef（已有） | 通过 SkillDefId 查找技能定义 |
| UnitBattleModifiers（a1-synergy） | 效果应用时读取羁绊修改值 |
| TraitSnapshot（a1-synergy） | 技能伤害计算需考虑羁绊加成 |
| E7 战斗模拟器（后续） | 提供 SkillExecutor 接口供 Tick 循环调用 |

### 设计边界说明

技能系统的"边界"在于：它提供**无状态的技能判定和执行方法**，由 E7 战斗模拟器在每个 Tick 中调用。技能系统本身不维护战斗状态——战斗状态（单位血量、位置、法力值等）由 E7 的 CombatState 持有，技能系统接收状态输入、返回效果输出。

这种设计保持了 E6 和 E7 的关注点分离：
- E6 回答"这个技能做什么"
- E7 回答"什么时候调用这个技能"
