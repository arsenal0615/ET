using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class PreBattleService
    {
        /// <summary>
        /// PreBattle 校验 + 自动修正。
        /// 超人口单位按保留优先级（star DESC, cost DESC, row ASC, col ASC, instId ASC）排序，
        /// 优先级最低的移到板凳；板凳满则强制出售。
        /// </summary>
        public static void ValidateAndFix(MatchPlayer player, SharedPoolComponent pool, int popCap, int round)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();

            // 获取棋盘单位，按保留优先级降序排序
            List<UnitInfo> boardUnits = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) boardUnits.Add(u);
            }

            if (boardUnits.Count <= popCap) return;

            // 按保留优先级降序排序（高优先级在前）
            boardUnits.Sort(CompareRetainPriority);

            // 超出 popCap 的单位需要移走（列表末尾 = 最低优先级）
            for (int i = boardUnits.Count - 1; i >= popCap; i--)
            {
                UnitInfo victim = boardUnits[i];

                // 尝试移到板凳
                int freeCol = FindFreeBenchCol(roster);
                if (freeCol >= 0)
                {
                    victim.Col = freeCol;
                    victim.Row = -1;
                }
                else
                {
                    // 板凳满，强制出售
                    RosterService.Remove(player, victim);
                    ShopService.TrySell(player, victim.TemplateId, victim.Star, victim.IsGift, pool, round);
                }
            }
        }

        /// <summary>
        /// 保留优先级（降序）：star DESC → cost DESC → row ASC → col ASC → instId ASC
        /// </summary>
        private static int CompareRetainPriority(UnitInfo a, UnitInfo b)
        {
            // star DESC
            int cmp = b.Star.CompareTo(a.Star);
            if (cmp != 0) return cmp;

            // cost DESC
            int costA = AutoChessConfigLoader.GetUnit(a.TemplateId).Cost;
            int costB = AutoChessConfigLoader.GetUnit(b.TemplateId).Cost;
            cmp = costB.CompareTo(costA);
            if (cmp != 0) return cmp;

            // row ASC
            cmp = a.Row.CompareTo(b.Row);
            if (cmp != 0) return cmp;

            // col ASC
            cmp = a.Col.CompareTo(b.Col);
            if (cmp != 0) return cmp;

            // instId ASC
            return a.InstId.CompareTo(b.InstId);
        }

        private static int FindFreeBenchCol(RosterComponent roster)
        {
            bool[] occupied = new bool[AutoChessDefine.BenchSize];
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1 && u.Col >= 0 && u.Col < AutoChessDefine.BenchSize)
                    occupied[u.Col] = true;
            }
            for (int i = 0; i < AutoChessDefine.BenchSize; i++)
            {
                if (!occupied[i]) return i;
            }
            return -1;
        }
    }
}
