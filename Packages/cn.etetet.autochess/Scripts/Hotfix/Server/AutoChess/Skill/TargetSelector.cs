using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 技能目标选取静态服务类。
    /// 根据 SkillTargetType 分发到具体选取算法。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        /// 根据 skill.TargetType 选取目标。
        /// </summary>
        public static List<CombatUnitState> Select(CombatUnitState caster, SkillDefData skill, List<CombatUnitState> allUnits)
        {
            switch (skill.TargetType)
            {
                case SkillTargetType.Self:
                    return SelectSelf(caster);

                case SkillTargetType.NearestEnemy:
                    return SelectNearestEnemy(GetEnemies(caster, allUnits), caster);

                case SkillTargetType.FarthestInRadius:
                {
                    int radius = skill.Effects != null && skill.Effects.Length > 0 ? (int)skill.Effects[0].Value2 : 0;
                    return SelectFarthestInRadius(GetEnemies(caster, allUnits), caster, radius);
                }

                case SkillTargetType.FarthestInRange:
                    return SelectFarthestInRange(GetEnemies(caster, allUnits), caster);

                case SkillTargetType.LowestHp:
                    return SelectLowestHp(GetEnemies(caster, allUnits));

                case SkillTargetType.MultiTargets:
                {
                    int count = skill.Effects != null && skill.Effects.Length > 0 ? (int)skill.Effects[0].Value2 : 1;
                    return SelectMultiTargets(GetEnemies(caster, allUnits), caster, count);
                }

                case SkillTargetType.AreaRadius:
                {
                    int areaRadius = skill.Effects != null && skill.Effects.Length > 0 ? (int)skill.Effects[0].Value2 : 0;
                    return SelectAreaRadius(GetEnemies(caster, allUnits), caster.Col, caster.Row, areaRadius);
                }

                case SkillTargetType.ClusterLargest:
                {
                    int clusterRadius = skill.Effects != null && skill.Effects.Length > 0 ? (int)skill.Effects[0].Value2 : 1;
                    return SelectClusterLargest(GetEnemies(caster, allUnits), clusterRadius);
                }

                case SkillTargetType.LinePierce:
                {
                    int distance = skill.ChainLimit;
                    return SelectLinePierce(GetEnemies(caster, allUnits), caster, distance);
                }

                case SkillTargetType.Cone:
                    return SelectCone(GetEnemies(caster, allUnits), caster);

                default:
                    return new List<CombatUnitState>();
            }
        }

        /// <summary>
        /// 过滤存活、非隐身的敌方单位。
        /// </summary>
        private static List<CombatUnitState> GetEnemies(CombatUnitState caster, List<CombatUnitState> allUnits)
        {
            var enemies = new List<CombatUnitState>();
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnitState u = allUnits[i];
                if (u.IsAlive && u.Side != caster.Side && !u.IsInvisible)
                {
                    enemies.Add(u);
                }
            }
            return enemies;
        }

        /// <summary>
        /// 选取自身。
        /// </summary>
        private static List<CombatUnitState> SelectSelf(CombatUnitState caster)
        {
            var result = new List<CombatUnitState>(1);
            result.Add(caster);
            return result;
        }

        /// <summary>
        /// 选取最近的敌人。距离相同时按 InstId 升序取第一个。
        /// </summary>
        private static List<CombatUnitState> SelectNearestEnemy(List<CombatUnitState> enemies, CombatUnitState caster)
        {
            var result = new List<CombatUnitState>();
            if (enemies.Count == 0) return result;

            CombatUnitState best = null;
            int bestDist = int.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnitState e = enemies[i];
                int dist = HexUtil.HexDistance(caster.Col, caster.Row, e.Col, e.Row);
                if (dist < bestDist || (dist == bestDist && (best == null || e.InstId < best.InstId)))
                {
                    best = e;
                    bestDist = dist;
                }
            }

            if (best != null) result.Add(best);
            return result;
        }

        /// <summary>
        /// 选取半径内最远的敌人。距离相同时按 InstId 升序取第一个。
        /// </summary>
        private static List<CombatUnitState> SelectFarthestInRadius(List<CombatUnitState> enemies, CombatUnitState caster, int radius)
        {
            var result = new List<CombatUnitState>();
            if (enemies.Count == 0) return result;

            CombatUnitState best = null;
            int bestDist = -1;
            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnitState e = enemies[i];
                int dist = HexUtil.HexDistance(caster.Col, caster.Row, e.Col, e.Row);
                if (dist > radius) continue;
                if (dist > bestDist || (dist == bestDist && (best == null || e.InstId < best.InstId)))
                {
                    best = e;
                    bestDist = dist;
                }
            }

            if (best != null) result.Add(best);
            return result;
        }

        /// <summary>
        /// 选取 caster.Range 范围内最远的敌人。距离相同时按 InstId 升序取第一个。
        /// </summary>
        private static List<CombatUnitState> SelectFarthestInRange(List<CombatUnitState> enemies, CombatUnitState caster)
        {
            return SelectFarthestInRadius(enemies, caster, caster.Range);
        }

        /// <summary>
        /// 选取血量最低的敌人。血量相同时按 InstId 升序取第一个。
        /// </summary>
        private static List<CombatUnitState> SelectLowestHp(List<CombatUnitState> enemies)
        {
            var result = new List<CombatUnitState>();
            if (enemies.Count == 0) return result;

            CombatUnitState best = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnitState e = enemies[i];
                if (best == null || e.Hp < best.Hp || (e.Hp == best.Hp && e.InstId < best.InstId))
                {
                    best = e;
                }
            }

            if (best != null) result.Add(best);
            return result;
        }

        /// <summary>
        /// 按距离最近排前 N 个。距离相同时按 InstId 升序。
        /// </summary>
        private static List<CombatUnitState> SelectMultiTargets(List<CombatUnitState> enemies, CombatUnitState caster, int count)
        {
            if (enemies.Count == 0) return new List<CombatUnitState>();

            // 复制一份进行排序，避免修改原列表
            var sorted = new List<CombatUnitState>(enemies);

            // 插入排序（避免 LINQ），按距离升序，距离相同按 InstId 升序
            for (int i = 1; i < sorted.Count; i++)
            {
                CombatUnitState key = sorted[i];
                int keyDist = HexUtil.HexDistance(caster.Col, caster.Row, key.Col, key.Row);
                int j = i - 1;
                while (j >= 0)
                {
                    CombatUnitState cur = sorted[j];
                    int curDist = HexUtil.HexDistance(caster.Col, caster.Row, cur.Col, cur.Row);
                    if (curDist > keyDist || (curDist == keyDist && cur.InstId > key.InstId))
                    {
                        sorted[j + 1] = sorted[j];
                        j--;
                    }
                    else
                    {
                        break;
                    }
                }
                sorted[j + 1] = key;
            }

            int take = count < sorted.Count ? count : sorted.Count;
            var result = new List<CombatUnitState>(take);
            for (int i = 0; i < take; i++)
            {
                result.Add(sorted[i]);
            }
            return result;
        }

        /// <summary>
        /// 选取中心点 radius 范围内的全部敌人。
        /// </summary>
        private static List<CombatUnitState> SelectAreaRadius(List<CombatUnitState> enemies, int centerCol, int centerRow, int radius)
        {
            var result = new List<CombatUnitState>();
            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnitState e = enemies[i];
                if (HexUtil.HexDistance(centerCol, centerRow, e.Col, e.Row) <= radius)
                {
                    result.Add(e);
                }
            }
            return result;
        }

        /// <summary>
        /// 选取最大密集区域的全部敌人。
        /// 遍历每个敌人位置为中心，统计 radius 内敌人数，取最大的。
        /// Tiebreaker: 中心 row ASC -> col ASC -> instId ASC。
        /// </summary>
        private static List<CombatUnitState> SelectClusterLargest(List<CombatUnitState> enemies, int radius)
        {
            if (enemies.Count == 0) return new List<CombatUnitState>();

            int bestCount = -1;
            int bestCenterIdx = 0;

            for (int c = 0; c < enemies.Count; c++)
            {
                CombatUnitState center = enemies[c];
                int cnt = 0;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (HexUtil.HexDistance(center.Col, center.Row, enemies[i].Col, enemies[i].Row) <= radius)
                    {
                        cnt++;
                    }
                }

                if (cnt > bestCount)
                {
                    bestCount = cnt;
                    bestCenterIdx = c;
                }
                else if (cnt == bestCount)
                {
                    CombatUnitState bestCenter = enemies[bestCenterIdx];
                    if (center.Row < bestCenter.Row
                        || (center.Row == bestCenter.Row && center.Col < bestCenter.Col)
                        || (center.Row == bestCenter.Row && center.Col == bestCenter.Col && center.InstId < bestCenter.InstId))
                    {
                        bestCenterIdx = c;
                    }
                }
            }

            CombatUnitState chosen = enemies[bestCenterIdx];
            return SelectAreaRadius(enemies, chosen.Col, chosen.Row, radius);
        }

        /// <summary>
        /// 获取从 (fromCol,fromRow) 到 (toCol,toRow) 最接近的六方向邻居索引(0-5)。
        /// 如果 from==to，默认返回 0（第一个邻居方向）。
        /// 使用 HexUtil.GetNeighbors 获取邻居，通过 axial 点积选取最佳方向。
        /// </summary>
        private static int GetDirectionIndex(int fromCol, int fromRow, int toCol, int toRow,
            List<int> tmpCols, List<int> tmpRows)
        {
            if (fromCol == toCol && fromRow == toRow) return 0;

            HexUtil.GetNeighbors(fromCol, fromRow, tmpCols, tmpRows);

            // 目标方向的 axial 差值
            HexUtil.OffsetToAxial(fromCol, fromRow, out int fq, out int fr);
            HexUtil.OffsetToAxial(toCol, toRow, out int tq, out int tr);
            int dq = tq - fq;
            int dr = tr - fr;

            // 评估每个方向，选点积最大的
            int bestDir = 0;
            int bestDot = int.MinValue;
            for (int d = 0; d < tmpCols.Count; d++)
            {
                HexUtil.OffsetToAxial(tmpCols[d], tmpRows[d], out int nq, out int nr);
                int ndq = nq - fq;
                int ndr = nr - fr;

                int dot = ndq * dq + ndr * dr;
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDir = d;
                }
            }
            return bestDir;
        }

        /// <summary>
        /// 沿 caster→facing 方向直线，distance 格内的所有敌人。
        /// </summary>
        private static List<CombatUnitState> SelectLinePierce(List<CombatUnitState> enemies, CombatUnitState caster, int distance)
        {
            var result = new List<CombatUnitState>();
            if (distance <= 0 || enemies.Count == 0) return result;

            var tmpCols = new List<int>(6);
            var tmpRows = new List<int>(6);
            int dirIdx = GetDirectionIndex(caster.Col, caster.Row, caster.FacingCol, caster.FacingRow, tmpCols, tmpRows);

            // 沿方向步进 distance 格，收集路径上的格子
            int curCol = caster.Col;
            int curRow = caster.Row;

            for (int step = 0; step < distance; step++)
            {
                // 每步都重新获取邻居（因为奇偶行偏移不同）
                HexUtil.GetNeighbors(curCol, curRow, tmpCols, tmpRows);
                curCol = tmpCols[dirIdx];
                curRow = tmpRows[dirIdx];

                // 检查这个格子上是否有敌人
                for (int i = 0; i < enemies.Count; i++)
                {
                    CombatUnitState e = enemies[i];
                    if (e.Col == curCol && e.Row == curRow)
                    {
                        result.Add(e);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 从 caster 朝 facing 方向的锥形（面向方向 + 左右各一个方向）内的邻居格子上的敌人。
        /// </summary>
        private static List<CombatUnitState> SelectCone(List<CombatUnitState> enemies, CombatUnitState caster)
        {
            var result = new List<CombatUnitState>();
            if (enemies.Count == 0) return result;

            var tmpCols = new List<int>(6);
            var tmpRows = new List<int>(6);
            int dirIdx = GetDirectionIndex(caster.Col, caster.Row, caster.FacingCol, caster.FacingRow, tmpCols, tmpRows);

            // GetDirectionIndex 已经调用了 GetNeighbors，tmpCols/tmpRows 已填充
            // 面向方向 + 左右各偏 1 个方向 = 3 个邻居格子
            int leftIdx = (dirIdx + 5) % 6;
            int rightIdx = (dirIdx + 1) % 6;

            int nc0 = tmpCols[dirIdx];
            int nr0 = tmpRows[dirIdx];
            int nc1 = tmpCols[leftIdx];
            int nr1 = tmpRows[leftIdx];
            int nc2 = tmpCols[rightIdx];
            int nr2 = tmpRows[rightIdx];

            for (int i = 0; i < enemies.Count; i++)
            {
                CombatUnitState e = enemies[i];
                if ((e.Col == nc0 && e.Row == nr0)
                    || (e.Col == nc1 && e.Row == nr1)
                    || (e.Col == nc2 && e.Row == nr2))
                {
                    result.Add(e);
                }
            }

            return result;
        }
    }
}
