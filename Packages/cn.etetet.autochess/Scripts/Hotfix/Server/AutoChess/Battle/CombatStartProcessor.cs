using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 处理 tick=0 的开战特殊效果：刺客跳后排、亡灵诅咒。
    /// </summary>
    public static class CombatStartProcessor
    {
        public static void Process(
            List<CombatUnitState> allUnits,
            TraitSnapshot leftSnapshot,
            TraitSnapshot rightSnapshot,
            List<CombatEvent> events,
            ref int eventIndex,
            bool[,] occupied)
        {
            // 1. Assassin jump (both sides, by instId ASC)
            ProcessAssassinJump(allUnits, leftSnapshot, 0, events, ref eventIndex, occupied);
            ProcessAssassinJump(allUnits, rightSnapshot, 1, events, ref eventIndex, occupied);

            // 2. Undead curse (after assassin jump)
            ProcessUndeadCurse(allUnits, leftSnapshot, 0, events, ref eventIndex);
            ProcessUndeadCurse(allUnits, rightSnapshot, 1, events, ref eventIndex);
        }

        private static void ProcessAssassinJump(
            List<CombatUnitState> allUnits,
            TraitSnapshot snapshot,
            int side,
            List<CombatEvent> events,
            ref int eventIndex,
            bool[,] occupied)
        {
            if (snapshot == null) return;

            int assassinLevel = GetSynergyLevel(snapshot, "Assassin");
            if (assassinLevel <= 0) return;

            // Collect assassins on this side, sorted by instId
            var assassins = new List<CombatUnitState>();
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState unit = allUnits[i];
                if (!unit.IsAlive || unit.Side != side) continue;
                if (!HasTag(unit, "Assassin")) continue;
                assassins.Add(unit);
            }

            assassins.Sort((a, b) => a.InstId.CompareTo(b.InstId));

            int enemySide = 1 - side;
            // Back rows: if assassin is L (side=0), enemy back rows are {0,1}; if R (side=1), enemy back rows are {3,4}
            int backRowMin = side == 0 ? 0 : AutoChessDefine.BoardHeight - 2;
            int backRowMax = side == 0 ? 1 : AutoChessDefine.BoardHeight - 1;

            for (int a = 0; a < assassins.Count; a++)
            {
                CombatUnitState assassin = assassins[a];

                // Find nearest back-row enemy
                CombatUnitState target = null;
                int bestDist = int.MaxValue;

                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnitState enemy = allUnits[i];
                    if (!enemy.IsAlive || enemy.Side != enemySide) continue;
                    if (enemy.Row < backRowMin || enemy.Row > backRowMax) continue;

                    int dist = HexUtil.HexDistance(assassin.Col, assassin.Row, enemy.Col, enemy.Row);
                    if (dist < bestDist || (dist == bestDist && enemy.InstId < (target?.InstId ?? int.MaxValue)))
                    {
                        bestDist = dist;
                        target = enemy;
                    }
                }

                if (target == null) continue;

                // Desired landing: adjacent to target towards assassin's original side
                int landingRow = side == 0 ? target.Row + 1 : target.Row - 1;
                int landingCol = target.Col;

                if (TryLand(landingCol, landingRow, occupied, out int finalCol, out int finalRow))
                {
                    // Success
                }
                else if (TryFindSameRowLanding(landingCol, landingRow, occupied, out finalCol, out finalRow))
                {
                    // Found space in same row
                }
                else if (TryFindNeighborLanding(target, assassin, occupied, out finalCol, out finalRow))
                {
                    // Found space in target's neighbors
                }
                else
                {
                    continue; // Can't jump
                }

                // Move assassin
                occupied[assassin.Col, assassin.Row] = false;
                assassin.Col = finalCol;
                assassin.Row = finalRow;
                occupied[finalCol, finalRow] = true;

                events.Add(new CombatEvent
                {
                    I = eventIndex++,
                    Tick = 0,
                    EventType = CombatEventType.Move,
                    SourceInstId = assassin.InstId,
                    Col = finalCol,
                    Row = finalRow,
                });
            }
        }

        private static bool TryLand(int col, int row, bool[,] occupied, out int outCol, out int outRow)
        {
            outCol = col;
            outRow = row;
            if (!HexUtil.IsInBounds(col, row)) return false;
            if (occupied[col, row]) return false;
            return true;
        }

        private static bool TryFindSameRowLanding(int preferCol, int row, bool[,] occupied, out int outCol, out int outRow)
        {
            outCol = 0;
            outRow = row;
            if (row < 0 || row >= AutoChessDefine.BoardHeight)
                return false;

            int bestCol = -1;
            int bestDist = int.MaxValue;

            for (int c = 0; c < AutoChessDefine.BoardWidth; c++)
            {
                if (occupied[c, row]) continue;
                int dist = Math.Abs(c - preferCol);
                if (dist < bestDist || (dist == bestDist && c < bestCol))
                {
                    bestDist = dist;
                    bestCol = c;
                }
            }

            if (bestCol < 0) return false;
            outCol = bestCol;
            return true;
        }

        private static bool TryFindNeighborLanding(CombatUnitState target, CombatUnitState assassin, bool[,] occupied, out int outCol, out int outRow)
        {
            outCol = 0;
            outRow = 0;

            int[] nCols = new int[6];
            int[] nRows = new int[6];
            int count = HexUtil.GetNeighbors(target.Col, target.Row, nCols, nRows);

            int bestIdx = -1;
            int bestDist = int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                int c = nCols[i];
                int r = nRows[i];
                if (!HexUtil.IsInBounds(c, r)) continue;
                if (occupied[c, r]) continue;

                int dist = HexUtil.HexDistance(assassin.Col, assassin.Row, c, r);
                if (dist < bestDist || (dist == bestDist && (r < nRows[bestIdx] || (r == nRows[bestIdx] && c < nCols[bestIdx]))))
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0) return false;
            outCol = nCols[bestIdx];
            outRow = nRows[bestIdx];
            return true;
        }

        private static void ProcessUndeadCurse(
            List<CombatUnitState> allUnits,
            TraitSnapshot snapshot,
            int side,
            List<CombatEvent> events,
            ref int eventIndex)
        {
            if (snapshot == null) return;

            int undeadLevel = GetSynergyLevel(snapshot, "Undead");
            if (undeadLevel <= 0) return;

            int curseCount = undeadLevel == 1 ? 2 : 3;
            float hpMultiplier = undeadLevel == 1 ? 0.75f : 0.50f;
            int enemySide = 1 - side;

            // Collect enemies sorted by maxHp DESC, instId ASC
            var enemies = new List<CombatUnitState>();
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (u.IsAlive && u.Side == enemySide) enemies.Add(u);
            }

            enemies.Sort((a, b) =>
            {
                int cmp = b.MaxHp.CompareTo(a.MaxHp);
                return cmp != 0 ? cmp : a.InstId.CompareTo(b.InstId);
            });

            int count = Math.Min(curseCount, enemies.Count);
            for (int i = 0; i < count; i++)
            {
                CombatUnitState target = enemies[i];
                target.MaxHp = (int)(target.MaxHp * hpMultiplier);
                if (target.Hp > target.MaxHp) target.Hp = target.MaxHp;
                target.IsCursedByUndead = true;

                events.Add(new CombatEvent
                {
                    I = eventIndex++,
                    Tick = 0,
                    EventType = CombatEventType.BuffAdd,
                    TargetInstId = target.InstId,
                    Amount = target.MaxHp,
                    HpAfter = target.Hp,
                });
            }
        }

        private static int GetSynergyLevel(TraitSnapshot snapshot, string tag)
        {
            if (snapshot?.ActiveSynergies == null) return 0;
            for (int i = 0; i < snapshot.ActiveSynergies.Count; i++)
            {
                if (snapshot.ActiveSynergies[i].Tag == tag)
                    return snapshot.ActiveSynergies[i].Level;
            }
            return 0;
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
