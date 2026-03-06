## 背景

a1-skill 是 E6（技能系统）的实现。技能配置层（SkillDefData、枚举、16 个配置、UnitTemplateDef.SkillDefId）在 a1-foundation 中已完成。本变更构建**执行层**——触发判定、目标选取、效果应用、法力追踪——为 E7 战斗模拟器提供可调用接口。

现有 autochess 系统已建立的模式（静态服务类、纯 C# 数据类 + `[EnableClass]`、Modifiers 快照）直接适用于本变更。

## 目标 / 非目标

**目标：**
- 构建技能执行引擎，E7 可通过静态方法调用
- 所有逻辑确定性（整数运算、无浮点漂移、确定性 tiebreaker）
- 六边形距离/目标选取算法可独立测试
- 支持 Buff 系统（眩晕/隐身/反伤等持续效果的生命周期管理）

**非目标：**
- 战斗 Tick 循环（E7）
- CombatEvent 事件流生成（E7）
- 单位移动/普攻/寻路（E7）
- 技能在 Tick 中的调度集成（Story 7.4）

## 决策

### TD-1: 战斗单位运行时数据 — 纯 C# 类

**选择：** CombatUnitState 为纯 C# 类（`[EnableClass]`），不是 Entity。

**考虑过的替代方案：**
- A) Entity（ComponentOf: MatchRoom） → 需要 System 注册、增加 Entity 树复杂度、战斗结束需要清理
- B) struct → 值语义导致频繁拷贝，不适合在技能执行中传递引用

**理由：** 与 UnitInfo/ShopOffer/TraitSnapshot 一致（纯 C# 数据类模式）。CombatUnitState 生命周期仅限战斗阶段，由 E7 创建和销毁，不需要 ET Entity 生命周期管理。

### TD-2: 执行逻辑 — 4 个静态服务类

**选择：** 拆为 4 个独立静态服务类：TriggerChecker、TargetSelector、EffectApplier、SkillExecutor。

**考虑过的替代方案：**
- A) 单个 SkillService 大类 → 职责过多，500+ 行
- B) 策略模式（每种 TriggerType/EffectType 一个类） → 过度工程化，ET 框架更偏好函数式静态方法

**理由：** 每个类职责单一（~100-200 行），符合 autochess 已有的静态服务类模式。SkillExecutor 作为门面编排 Trigger→Target→Effect。

### TD-3: 目标选取 — 六边形工具类

**选择：** 新增 HexUtil 静态工具类，提供 odd-r ↔ axial 转换和距离计算。

**考虑过的替代方案：**
- A) 内联在 TargetSelector 中 → E7 的寻路/移动也需要六边形计算，复用性差
- B) 放在 Model/Share 中共享 → 正确选择，六边形数学是纯函数，无副作用

**理由：** HexUtil 放 Model/Share，TargetSelector 放 Hotfix/Server。HexUtil 为 E6 和 E7 共用基础设施。

### TD-4: Buff 系统 — 轻量级列表

**选择：** CombatUnitState 维护 `List<ActiveBuff>` 列表，每个 Buff 有 type、remainingTicks、value。

**考虑过的替代方案：**
- A) 标志位（bool isStunned, bool isInvisible） → 不支持持续时间追踪、不支持多个同类型 Buff
- B) 重量级 Buff 框架（Buff 继承体系 + 优先级 + 叠加规则） → MVP 不需要，13 种羁绊 + 10 种效果的组合足够简单

**理由：** ActiveBuff 是值对象（type + ticks + value），足以覆盖 MVP 的眩晕/隐身/反伤/HoT/SpeedBuff 五种持续效果。E7 每 tick 调用 TickBuffs() 递减 remainingTicks。

### TD-5: 效果结果 — 结构化返回值

**选择：** 每个效果返回 `SkillEffectResult`（targetInstId、effectType、value），SkillExecutor 聚合为 `SkillExecutionResult`。

**考虑过的替代方案：**
- A) 直接在效果内生成 CombatEvent → 职责越界（事件流是 E7 的职责）
- B) void 返回值 + 副作用 → E7 无法知道发生了什么，无法生成事件流

**理由：** 保持 E6/E7 职责分离。E6 返回"发生了什么"，E7 决定"如何记录和呈现"。

### TD-6: 法力 — CombatUnitState 字段

**选择：** 法力值（mana）作为 CombatUnitState 的字段，ManaService 提供 GainMana/CheckAndConsume 静态方法。

**考虑过的替代方案：**
- A) 独立 ManaComponent → 法力与战斗单位 1:1 绑定，拆出去增加间接性无收益
- B) 合并到 SkillExecutor → 法力获取发生在 E7 的攻击/受伤逻辑中，不应由 SkillExecutor 控制

**理由：** ManaService 是极轻量的工具类（< 30 行）。E7 在普攻命中时调用 ManaService.GainOnAttack()，在受伤时调用 ManaService.GainOnHit()。ManaFull 触发检查由 SkillExecutor 在每次法力变化后调用。

### TD-7: 星级缩放 — 利用已有的 atk 星级倍率

**选择：** 伤害/治疗通过 caster.atk（已含星级倍率 1x/2x/4x/8x）自然缩放，不在 SkillDefData 中额外存储星级参数。

