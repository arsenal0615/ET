# a1-skill Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 构建技能执行引擎（触发判定、目标选取、效果应用、法力追踪），为 E7 战斗模拟器提供可调用接口。

**Architecture:** 4 个静态服务类（TriggerChecker/TargetSelector/EffectApplier/SkillExecutor）+ 1 个工具类（ManaService）操作纯 C# 数据类（CombatUnitState），通过 SkillExecutor 门面编排 Trigger→Target→Effect 管线。HexUtil 提供六边形距离计算。所有逻辑确定性，整数运算。

**Tech Stack:** C# / ET9 / cn.etetet.autochess 包

**Impact:**
- Modules/Assemblies: cn.etetet.autochess — Model/Share + Hotfix/Server
- Code generation changes: 否
- New data models: CombatUnitState, ActiveBuff, BuffType, TriggerState, SkillEffectResult, SkillExecutionResult（全部纯 C# 类，非 Entity）
- New messages/protocols: 无

**Rules:** ecs-patterns, code-templates

**Design Ref:** docs/changes/a1-skill/design.md

---

## 1. 数据模型（Model/Share）

- [x] 1.1 BuffType 枚举和 ActiveBuff 数据类

**Context:**
- Why: 定义持续效果类型和运行时数据，供 CombatUnitState 和 EffectApplier 使用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/BuffType.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/ActiveBuff.cs`

**Steps:**
1. 创建 `Combat/` 子目录
2. 创建 BuffType 枚举：`None=0, Stun=1, Invisibility=2, Reflect=3, HealOverTime=4, SpeedBuff=5`
3. 创建 ActiveBuff 类（`[EnableClass]`，命名空间 `ET`）：`BuffType Type; int RemainingTicks; float Value1; float Value2;`
4. 编译检查：`dotnet build ET.sln`
5. 提交

- [x] 1.2 TriggerState 数据类

**Context:**
- Why: 追踪每个战斗单位的触发器运行时状态（命中计数、触发次数、HP阈值标记等）

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/TriggerState.cs`

**Steps:**
1. 创建 TriggerState 类（`[EnableClass]`，命名空间 `ET`）：
   - `int HitCount;` — OnHitCount 累计
   - `int KillTriggerCount;` — OnKill 已触发次数
   - `bool HpBelowTriggered;` — OnHpBelow 是否已触发
   - `int LastIntervalTick;` — Interval 上次触发 tick
2. 添加 `Reset()` 方法清零所有字段
3. 编译检查
4. 提交

- [x] 1.3 SkillEffectResult 和 SkillExecutionResult 数据类

**Context:**
- Why: 技能执行的结构化返回值，供 E7 生成 CombatEvent 事件流

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/SkillEffectResult.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/SkillExecutionResult.cs`

**Steps:**
1. 创建 SkillEffectResult（`[EnableClass]`，命名空间 `ET`）：
   - `int TargetInstId;`
   - `SkillEffectType EffectType;`
   - `int Value;` — 伤害/治疗量等
   - `int Col; int Row;` — 位置变更（击退/召唤时用）
2. 创建 SkillExecutionStatus 枚举：`NotTriggered=0, NoTargets=1, Executed=2`
3. 创建 SkillExecutionResult（`[EnableClass]`，命名空间 `ET`）：
   - `SkillExecutionStatus Status;`
   - `int CasterInstId;`
   - `int SkillId;`
   - `List<SkillEffectResult> EffectResults;`
4. 编译检查
5. 提交

- [x] 1.4 CombatUnitState 数据类

**Context:**
- Depends: 1.1, 1.2
- Why: 技能系统与战斗系统之间的核心接口，持有战斗单位的完整运行时状态

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/CombatUnitState.cs`

**Steps:**
1. 创建 CombatUnitState（`[EnableClass]`，命名空间 `ET`）：
   - 身份：`int InstId; int TemplateId; int Star;`
   - 战斗属性：`int Hp; int MaxHp; int Atk; float AtkSpeed; int Range; float MoveSpeed; float CritChance;`
   - 法力：`int Mana; int ManaGainOnAttack; int ManaGainOnHit;`
   - 位置：`int Col; int Row;`
   - 面向：`int FacingCol; int FacingRow;` — 上一个攻击目标位置
   - 状态：`bool IsAlive; int Side;` — Side: 0=Left, 1=Right
   - 技能：`int SkillDefId;`
   - 修改器：`UnitBattleModifiers Modifiers;` — 从 TraitSnapshot 获取
   - Buff：`List<ActiveBuff> Buffs;`
   - 触发器：`TriggerState Trigger;`
   - 标记：`bool IsEffectiveForDamageCount;` — 召唤物为 false
