using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 确定性战斗 Tick 循环。纯函数：输入双方 CombatUnitState，输出 CombatResult。
    /// </summary>
    public static class CombatSimulator
    {
        public static CombatResult RunCombat(
            List<CombatUnitState> leftUnits,
            List<CombatUnitState> rightUnits,
            TraitSnapshot leftSnapshot,
            TraitSnapshot rightSnapshot,
            DeterministicRngComponent rng,
            int round)
        {
            var allUnits = new List<CombatUnitState>(leftUnits.Count + rightUnits.Count);
            allUnits.AddRange(leftUnits);
            allUnits.AddRange(rightUnits);

            var events = new List<CombatEvent>();
            int eventIndex = 0;
            bool isFrenzy = false;

            // Build occupied grid
            bool[,] occupied = new bool[AutoChessDefine.BoardWidth, AutoChessDefine.BoardHeight];
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (u.IsAlive && HexUtil.IsInBounds(u.Col, u.Row))
                    occupied[u.Col, u.Row] = true;
            }

            // ROUND_START event
            events.Add(new CombatEvent { I = eventIndex++, Tick = 0, EventType = CombatEventType.RoundStart });

            // SPAWN events
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                events.Add(new CombatEvent
                {
                    I = eventIndex++, Tick = 0, EventType = CombatEventType.Spawn,
                    SourceInstId = u.InstId, TemplateId = u.TemplateId, Star = u.Star,
                    Side = u.Side, Col = u.Col, Row = u.Row, MaxHp = u.MaxHp, HpAfter = u.Hp,
                });
            }

            // Combat start effects (Assassin jump, Undead curse)
            CombatStartProcessor.Process(allUnits, leftSnapshot, rightSnapshot, events, ref eventIndex, occupied);

            // Apply initial dynamic trait setup
            ApplyAceCaptain(allUnits, leftSnapshot, 0);
            ApplyAceCaptain(allUnits, rightSnapshot, 1);

            // CombatStart skill triggers
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (!u.IsAlive || u.IsStunned) continue;
                TrySkillTrigger(u, SkillTriggerType.CombatStart, allUnits, rng, 0, events, ref eventIndex);
            }

            // Use sub-seed for combat randomness
            uint combatSeed = rng.DeriveSubSeed(PrngPurpose.Combat, round);
            var combatRng = new SimpleRng(combatSeed);

            // Main tick loop
            for (int tick = 0; tick < AutoChessDefine.MaxCombatTicks; tick++)
            {
                // Step 1: Tick buffs (state expiry)
                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnitState u = allUnits[i];
                    if (!u.IsAlive) continue;

                    int buffCountBefore = u.Buffs?.Count ?? 0;
                    SkillExecutor.TickBuffs(u);

                    // Check for removed buffs and emit events
                    if (u.Buffs != null && u.Buffs.Count < buffCountBefore)
                    {
                        events.Add(new CombatEvent
                        {
                            I = eventIndex++, Tick = tick,
                            EventType = CombatEventType.BuffRemove,
                            TargetInstId = u.InstId,
                        });
                    }
                }

                // Step 2: Interval skill triggers
                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnitState u = allUnits[i];
                    if (!u.IsAlive || u.IsStunned) continue;
                    TrySkillTrigger(u, SkillTriggerType.Interval, allUnits, rng, tick, events, ref eventIndex);
                }

                // Step 3: Unit actions (sorted by instId ASC)
                // Sort allUnits by instId for deterministic order
                allUnits.Sort((a, b) => a.InstId.CompareTo(b.InstId));

                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnitState unit = allUnits[i];
                    if (!unit.IsAlive || unit.IsStunned) continue;

                    int effectiveRange = unit.Range + unit.RangeBonus;
                    CombatUnitState target = FindTarget(unit, allUnits, effectiveRange);

                    if (target != null)
                    {
                        // Check attack cooldown
                        float effectiveAtkSpeed = unit.AtkSpeed * unit.AtkSpeedMultiplier;
                        if (isFrenzy) effectiveAtkSpeed *= AutoChessDefine.FrenzyAtkSpeedMultiplier;
                        int atkInterval = Math.Max(1, (int)Math.Ceiling(1f / effectiveAtkSpeed * AutoChessDefine.CombatTickRate));

                        if (tick - unit.LastAttackTick >= atkInterval)
                        {
                            PerformAttack(unit, target, allUnits, combatRng, rng, tick, isFrenzy, events, ref eventIndex, occupied);
                            unit.LastAttackTick = tick;
                        }
                    }
                    else
                    {
                        // No target in range — try to move
                        float effectiveMoveSpeed = unit.MoveSpeed;
                        int moveInterval = Math.Max(1, (int)Math.Ceiling(1f / effectiveMoveSpeed * AutoChessDefine.CombatTickRate));

                        if (tick - unit.LastMoveTick >= moveInterval)
                        {
                            TryMove(unit, allUnits, tick, events, ref eventIndex, occupied);
                            unit.LastMoveTick = tick;
                        }
                    }
                }

                // Step 4: Death cleanup
                var deaths = new List<CombatUnitState>();
                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnitState u = allUnits[i];
                    if (u.IsAlive && u.Hp <= 0)
                    {
                        u.IsAlive = false;
                        deaths.Add(u);

                        // OnDeath triggers
                        TrySkillTrigger(u, SkillTriggerType.OnDeath, allUnits, rng, tick, events, ref eventIndex);

                        // Free grid space
                        if (HexUtil.IsInBounds(u.Col, u.Row))
                            occupied[u.Col, u.Row] = false;
                    }
                }

                deaths.Sort((a, b) => a.InstId.CompareTo(b.InstId));
                for (int d = 0; d < deaths.Count; d++)
                {
                    events.Add(new CombatEvent
                    {
                        I = eventIndex++, Tick = tick,
                        EventType = CombatEventType.Death,
                        SourceInstId = deaths[d].InstId,
                        Side = deaths[d].Side,
                    });
                }

                // Step 5: Check win condition
                var (leftAlive, rightAlive, leftEffective, rightEffective) = CountAlive(allUnits);

                if (leftAlive == 0 && rightAlive == 0)
                {
                    return BuildResult(CombatWinner.Draw, false, events, leftEffective, rightEffective, eventIndex, tick);
                }

                if (leftAlive == 0)
                {
                    return BuildResult(CombatWinner.Right, false, events, leftEffective, rightEffective, eventIndex, tick);
                }

                if (rightAlive == 0)
                {
                    return BuildResult(CombatWinner.Left, false, events, leftEffective, rightEffective, eventIndex, tick);
                }

                // Step 6: Frenzy detection
                if (!isFrenzy && tick >= AutoChessDefine.FrenzyStartTick)
                {
                    isFrenzy = true;
                    events.Add(new CombatEvent
                    {
                        I = eventIndex++, Tick = tick,
                        EventType = CombatEventType.FrenzyStart,
                    });
                }
            }

            // Timeout — draw
            var (_, _, le, re) = CountAlive(allUnits);
            return BuildResult(CombatWinner.Draw, true, events, le, re, eventIndex, AutoChessDefine.MaxCombatTicks - 1);
        }

        private static void PerformAttack(
            CombatUnitState attacker,
            CombatUnitState target,
            List<CombatUnitState> allUnits,
            SimpleRng combatRng,
            DeterministicRngComponent rng,
            int tick,
            bool isFrenzy,
            List<CombatEvent> events,
            ref int eventIndex,
            bool[,] occupied)
        {
            attacker.FacingCol = target.Col;
            attacker.FacingRow = target.Row;
            attacker.AttackTargetInstId = target.InstId;

            // Base damage
            int damage = (int)Math.Floor(attacker.Atk * attacker.DamageMultiplier);

            // Blaster distance bonus
            if (attacker.DistanceDamagePerHex > 0)
            {
                int dist = HexUtil.HexDistance(attacker.Col, attacker.Row, target.Col, target.Row);
                damage = (int)Math.Floor(damage * (1f + dist * attacker.DistanceDamagePerHex));
            }

            // Crit
            bool isCrit = combatRng.Next() % 1000 < (uint)(attacker.CritChance * 1000);
            if (isCrit)
            {
                damage = (int)Math.Floor(damage * AutoChessDefine.DefaultCritMultiplier);
            }

            // Damage reduction
            if (target.DamageReduction > 0)
            {
                damage = (int)Math.Floor(damage * (1f - target.DamageReduction));
            }

            damage = Math.Max(1, damage);

            // ATTACK event
            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.Attack,
                SourceInstId = attacker.InstId,
                TargetInstId = target.InstId,
            });

            // Apply damage
            target.Hp -= damage;

            // DAMAGE event
            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.Damage,
                SourceInstId = attacker.InstId,
                TargetInstId = target.InstId,
                Amount = damage,
                HpAfter = target.Hp,
                IsCrit = isCrit,
                DamageType = CombatDamageType.Normal,
            });

            // Mana gain
            ManaService.GainOnAttack(attacker);
            ManaService.GainOnHit(target);

            // Ace lifesteal
            if (attacker.AceLifestealPct > 0)
            {
                int heal = damage * attacker.AceLifestealPct / 100;
                if (heal > 0)
                {
                    attacker.Hp = Math.Min(attacker.Hp + heal, attacker.MaxHp);
                    events.Add(new CombatEvent
                    {
                        I = eventIndex++, Tick = tick,
                        EventType = CombatEventType.Heal,
                        SourceInstId = attacker.InstId,
                        TargetInstId = attacker.InstId,
                        Amount = heal,
                        HpAfter = attacker.Hp,
                    });
                }
            }

            // Ranger stacking
            if (HasTag(attacker, "Ranger"))
            {
                TraitSnapshot snapshot = attacker.Side == 0 ? null : null; // Ranger uses per-unit tracking
                int maxStacks = GetRangerMaxStacks(attacker);
                if (maxStacks > 0 && attacker.RangerStacks < maxStacks)
                {
                    attacker.RangerStacks++;
                    float bonusPerStack = GetRangerBonusPerStack(attacker);
                    attacker.AtkSpeedMultiplier = 1.0f + attacker.RangerStacks * bonusPerStack;
                }
            }

            // Check Clan trigger
            if (HasTag(attacker, "Clan")) CheckClanTrigger(attacker, tick, events, ref eventIndex);
            if (HasTag(target, "Clan")) CheckClanTrigger(target, tick, events, ref eventIndex);

            // AttackTrait skill trigger
            TrySkillTrigger(attacker, SkillTriggerType.AttackTrait, allUnits, rng, tick, events, ref eventIndex);

            // Update hit count for skill triggers
            if (attacker.Trigger != null) attacker.Trigger.HitCount++;

            // OnHitCount trigger
            TrySkillTrigger(attacker, SkillTriggerType.OnHitCount, allUnits, rng, tick, events, ref eventIndex);

            // ManaFull trigger
            if (ManaService.IsFull(attacker))
            {
                TrySkillTrigger(attacker, SkillTriggerType.ManaFull, allUnits, rng, tick, events, ref eventIndex);
            }

            // OnHpBelow trigger for target
            TrySkillTrigger(target, SkillTriggerType.OnHpBelow, allUnits, rng, tick, events, ref eventIndex);

            // Check kill
            if (target.Hp <= 0)
            {
                // P.E.K.K.A kill reward
                if (HasTag(attacker, "P.E.K.K.A"))
                {
                    int healAmount = (int)(attacker.MaxHp * 0.60f);
                    attacker.Hp = Math.Min(attacker.Hp + healAmount, attacker.MaxHp);
                    attacker.DamageMultiplier += 0.40f;

                    events.Add(new CombatEvent
                    {
                        I = eventIndex++, Tick = tick,
                        EventType = CombatEventType.Heal,
                        SourceInstId = attacker.InstId,
                        TargetInstId = attacker.InstId,
                        Amount = healAmount,
                        HpAfter = attacker.Hp,
                    });
                }

                // Undead kill bonus
                if (target.IsCursedByUndead)
                {
                    for (int i = 0; i < allUnits.Count; i++)
                    {
                        CombatUnitState u = allUnits[i];
                        if (u.IsAlive && u.Side == attacker.Side && HasTag(u, "Undead"))
                        {
                            u.DamageMultiplier += 0.30f;
                        }
                    }
                }

                // OnKill trigger
                if (attacker.Trigger != null) attacker.Trigger.KillTriggerCount++;
                TrySkillTrigger(attacker, SkillTriggerType.OnKill, allUnits, rng, tick, events, ref eventIndex);
            }
        }

        private static void TryMove(
            CombatUnitState unit,
            List<CombatUnitState> allUnits,
            int tick,
            List<CombatEvent> events,
            ref int eventIndex,
            bool[,] occupied)
        {
            // Find nearest enemy
            CombatUnitState nearestEnemy = null;
            int nearestDist = int.MaxValue;

            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState e = allUnits[i];
                if (!e.IsAlive || e.Side == unit.Side || e.IsInvisible) continue;

                int dist = HexUtil.HexDistance(unit.Col, unit.Row, e.Col, e.Row);
                if (dist < nearestDist || (dist == nearestDist && e.InstId < (nearestEnemy?.InstId ?? int.MaxValue)))
                {
                    nearestDist = dist;
                    nearestEnemy = e;
                }
            }

            if (nearestEnemy == null) return;

            // Get neighbors and find best move
            int[] nCols = new int[6];
            int[] nRows = new int[6];
            int count = HexUtil.GetNeighbors(unit.Col, unit.Row, nCols, nRows);

            int bestCol = -1;
            int bestRow = -1;
            int bestReduction = 0;
            int bestDirIdx = -1;

            for (int i = 0; i < count; i++)
            {
                int c = nCols[i];
                int r = nRows[i];
                if (!HexUtil.IsInBounds(c, r)) continue;
                if (occupied[c, r]) continue;

                int newDist = HexUtil.HexDistance(c, r, nearestEnemy.Col, nearestEnemy.Row);
                int reduction = nearestDist - newDist;

                if (reduction <= 0) continue;

                if (reduction > bestReduction || (reduction == bestReduction && i < bestDirIdx))
                {
                    bestReduction = reduction;
                    bestCol = c;
                    bestRow = r;
                    bestDirIdx = i;
                }
            }

            if (bestCol < 0) return; // Can't move

            // Execute move
            occupied[unit.Col, unit.Row] = false;
            unit.Col = bestCol;
            unit.Row = bestRow;
            occupied[bestCol, bestRow] = true;

            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.Move,
                SourceInstId = unit.InstId,
                Col = bestCol,
                Row = bestRow,
            });
        }

        private static CombatUnitState FindTarget(CombatUnitState unit, List<CombatUnitState> allUnits, int range)
        {
            CombatUnitState best = null;
            int bestDist = int.MaxValue;
            bool bestIsAggro = false;

            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState e = allUnits[i];
                if (!e.IsAlive || e.Side == unit.Side || e.IsInvisible) continue;

                int dist = HexUtil.HexDistance(unit.Col, unit.Row, e.Col, e.Row);
                if (dist > range) continue;

                bool isAggro = e.AttackTargetInstId == unit.InstId;

                if (best == null)
                {
                    best = e; bestDist = dist; bestIsAggro = isAggro;
                    continue;
                }

                // Priority: distance < aggro > instId
                if (dist < bestDist)
                {
                    best = e; bestDist = dist; bestIsAggro = isAggro;
                }
                else if (dist == bestDist)
                {
                    if (isAggro && !bestIsAggro)
                    {
                        best = e; bestIsAggro = true;
                    }
                    else if (isAggro == bestIsAggro && e.InstId < best.InstId)
                    {
                        best = e;
                    }
                }
            }

            return best;
        }

        private static void TrySkillTrigger(
            CombatUnitState unit,
            SkillTriggerType triggerType,
            List<CombatUnitState> allUnits,
            DeterministicRngComponent rng,
            int tick,
            List<CombatEvent> events,
            ref int eventIndex)
        {
            if (unit.SkillDefId == 0) return;

            SkillExecutionResult result = SkillExecutor.TryExecute(unit, triggerType, allUnits, rng, tick);
            if (result.Status != SkillExecutionStatus.Executed) return;

            // CAST event
            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.Cast,
                SourceInstId = unit.InstId,
                SkillDefId = unit.SkillDefId,
            });

            // Convert SkillEffectResults to events
            if (result.EffectResults != null)
            {
                for (int i = 0; i < result.EffectResults.Count; i++)
                {
                    SkillEffectResult er = result.EffectResults[i];

                    CombatEventType evtType;
                    if (er.Value < 0) // Healing (negative value convention check)
                        evtType = CombatEventType.Heal;
                    else if (er.EffectType == SkillEffectType.Damage || er.EffectType == SkillEffectType.Projectile)
                        evtType = CombatEventType.Damage;
                    else if (er.EffectType == SkillEffectType.HealOverTime)
                        evtType = CombatEventType.BuffAdd;
                    else if (er.EffectType == SkillEffectType.Stun || er.EffectType == SkillEffectType.Invisibility
                             || er.EffectType == SkillEffectType.SpeedBuff || er.EffectType == SkillEffectType.Reflect)
                        evtType = CombatEventType.BuffAdd;
                    else
                        evtType = CombatEventType.Damage;

                    // Find target HP after effect
                    int hpAfter = 0;
                    for (int j = 0; j < allUnits.Count; j++)
                    {
                        if (allUnits[j].InstId == er.TargetInstId)
                        {
                            hpAfter = allUnits[j].Hp;
                            break;
                        }
                    }

                    events.Add(new CombatEvent
                    {
                        I = eventIndex++, Tick = tick,
                        EventType = evtType,
                        SourceInstId = unit.InstId,
                        TargetInstId = er.TargetInstId,
                        Amount = Math.Abs(er.Value),
                        HpAfter = hpAfter,
                        DamageType = CombatDamageType.Skill,
                        Col = er.Col,
                        Row = er.Row,
                    });
                }
            }
        }

        private static void ApplyAceCaptain(List<CombatUnitState> allUnits, TraitSnapshot snapshot, int side)
        {
            if (snapshot == null) return;

            int aceLevel = 0;
            if (snapshot.ActiveSynergies != null)
            {
                for (int i = 0; i < snapshot.ActiveSynergies.Count; i++)
                {
                    if (snapshot.ActiveSynergies[i].Tag == "Ace")
                    {
                        aceLevel = snapshot.ActiveSynergies[i].Level;
                        break;
                    }
                }
            }

            if (aceLevel <= 0) return;

            // Find captain: star DESC → cost DESC → instId ASC
            CombatUnitState captain = null;
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (!u.IsAlive || u.Side != side || !HasTag(u, "Ace")) continue;

                if (captain == null)
                {
                    captain = u;
                    continue;
                }

                if (u.Star > captain.Star) { captain = u; continue; }
                if (u.Star == captain.Star && u.Cost > captain.Cost) { captain = u; continue; }
                if (u.Star == captain.Star && u.Cost == captain.Cost && u.InstId < captain.InstId) { captain = u; }
            }

            if (captain == null) return;

            SynergyDef aceDef = AutoChessConfigLoader.GetSynergy("Ace");
            int levelIdx = aceLevel - 1;
            if (aceDef?.Effects == null || levelIdx >= aceDef.Effects.Length) return;

            float damageBonus = aceDef.Effects[levelIdx].Param1;
            float lifesteal = aceDef.Effects[levelIdx].Param2;

            captain.DamageMultiplier += damageBonus;
            captain.AceLifestealPct = (int)(lifesteal * 100);
        }

        private static void CheckClanTrigger(CombatUnitState unit, int tick, List<CombatEvent> events, ref int eventIndex)
        {
            if (unit.ClanTriggered || !unit.IsAlive) return;
            if (unit.Hp > unit.MaxHp * 0.5f) return;

            unit.ClanTriggered = true;

            SynergyDef clanDef = AutoChessConfigLoader.GetSynergy("Clan");
            // Determine level from unit's tags — simplified: use level 1 params
            // In practice we'd check the snapshot, but Clan trigger is per-unit
            int clanLevel = 1; // Default to 1, actual level from snapshot would be ideal
            int levelIdx = clanLevel - 1;

            if (clanDef?.Effects == null || levelIdx >= clanDef.Effects.Length) return;

            float healPct = clanDef.Effects[levelIdx].Param1;
            float atkSpeedBoost = clanDef.Effects[levelIdx].Param2;
            float duration = clanDef.Effects[levelIdx].Param3;

            int heal = (int)(unit.MaxHp * healPct);
            unit.Hp = Math.Min(unit.Hp + heal, unit.MaxHp);

            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.Heal,
                SourceInstId = unit.InstId,
                TargetInstId = unit.InstId,
                Amount = heal,
                HpAfter = unit.Hp,
            });

            // Add speed buff
            int durationTicks = (int)(duration * AutoChessDefine.CombatTickRate);
            unit.Buffs.Add(new ActiveBuff
            {
                Type = BuffType.SpeedBuff,
                RemainingTicks = durationTicks,
                Value1 = atkSpeedBoost,
            });

            events.Add(new CombatEvent
            {
                I = eventIndex++, Tick = tick,
                EventType = CombatEventType.BuffAdd,
                TargetInstId = unit.InstId,
                BType = BuffType.SpeedBuff,
                DurationTicks = durationTicks,
            });
        }

        private static int GetRangerMaxStacks(CombatUnitState unit)
        {
            // Use Ranger synergy Param2 as max stacks (default 5)
            SynergyDef rangerDef = AutoChessConfigLoader.GetSynergy("Ranger");
            if (rangerDef?.Effects == null || rangerDef.Effects.Length == 0) return 5;
            return (int)rangerDef.Effects[0].Param2;
        }

        private static float GetRangerBonusPerStack(CombatUnitState unit)
        {
            SynergyDef rangerDef = AutoChessConfigLoader.GetSynergy("Ranger");
            if (rangerDef?.Effects == null || rangerDef.Effects.Length == 0) return 0.10f;
            return rangerDef.Effects[0].Param1;
        }

        private static (int leftAlive, int rightAlive, int leftEffective, int rightEffective) CountAlive(List<CombatUnitState> allUnits)
        {
            int la = 0, ra = 0, le = 0, re = 0;
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (!u.IsAlive) continue;
                if (u.Side == 0) { la++; if (u.IsEffectiveForDamageCount) le++; }
                else { ra++; if (u.IsEffectiveForDamageCount) re++; }
            }
            return (la, ra, le, re);
        }

        private static CombatResult BuildResult(
            CombatWinner winner, bool timeUp,
            List<CombatEvent> events, int leftEffective, int rightEffective,
            int eventIndex, int finalTick)
        {
            events.Add(new CombatEvent
            {
                I = eventIndex, Tick = finalTick,
                EventType = CombatEventType.RoundEnd,
                Winner = winner,
                TimeUp = timeUp,
                LeftAliveEffective = leftEffective,
                RightAliveEffective = rightEffective,
            });

            return new CombatResult
            {
                Winner = winner,
                TimeUp = timeUp,
                Events = events,
                LeftAliveEffective = leftEffective,
                RightAliveEffective = rightEffective,
            };
        }

        private static bool HasTag(CombatUnitState unit, string tag)
        {
            if (unit.Tags == null) return false;
            for (int i = 0; i < unit.Tags.Length; i++)
            {
                if (unit.Tags[i] == tag) return true;
            }
            return false;
        }
    }
}