**考虑过的替代方案：**
- A) ScalingCell 覆写表 → 配置复杂度高，16 个技能 × 4 星级 = 64 条覆写
- B) 星级倍率乘在 EffectData.Value1 上 → 破坏 Value1 的语义（有时是倍率，有时是绝对值）

**理由：** MVP 中技能伤害 = atk × 倍率，atk 本身已含星级。召唤物星级等特殊参数直接在 SkillDefData 中硬编码（Value2 字段）。Phase 2 如需更灵活的缩放再引入 ScalingCell。

### TD-8: Superstar 连发 — SkillExecutor 内嵌处理

**选择：** Superstar 连发逻辑在 SkillExecutor.Execute 的 ManaFull 分支中处理（循环判定 50% 概率，最多 4 次额外释放）。

**考虑过的替代方案：**
- A) 独立 SuperstarService → 过度封装，连发逻辑仅 ~10 行
- B) 由 E7 Tick 循环处理 → 连发是即时的（同 tick 内），不需要跨 tick

**理由：** 连发使用 combatRng（战斗 PRNG），结果确定性。连发不消耗法力，每次连发调用完整的 Target→Effect 管线。

## 框架特定决策

### 程序集放置

| 类 | 程序集 | 理由 |
|----|--------|------|
| CombatUnitState | Model/Share | 纯数据类，E7 也需要 |
| ActiveBuff | Model/Share | 纯数据类 |
| BuffType (enum) | Model/Share | 枚举 |
| SkillExecutionResult | Model/Share | 纯数据类 |
| SkillEffectResult | Model/Share | 纯数据类 |
| TriggerState | Model/Share | 纯数据类 |
| HexUtil | Model/Share | 纯函数工具类，E7 共用 |
| TriggerChecker | Hotfix/Server | 执行逻辑 |
| TargetSelector | Hotfix/Server | 执行逻辑 |
| EffectApplier | Hotfix/Server | 执行逻辑 |
| SkillExecutor | Hotfix/Server | 门面编排 |
| ManaService | Hotfix/Server | 法力工具方法 |

### 新增类型清单（含 System 类检查）

| 类型 | 需要 EntitySystemOf？ | 说明 |
|------|----------------------|------|
| CombatUnitState | 否 | 纯 C# 类，非 Entity |
| ActiveBuff | 否 | 纯 C# 类 |
| TriggerState | 否 | 纯 C# 类 |
| SkillExecutionResult | 否 | 纯 C# 类 |
| SkillEffectResult | 否 | 纯 C# 类 |
| HexUtil | 否 | 静态工具类 |
| 所有 Service 类 | 否 | 静态服务类 |

**结论：** 本变更无新 Entity 类型，不需要 EntitySystemOf 配对。

### [FriendOf] 需求

| 服务类 | 需要 FriendOf | 说明 |
|--------|-------------|------|
| TriggerChecker | 否 | 操作 CombatUnitState（纯 C# 类，字段 public） |
| TargetSelector | 否 | 同上 |
| EffectApplier | 否 | 同上 |
| SkillExecutor | 否 | 同上 |
| ManaService | 否 | 同上 |

**结论：** CombatUnitState 字段全部 public（非 Entity），无需 FriendOf。

## 关键接口定义

### SkillExecutor（E7 调用入口）

```csharp
public static class SkillExecutor
{
    // E7 在触发事件发生时调用
    public static SkillExecutionResult TryExecute(
        CombatUnitState caster,
        SkillTriggerType triggerEvent,
        List<CombatUnitState> allUnits,
        DeterministicRngComponent rng,  // 仅 Superstar 连发用
        int currentTick)

    // E7 每 tick 调用，递减 buff 持续时间
    public static void TickBuffs(CombatUnitState unit)
}
```

### ManaService（E7 攻击/受伤时调用）

```csharp
public static class ManaService
{
    public static void GainOnAttack(CombatUnitState unit)  // +10, clamp 100
    public static void GainOnHit(CombatUnitState unit)     // +6, clamp 100
    public static bool IsFull(CombatUnitState unit)         // mana >= 100
    public static void Consume(CombatUnitState unit)        // mana = 0
}
```

## 风险 / 权衡

| 风险 | 缓解措施 |
|------|----------|
| CombatUnitState 字段在 E6 定义时可能不完整，E7 需要额外字段 | CombatUnitState 放 Model/Share，E7 可自由扩展字段 |
| 击退效果需要棋盘空格检查，依赖 E7 的战场状态 | EffectApplier.ApplyKnockback 接收 allUnits 列表，自行检查占位冲突 |
| Superstar 连发使用 PRNG，消费序列可能影响后续随机 | 使用 DeriveSubSeed(PrngPurpose.SkillChain, currentTick) 隔离子种子 |
| 目标选取中"面向方向"（Cone/LinePierce）需要单位面向数据 | CombatUnitState 增加 facingCol/facingRow 字段（上一个攻击目标位置），E7 负责更新 |
| EffectType 枚举中没有 Heal（只有 HealOverTime） | 当前 16 个技能不需要即时治疗，HealOverTime 满足 Monk 反伤护盾的需求 |