2. 添加辅助属性：`bool IsStunned` → 检查 Buffs 中是否有 BuffType.Stun；`bool IsInvisible` → 检查 Invisibility
3. 编译检查
4. 提交

## 2. 六边形工具（Model/Share）

- [x] 2.1 HexUtil 静态工具类

**Context:**
- Why: 提供 odd-r offset ↔ axial 坐标转换和六边形距离计算，E6 目标选取和 E7 寻路共用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Combat/HexUtil.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestHexUtil

**Steps:**
1. 创建 HexUtil 静态类（命名空间 `ET`）：
   - `OffsetToAxial(int col, int row, out int q, out int r)` — odd-r offset → axial：`q = col - (row - (row & 1)) / 2; r = row;`
   - `HexDistance(int col1, int row1, int col2, int row2)` — 转 axial 后：`(|q1-q2| + |r1-r2| + |q1+r1-q2-r2|) / 2`
   - `GetNeighbors(int col, int row)` — 返回 6 个邻居坐标（odd-r 偶行/奇行偏移不同）
   - `IsInBounds(int col, int row)` — 检查 `0<=col<BoardWidth && 0<=row<BoardHeight`
2. 在 AutoChessTestHelper 中添加 TestHexUtil：验证相邻格距离=1、对角格距离=2、(0,0)到(7,4) 距离正确
3. 编译检查
4. 提交

## 3. 法力服务（Hotfix/Server）

- [x] 3.1 ManaService 静态工具类

**Context:**
- Depends: 1.4
- Why: 法力值累积/消耗/检查的单一入口，E7 在攻击/受伤时调用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/ManaService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestMana

**Steps:**
1. 创建 `Skill/` 子目录（Hotfix/Server/AutoChess/ 下）
2. 创建 ManaService 静态类（命名空间 `ET.Server`）：
   - `GainOnAttack(CombatUnitState unit)` — `unit.Mana = Math.Min(unit.Mana + unit.ManaGainOnAttack, 100);`
   - `GainOnHit(CombatUnitState unit)` — `unit.Mana = Math.Min(unit.Mana + unit.ManaGainOnHit, 100);`
   - `IsFull(CombatUnitState unit)` — `return unit.Mana >= 100;`
   - `Consume(CombatUnitState unit)` — `unit.Mana = 0;`
3. 在 AutoChessTestHelper 中添加 TestMana：
   - 初始 mana=0，GainOnAttack 10次后 mana=100，IsFull=true
   - Consume 后 mana=0
   - mana=95 时 GainOnHit(+6) → mana=100（不溢出）
4. 编译检查
5. 提交

## 4. 触发判定器（Hotfix/Server）

- [x] 4.1 TriggerChecker 静态服务类

