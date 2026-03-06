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

                // 尚未实现的多目标/区域类型，Task 5.2 处理
                case SkillTargetType.MultiTargets:
                case SkillTargetType.ClusterLargest:
                case SkillTargetType.AreaRadius:
                case SkillTargetType.LinePierce:
                case SkillTargetType.Cone:
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
    }
}
