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
                case SkillEffectType.Summon:
                    ApplySummon(caster, effect, allUnits, results);
                    break;
                case SkillEffectType.Clone:
                    ApplyClone(caster, effect, allUnits, results);
                    break;
                case SkillEffectType.Reflect:
                    ApplyReflect(targets, effect, results);
                    break;
                case SkillEffectType.HealOverTime:
                    ApplyHealOverTime(targets, effect, results);
                    break;
                case SkillEffectType.Projectile:
                    ApplyProjectile(caster, targets, effect, allUnits, results);
                    break;
                default:
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

        private static void ApplySummon(
            CombatUnitState caster,
            SkillEffectData effect,
            List<CombatUnitState> allUnits,
            List<SkillEffectResult> results)
        {
            int count = (int)effect.Value1;
            var neighborCols = new List<int>(6);
            var neighborRows = new List<int>(6);

            for (int i = 0; i < count; i++)
            {
                // 找 caster 附近的空格
                if (!FindEmptyNeighbor(caster.Col, caster.Row, allUnits, neighborCols, neighborRows,
                        out int col, out int row))
                {
                    break;
                }

                var summoned = new CombatUnitState
                {
                    InstId = allUnits.Count + 100 + i,
                    TemplateId = caster.TemplateId,
                    Star = 1,
                    Hp = (int)(caster.MaxHp * 0.3f),
                    MaxHp = (int)(caster.MaxHp * 0.3f),
                    Atk = (int)(caster.Atk * 0.3f),
                    AtkSpeed = caster.AtkSpeed,
                    Range = caster.Range,
                    MoveSpeed = caster.MoveSpeed,
                    Col = col,
                    Row = row,
                    Side = caster.Side,
                    IsAlive = true,
                    IsEffectiveForDamageCount = false,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(),
                    Trigger = new TriggerState()
                };

                allUnits.Add(summoned);

                results.Add(new SkillEffectResult
                {
                    TargetInstId = summoned.InstId,
                    EffectType = SkillEffectType.Summon,
                    Col = col,
                    Row = row
                });
            }
        }

        private static void ApplyClone(
            CombatUnitState caster,
            SkillEffectData effect,
            List<CombatUnitState> allUnits,
            List<SkillEffectResult> results)
        {
            int count = (int)effect.Value1;
            float hpRatio = effect.Value2;
            int cloneHp = (int)(caster.Hp * hpRatio);

            var neighborCols = new List<int>(6);
            var neighborRows = new List<int>(6);

            int clonesCreated = 0;
            for (int i = 0; i < count; i++)
            {
                if (!FindEmptyNeighbor(caster.Col, caster.Row, allUnits, neighborCols, neighborRows,
                        out int col, out int row))
                {
                    break;
                }

                var clone = new CombatUnitState
                {
                    InstId = allUnits.Count + 100 + i,
                    TemplateId = caster.TemplateId,
                    Star = caster.Star,
                    Hp = cloneHp,
                    MaxHp = cloneHp,
                    Atk = caster.Atk,
                    AtkSpeed = caster.AtkSpeed,
                    Range = caster.Range,
                    MoveSpeed = caster.MoveSpeed,
                    Col = col,
                    Row = row,
                    Side = caster.Side,
                    IsAlive = true,
                    IsEffectiveForDamageCount = false,
                    DamageMultiplier = caster.DamageMultiplier,
                    Buffs = new List<ActiveBuff>(),
                    Trigger = new TriggerState()
                };

                allUnits.Add(clone);
                clonesCreated++;

                results.Add(new SkillEffectResult
                {
                    TargetInstId = clone.InstId,
                    EffectType = SkillEffectType.Clone,
                    Col = col,
                    Row = row
                });
            }

            // 分摊 caster HP
            caster.Hp = (int)(caster.Hp * (1f - hpRatio * clonesCreated));
        }

        private static void ApplyReflect(
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
                    Type = BuffType.Reflect,
                    RemainingTicks = ticks,
                    Value1 = effect.Value1
                });

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.Reflect,
                    Value = ticks
                });
            }
        }

        private static void ApplyHealOverTime(
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
                    Type = BuffType.HealOverTime,
                    RemainingTicks = ticks,
                    Value1 = effect.Value1
                });

                results.Add(new SkillEffectResult
                {
                    TargetInstId = target.InstId,
                    EffectType = SkillEffectType.HealOverTime,
                    Value = ticks
                });
            }
        }

        private static void ApplyProjectile(
            CombatUnitState caster,
            List<CombatUnitState> targets,
            SkillEffectData effect,
            List<CombatUnitState> allUnits,
            List<SkillEffectResult> results)
        {
            List<CombatUnitState> actualTargets;

            if (effect.Value2 > 0)
            {
                // 选最远的 N 个敌方目标
                int maxTargets = (int)effect.Value2;
                var enemies = new List<CombatUnitState>();
                for (int i = 0; i < allUnits.Count; i++)
                {
                    var u = allUnits[i];
                    if (u.Side != caster.Side && u.IsAlive) enemies.Add(u);
                }

                // 按距离降序排列，选最远的
                enemies.Sort((a, b) =>
                {
                    int da = HexUtil.HexDistance(caster.Col, caster.Row, a.Col, a.Row);
                    int db = HexUtil.HexDistance(caster.Col, caster.Row, b.Col, b.Row);
                    return db.CompareTo(da);
                });

                actualTargets = new List<CombatUnitState>();
                for (int i = 0; i < enemies.Count && i < maxTargets; i++)
                {
                    actualTargets.Add(enemies[i]);
                }
            }
            else
            {
                actualTargets = targets;
            }

            int baseDamage = (int)Math.Floor(caster.Atk * effect.Value1 * caster.DamageMultiplier);

            for (int i = 0; i < actualTargets.Count; i++)
            {
                var target = actualTargets[i];
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
                    EffectType = SkillEffectType.Projectile,
                    Value = actualDamage
                });
            }
        }

        private static bool FindEmptyNeighbor(
            int centerCol, int centerRow,
            List<CombatUnitState> allUnits,
            List<int> neighborCols, List<int> neighborRows,
            out int foundCol, out int foundRow)
        {
            HexUtil.GetNeighbors(centerCol, centerRow, neighborCols, neighborRows);

            for (int n = 0; n < neighborCols.Count; n++)
            {
                int nc = neighborCols[n];
                int nr = neighborRows[n];
                if (!HexUtil.IsInBounds(nc, nr)) continue;
                if (IsOccupied(nc, nr, allUnits, null)) continue;

                foundCol = nc;
                foundRow = nr;
                return true;
            }

            foundCol = -1;
            foundRow = -1;
            return false;
        }

        /// <summary>
        /// 每 tick 递减 Buff 持续时间，处理 HealOverTime 效果，移除到期 Buff。
        /// </summary>
        public static void TickBuff(CombatUnitState unit)
        {
            if (unit.Buffs == null || unit.Buffs.Count == 0) return;

            for (int i = unit.Buffs.Count - 1; i >= 0; i--)
            {
                var buff = unit.Buffs[i];
                buff.RemainingTicks--;

                if (buff.Type == BuffType.HealOverTime)
                {
                    int heal = (int)(unit.MaxHp * buff.Value1 / AutoChessDefine.CombatTickRate);
                    unit.Hp = Math.Min(unit.Hp + heal, unit.MaxHp);
                }

                if (buff.RemainingTicks <= 0)
                {
                    unit.Buffs.RemoveAt(i);
                }
            }
        }
    }
}