**Context:**
- Depends: 1.2, 1.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Defs/SkillDefData.cs`
- Why: 8 种触发条件的确定性判定逻辑

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/TriggerChecker.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestTriggerChecker

**Steps:**
1. 创建 TriggerChecker 静态类（命名空间 `ET.Server`）：
   - `Check(CombatUnitState caster, SkillDefData skill, SkillTriggerType triggerEvent, int currentTick)` → bool
   - 内部 switch 按 TriggerType 分发：
     - `CombatStart`: triggerEvent==CombatStart 且 currentTick==0
     - `Interval`: triggerEvent==Interval 且 (currentTick - trigger.LastIntervalTick) >= skill.IntervalSeconds * 20（tickRate），触发后更新 LastIntervalTick
     - `OnHitCount`: triggerEvent==OnHitCount 且 trigger.HitCount >= skill.HitCountRequired，触发后 HitCount=0
     - `OnKill`: triggerEvent==OnKill 且 (skill.ChainLimit<=0 || trigger.KillTriggerCount < skill.ChainLimit)，触发后 KillTriggerCount++
     - `OnHpBelow`: triggerEvent==OnHpBelow 且 !trigger.HpBelowTriggered 且 caster.Hp < caster.MaxHp * skill.HpThresholdPct / 100，触发后 HpBelowTriggered=true
     - `ManaFull`: triggerEvent==ManaFull（法力检查由 ManaService 完成）
     - `AttackTrait`: triggerEvent==AttackTrait（每次普攻恒 true）
     - `OnDeath`: triggerEvent==OnDeath
   - `IncrementHitCount(CombatUnitState caster)` — 供 E7 普攻命中时调用
2. 在 AutoChessTestHelper 中添加 TestTriggerChecker：
   - CombatStart: tick=0 触发，tick>0 不触发
   - OnHitCount: HitCountRequired=3，命中 2 次不触发，第 3 次触发，计数器重置
   - OnHpBelow: HP 降至阈值以下触发一次，恢复后再降不触发
   - OnKill: ChainLimit=2，触发 2 次后不再触发
3. 编译检查
4. 提交

## 5. 目标选取器（Hotfix/Server）

- [x] 5.1 TargetSelector 静态服务类 — 基础选取（Self/Nearest/Farthest/Lowest）

**Context:**
- Depends: 1.4, 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Defs/SkillDefData.cs`
- Why: 实现 4 种最常用的单目标选取算法

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/TargetSelector.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestTargetSelector

**Steps:**
1. 创建 TargetSelector 静态类（命名空间 `ET.Server`）：
   - `Select(CombatUnitState caster, SkillDefData skill, List<CombatUnitState> allUnits)` → `List<CombatUnitState>`
   - 内部辅助：`GetEnemies(caster, allUnits)` → 过滤 IsAlive && Side != caster.Side && !IsInvisible
   - `SelectSelf` → 返回 [caster]
   - `SelectNearestEnemy` → 按 HexDistance 升序，tiebreaker instId ASC
   - `SelectFarthestInRadius(enemies, caster, radius)` → 半径内按 HexDistance 降序
   - `SelectFarthestInRange(enemies, caster)` → caster.Range 内按 HexDistance 降序
   - `SelectLowestHp(enemies)` → 按 Hp ASC，tiebreaker instId ASC
2. 在 TestTargetSelector 中验证：
   - NearestEnemy: 3 个敌人不同距离，返回最近的
   - 距离相同时按 instId 排序
   - 隐身单位被排除
3. 编译检查
4. 提交

- [x] 5.2 TargetSelector — 多目标和区域选取（Multi/Area/Cluster/Line/Cone）

**Context:**
- Depends: 5.1
- Why: 实现 5 种多目标/区域选取算法，覆盖所有 16 个技能的目标需求

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/TargetSelector.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 扩展 TestTargetSelector

**Steps:**
1. 在 TargetSelector 中添加：
   - `SelectMultiTargets(enemies, caster, count)` → 按距离最近排前 N 个
   - `SelectAreaRadius(enemies, centerCol, centerRow, radius)` → HexDistance <= radius 的全部
   - `SelectClusterLargest(enemies, radius)` → 遍历每个敌人位置为中心，统计半径内敌人数，取最大密集区域。tiebreaker: 中心 row ASC → col ASC → instId ASC
   - `SelectLinePierce(enemies, caster, distance)` → 沿 caster→facing 方向的直线，distance 格内的所有敌人（六边形直线射线检测）
   - `SelectCone(enemies, caster)` → 从 caster 朝 facing 方向的锥形（3 个前方邻居方向）内的敌人
2. 扩展测试：
   - MultiTargets: N=2 时返回最近的 2 个
   - ClusterLargest: 3 个敌人聚集 vs 1 个孤立，选聚集区域
   - AreaRadius: radius=1 时返回相邻格的敌人
3. 编译检查
4. 提交

## 6. 效果应用器（Hotfix/Server）

- [x] 6.1 EffectApplier — 基础效果（Damage/Stun/Knockback/Invisibility/SpeedBuff）

**Context:**
- Depends: 1.1, 1.3, 1.4, 2.1
- Why: 实现 5 种最常用的基础效果，覆盖大部分技能的核心逻辑

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/EffectApplier.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestEffectApplier

