# 技术债务 — a1-skill

> 以下 Important 级别问题在代码审查中被发现但未修复。
> 请在后续迭代中处理，或在 archive 时确认。

## 未修复的 Important 问题

### Task 7.1 — SkillExecutor 门面类
- **[I1]** `AutoChessConfigLoader.GetSkill(id)` 使用 `skills[id]` 直接索引，无效 ID 会抛 `KeyNotFoundException` 而非返回 null。`SkillExecutor` 的 null 检查无法生效。建议改为 `TryGetValue`。

### 跨任务 — 确定性保证
- **[I2]** `EffectApplier.ApplyDamage` 和 `ApplyProjectile` 中使用 float 乘法计算伤害。MVP 阶段可接受，但跨平台确定性（客户端验证）需迁移至定点数或整数百分比方案。
- **[I3]** `TriggerChecker.Check` 中 `OnHpBelow` 的 `HpThresholdPct` 语义为 0.5f=50%（ratio），与计划描述的整数百分比格式不一致。当前实现与配置数据匹配，只需确保后续新增技能保持一致。
