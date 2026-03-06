namespace ET.Server
{
    /// <summary>
    /// 8 种技能触发条件的确定性判定逻辑。
    /// </summary>
    public static class TriggerChecker
    {
        /// <summary>
        /// 检查 caster 的技能是否在当前触发事件下满足触发条件。
        /// </summary>
        public static bool Check(CombatUnitState caster, SkillDefData skill, SkillTriggerType triggerEvent, int currentTick)
        {
            if (skill.TriggerType != triggerEvent)
            {
                return false;
            }

            switch (skill.TriggerType)
            {
                case SkillTriggerType.CombatStart:
                    return currentTick == 0;

                case SkillTriggerType.Interval:
                {
                    int requiredTicks = (int)(skill.IntervalSeconds * AutoChessDefine.CombatTickRate);
                    if ((currentTick - caster.Trigger.LastIntervalTick) >= requiredTicks)
                    {
                        caster.Trigger.LastIntervalTick = currentTick;
                        return true;
                    }
                    return false;
                }

                case SkillTriggerType.OnHitCount:
                {
                    if (caster.Trigger.HitCount >= skill.HitCountRequired)
                    {
                        caster.Trigger.HitCount = 0;
                        return true;
                    }
                    return false;
                }

                case SkillTriggerType.OnKill:
                {
                    if (skill.ChainLimit <= 0 || caster.Trigger.KillTriggerCount < skill.ChainLimit)
                    {
                        caster.Trigger.KillTriggerCount++;
                        return true;
                    }
                    return false;
                }

                case SkillTriggerType.OnHpBelow:
                {
                    if (!caster.Trigger.HpBelowTriggered && caster.Hp < (int)(caster.MaxHp * skill.HpThresholdPct))
                    {
                        caster.Trigger.HpBelowTriggered = true;
                        return true;
                    }
                    return false;
                }

                case SkillTriggerType.ManaFull:
                    // 法力检查由 ManaService 完成，这里只做事件匹配
                    return true;

                case SkillTriggerType.AttackTrait:
                    // 每次普攻恒 true
                    return true;

                case SkillTriggerType.OnDeath:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 累加命中计数。
        /// </summary>
        public static void IncrementHitCount(CombatUnitState caster)
        {
            caster.Trigger.HitCount++;
        }
    }
}