**Steps:**
1. 创建 EffectApplier 静态类（命名空间 `ET.Server`）：
   - `Apply(CombatUnitState caster, List<CombatUnitState> targets, SkillEffectData effect, List<CombatUnitState> allUnits, int currentTick)` → `List<SkillEffectResult>`
   - 内部 switch 按 EffectType 分发：
     - `Damage`: `damage = (int)Math.Floor(caster.Atk * effect.Value1 * caster.Modifiers.DamageMultiplier)`，对每个存活目标：`actualDamage = (int)Math.Floor(damage * (1 - target.Modifiers.DamageReduction))`，`target.Hp = Math.Max(target.Hp - actualDamage, 0)`，Hp<=0 → IsAlive=false
     - `Stun`: 添加 ActiveBuff{Type=Stun, RemainingTicks=(int)(effect.Duration*20)}
     - `Knockback`: 沿 caster→target 方向推 (int)effect.Value1 格，逐格检查占位和边界
     - `Invisibility`: 添加 ActiveBuff{Type=Invisibility, RemainingTicks}，Frenzy 时 ticks 减半（由调用方传入 isFrenzy 标记或在 Duration 上预处理）
     - `SpeedBuff`: 添加 ActiveBuff{Type=SpeedBuff, RemainingTicks, Value1=effect.Value1}
2. 在 TestEffectApplier 中验证：
   - Damage: atk=100, Value1=1.5 → damage=150，目标 HP 减少
   - Damage: DamageReduction=0.4 → 实际伤害=90
   - Stun: buff 添加成功，IsStunned=true
   - Knockback: 推 2 格，边界阻挡停在边界前
3. 编译检查
4. 提交

- [x] 6.2 EffectApplier — 高级效果（Summon/Clone/Reflect/HealOverTime/Projectile）

**Context:**
- Depends: 6.1
- Why: 实现 5 种高级效果，覆盖女巫召唤、骷髅龙克隆、和尚反伤等特殊技能

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/EffectApplier.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 扩展 TestEffectApplier

**Steps:**
1. 在 EffectApplier 中添加：
   - `Summon`: 在 caster 附近找 (int)effect.Value1 个空格（HexUtil.GetNeighbors），创建新 CombatUnitState 加入 allUnits，IsEffectiveForDamageCount=false，Side=caster.Side
   - `Clone`: 在附近空格创建克隆体，cloneHp=(int)(caster.Hp * effect.Value2)，caster.Hp=(int)(caster.Hp * effect.Value1)，克隆体 IsEffectiveForDamageCount=false
   - `Reflect`: 添加 ActiveBuff{Type=Reflect, RemainingTicks, Value1=effect.Value1(减伤比例), Value2=0}
   - `HealOverTime`: 添加 ActiveBuff{Type=HealOverTime, RemainingTicks, Value1=effect.Value1(每tick治疗比例)}
   - `Projectile`: 选 (int)effect.Value1 个最远目标（类似 FarthestInRange 多目标），对每个造成 Damage
2. 添加 `TickBuff(CombatUnitState unit)` 辅助方法：
   - 遍历 Buffs，RemainingTicks--
   - HealOverTime: 每 20 ticks（1秒）恢复 `(int)(unit.MaxHp * buff.Value1)`，clamp 到 MaxHp
   - Reflect: 被攻击时由外部调用反弹逻辑（E7 集成）
   - 移除 RemainingTicks<=0 的 Buff
3. 扩展测试：
   - Summon: 召唤 2 个单位，allUnits 增加 2，IsEffectiveForDamageCount=false
   - Clone: caster HP 减少，克隆体 HP 正确
   - HealOverTime: 20 ticks 后恢复一次 HP
4. 编译检查
5. 提交

## 7. 技能执行器（Hotfix/Server）

- [x] 7.1 SkillExecutor 门面类

**Context:**
- Depends: 3.1, 4.1, 5.1, 5.2, 6.1, 6.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Share/AutoChess/AutoChessConfigLoader.cs`
- Why: 编排 Trigger→Target→Effect 管线，作为 E7 调用技能系统的唯一入口

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/Skill/SkillExecutor.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs` — 新增 TestSkillExecutor

