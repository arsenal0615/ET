using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 技能管线门面：Trigger -> Target -> Effect。
    /// 战斗引擎调用技能系统的唯一入口。
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>
        /// 尝试执行 caster 的技能。
        /// rng 可为 null，此时跳过 Superstar 连发判定。
        /// </summary>
        public static SkillExecutionResult TryExecute(
            CombatUnitState caster,
            SkillTriggerType triggerEvent,
            List<CombatUnitState> allUnits,
            DeterministicRngComponent rng,
            int currentTick)
        {
            var result = new SkillExecutionResult
            {
                CasterInstId = caster.InstId,
                SkillId = caster.SkillDefId,
                EffectResults = new List<SkillEffectResult>()
            };

            // 无技能
            if (caster.SkillDefId == 0)
            {
                result.Status = SkillExecutionStatus.NotTriggered;
                return result;
            }

            SkillDefData skill = AutoChessConfigLoader.GetSkill(caster.SkillDefId);
            if (skill == null)
            {
                result.Status = SkillExecutionStatus.NotTriggered;
                return result;
            }

            // 触发检查
            if (!TriggerChecker.Check(caster, skill, triggerEvent, currentTick))
            {
                result.Status = SkillExecutionStatus.NotTriggered;
                return result;
            }

            // 目标选取
            List<CombatUnitState> targets = TargetSelector.Select(caster, skill, allUnits);

            if (targets.Count == 0)
            {
                // ManaFull 时即使无目标也消耗法力
                if (triggerEvent == SkillTriggerType.ManaFull)
                {
                    ManaService.Consume(caster);
                }

                result.Status = SkillExecutionStatus.NoTargets;
                return result;
            }

            // 应用效果
            ApplyAllEffects(caster, targets, skill, allUnits, currentTick, result.EffectResults);

            // ManaFull 后处理：消耗法力 + Superstar 连发
            if (triggerEvent == SkillTriggerType.ManaFull)
            {
                ManaService.Consume(caster);

                // Superstar 连发
                if (rng != null && caster.ChainCastChancePct > 0)
                {
                    int chainCount = 0;
                    while (chainCount < AutoChessDefine.SuperstarMaxChains)
                    {
                        uint subSeed = rng.DeriveSubSeed(PrngPurpose.SkillChain, currentTick * 100 + chainCount);
                        if (subSeed % 100 >= (uint)caster.ChainCastChancePct)
                        {
                            break;
                        }

                        // 重新选目标并应用效果
                        List<CombatUnitState> chainTargets = TargetSelector.Select(caster, skill, allUnits);
                        if (chainTargets.Count == 0) break;

                        ApplyAllEffects(caster, chainTargets, skill, allUnits, currentTick, result.EffectResults);
                        chainCount++;
                    }
                }
            }

            result.Status = SkillExecutionStatus.Executed;
            return result;
        }

        /// <summary>
        /// 每 tick 递减单位 Buff 持续时间，委托给 EffectApplier。
        /// </summary>
        public static void TickBuffs(CombatUnitState unit)
        {
            EffectApplier.TickBuff(unit);
        }

        private static void ApplyAllEffects(
            CombatUnitState caster,
            List<CombatUnitState> targets,
            SkillDefData skill,
            List<CombatUnitState> allUnits,
            int currentTick,
            List<SkillEffectResult> aggregatedResults)
        {
            if (skill.Effects == null) return;

            for (int i = 0; i < skill.Effects.Length; i++)
            {
                List<SkillEffectResult> effectResults = EffectApplier.Apply(caster, targets, skill.Effects[i], allUnits, currentTick);
                for (int j = 0; j < effectResults.Count; j++)
                {
                    aggregatedResults.Add(effectResults[j]);
                }
            }
        }
    }
}
