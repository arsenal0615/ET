using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 技能效果应用器。根据 SkillEffectData 对目标列表施加效果。
    /// </summary>
    public static class EffectApplier
    {

        /// <summary>
        /// 对目标列表施加技能效果。
        /// </summary>
        public static List<SkillEffectResult> Apply(
            CombatUnitState caster,
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<CombatUnitState> allUnits,
            int currentTick)
        {
            var results = new List<SkillEffectResult>();

            switch (effect.EffectType)
            {
                case SkillEffectType.Damage:
                    ApplyDamage(caster, targets, effect, results);
                    break;
                case SkillEffectType.Stun:
                    ApplyStun(targets, effect, results);
                    break;
                case SkillEffectType.Knockback:
                    ApplyKnockback(caster, targets, effect, allUnits, results);
                    break;
                case SkillEffectType.Invisibility:
                    ApplyInvisibility(targets, effect, results);
                    break;
                case SkillEffectType.SpeedBuff:
                    ApplySpeedBuff(targets, effect, results);
                    break;
                default:
                    // Summon/Clone/Reflect/HealOverTime/Projectile — Task 6.2
                    break;
            }

            return results;
        }

        private static void ApplyDamage(
            CombatUnitState caster,
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<SkillEffectResult> results)
        {
            int baseDamage = (int)Math.Floor(caster.Atk * effect.Value1 * caster.DamageMultiplier);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive) continue;

                int actualDamage = (int)Math.Floor(baseDamage * (1f - target.DamageReduction));
                target.Hp = Math.Max(target.Hp - actualDamage, 0);
                if (target.Hp <= 0)
                {
                    target.IsAlive = false;
                }

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.Damage,
                    Value = actualDamage
                });
            }
        }

        private static void ApplyStun(
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<SkillEffectResult> results)
        {
            int ticks = (int)(effect.Duration * AutoChessDefine.CombatTickRate);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive) continue;

                if (target.Buffs == null)
                {
                    target.Buffs = new List<ActiveBuff>();
                }

                target.Buffs.Add(new ActiveBuff
                {
                    Type = BuffType.Stun,
                    RemainingTicks = ticks
                });

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.Stun,
                    Value = ticks
                });
            }
        }

        private static void ApplyKnockback(
            CombatUnitState caster,
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<CombatUnitState> allUnits,
            List<SkillEffectResult> results)
        {
            int distance = (int)effect.Value1;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive) continue;

                KnockbackUnit(target, caster, distance, allUnits);

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.Knockback,
                    Col = target.Col,
                    Row = target.Row
                });
            }
        }

        private static void KnockbackUnit(
            CombatUnitState target,
            CombatUnitState caster,
            int distance,
            List<CombatUnitState> allUnits)
        {
            var neighborCols = new List<int>(6);
            var neighborRows = new List<int>(6);

            for (int step = 0; step < distance; step++)
            {
                HexUtil.GetNeighbors(target.Col, target.Row, neighborCols, neighborRows);

                int bestCol = -1;
                int bestRow = -1;
                int bestAwayDist = -1;

                for (int n = 0; n < neighborCols.Count; n++)
                {
                    int nc = neighborCols[n];
                    int nr = neighborRows[n];
                    if (!HexUtil.IsInBounds(nc, nr)) continue;
                    if (IsOccupied(nc, nr, allUnits, target)) continue;

                    int awayDist = HexUtil.HexDistance(caster.Col, caster.Row, nc, nr);
                    if (awayDist > bestAwayDist)
                    {
                        bestAwayDist = awayDist;
                        bestCol = nc;
                        bestRow = nr;
                    }
                }

                if (bestCol < 0) break; // 无合法格子，停止
                target.Col = bestCol;
                target.Row = bestRow;
            }
        }

        private static bool IsOccupied(int col, int row, List<CombatUnitState> allUnits, CombatUnitState exclude)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u == exclude) continue;
                if (!u.IsAlive) continue;
                if (u.Col == col && u.Row == row) return true;
            }

            return false;
        }

        private static void ApplyInvisibility(
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<SkillEffectResult> results)
        {
            int ticks = (int)(effect.Duration * AutoChessDefine.CombatTickRate);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive) continue;

                if (target.Buffs == null)
                {
                    target.Buffs = new List<ActiveBuff>();
                }

                target.Buffs.Add(new ActiveBuff
                {
                    Type = BuffType.Invisibility,
                    RemainingTicks = ticks
                });

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.Invisibility,
                    Value = ticks
                });
            }
        }

        private static void ApplySpeedBuff(
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<SkillEffectResult> results)
        {
            int ticks = (int)(effect.Duration * AutoChessDefine.CombatTickRate);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive) continue;

                if (target.Buffs == null)
                {
                    target.Buffs = new List<ActiveBuff>();
                }

                target.Buffs.Add(new ActiveBuff
                {
                    Type = BuffType.SpeedBuff,
                    RemainingTicks = ticks,
                    Value1 = effect.Value1
                });

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.SpeedBuff,
                    Value = ticks
                });
            }
        }
    }
}