**Steps:**
1. 创建 SkillExecutor 静态类（命名空间 `ET.Server`）：
   - `TryExecute(CombatUnitState caster, SkillTriggerType triggerEvent, List<CombatUnitState> allUnits, DeterministicRngComponent rng, int currentTick)` → SkillExecutionResult
   - 流程：
     1. `SkillDefData skill = AutoChessConfigLoader.GetSkill(caster.SkillDefId)`，null 检查
     2. `if (!TriggerChecker.Check(caster, skill, triggerEvent, currentTick))` → return NotTriggered
     3. `List<CombatUnitState> targets = TargetSelector.Select(caster, skill, allUnits)`
     4. `if (targets.Count == 0)` → return NoTargets（ManaFull 时仍 Consume）
     5. 遍历 skill.Effects，调用 `EffectApplier.Apply`，聚合结果
     6. ManaFull 触发后：`ManaService.Consume(caster)` + Superstar 连发判定
     7. 返回 SkillExecutionResult{Status=Executed, ...}
   - `TickBuffs(CombatUnitState unit)` → 委托给 EffectApplier.TickBuff
2. Superstar 连发逻辑（在 ManaFull 分支中）：
   - 检查 caster.Modifiers 是否有连发标记（约定 `AtkSpeedMultiplier` 之外新增 `SuperstarChainChance` 字段或通过 Modifiers 传递）
   - 简化方案：通过 rng.DeriveSubSeed(PrngPurpose.SkillChain, currentTick) 生成子种子
   - while (chainCount < 4 && subSeed % 100 < 50)：重新执行 Target→Effect，chainCount++
3. 在 TestSkillExecutor 中验证：
   - 完整管线：CombatStart 技能，触发 → 选目标 → 应用伤害 → 返回 Executed
   - 未触发：Interval 技能在 tick=0 不触发 → NotTriggered
   - 无目标：所有敌人死亡 → NoTargets
4. 编译检查
5. 提交

## 8. 集成与收尾

- [x] 8.1 UnitBattleModifiers 扩展 — Superstar 连发字段

**Context:**
- Depends: 7.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/UnitBattleModifiers.cs`
- Why: 支持 Superstar 连发概率和法力预充值传递

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/UnitBattleModifiers.cs`

**Steps:**
1. 在 UnitBattleModifiers 中添加两个字段：
   - `public int ManaPrecharge = 0;` — Superstar 预充法力值（0/33/66）
   - `public int ChainCastChancePct = 0;` — 连发概率百分比（0/50）
2. 编译检查
3. 提交

- [x] 8.2 PrngPurpose 扩展 — SkillChain

**Context:**
- Depends: 7.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: Superstar 连发需要独立子种子隔离

**Files:**
- Modify: 包含 PrngPurpose 枚举的文件（在 AutoChessDefine.cs 或独立枚举文件中）

**Steps:**
1. 在 PrngPurpose 枚举中添加 `SkillChain` 值
2. 编译检查
3. 提交

- [x] 8.3 集成测试 — 完整技能管线验证

**Context:**
- Depends: 7.1, 8.1, 8.2
- Why: 端到端验证技能系统的完整性

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 添加 TestSkillSystem 集成测试，覆盖：
   - 近战爆发（SkillId=1, AttackTrait → NearestEnemy → Damage）
   - 王子冲锋（SkillId=4, CombatStart → NearestEnemy → Damage+Knockback+Stun）
   - 女巫召唤（SkillId=9, Interval → Self → Summon，验证 allUnits 增长）
   - 骷髅龙克隆（SkillId=11, OnDeath → Self → Clone）
   - 法力满触发（SkillId=12, ManaFull → ClusterLargest → Projectile）
   - Buff 生命周期（Stun 持续 N ticks 后消失）
2. 更新 RunAllTests 调用链，包含新测试
3. 编译检查
4. 提交

- [x] 8.4 AutoChessDefine 常量补充

**Context:**
- Why: 集中管理技能系统相关的魔法数字

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`

**Steps:**
1. 在 AutoChessDefine 中添加：
   - `public const int ManaMax = 100;`
   - `public const int DefaultManaGainOnAttack = 10;`
   - `public const int DefaultManaGainOnHit = 6;`
   - `public const int CombatTickRate = 20;` — 如果尚不存在
   - `public const int SuperstarMaxChains = 4;`
   - `public const int SuperstarChainChancePct = 50;`
2. 更新 ManaService 和 SkillExecutor 引用这些常量（替换硬编码数字）
3. 编译检查
4. 提交
