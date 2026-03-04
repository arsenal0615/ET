using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    public static class RosterService
    {
        /// <summary>
        /// 添加单位到板凳最小空位。板凳满则返回 null。
        /// </summary>
        public static UnitInfo AddToBench(MatchPlayer player, int templateId, int star, bool isGift)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();

            // 找板凳最小空位
            int freeCol = FindFreeBenchCol(roster);
            if (freeCol < 0) return null;

            UnitInfo unit = new UnitInfo
            {
                InstId = roster.NextInstId++,
                TemplateId = templateId,
                Star = star,
                IsGift = isGift,
                Col = freeCol,
                Row = -1,
            };
            roster.Units.Add(unit);
            return unit;
        }

        /// <summary>
        /// 移除单位。
        /// </summary>
        public static void Remove(MatchPlayer player, UnitInfo unit)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            roster.Units.Remove(unit);
        }

        /// <summary>
        /// 按棋盘/板凳坐标查找。
        /// </summary>
        public static UnitInfo FindAt(MatchPlayer player, int col, int row)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Col == col && u.Row == row) return u;
            }
            return null;
        }

        /// <summary>
        /// 按 InstId 查找。
        /// </summary>
        public static UnitInfo FindByInstId(MatchPlayer player, int instId)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.InstId == instId) return u;
            }
            return null;
        }

        /// <summary>
        /// 获取棋盘上的单位列表（Row >= 0）。
        /// </summary>
        public static List<UnitInfo> GetBoardUnits(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            List<UnitInfo> result = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) result.Add(u);
            }
            return result;
        }

        /// <summary>
        /// 获取板凳上的单位列表（Row == -1）。
        /// </summary>
        public static List<UnitInfo> GetBenchUnits(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            List<UnitInfo> result = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1) result.Add(u);
            }
            return result;
        }

        /// <summary>
        /// 棋盘上单位数（= 人口使用量）。
        /// </summary>
        public static int GetPopUsed(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int count = 0;
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) count++;
            }
            return count;
        }

        /// <summary>
        /// 板凳上单位数。
        /// </summary>
        public static int GetBenchUsed(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int count = 0;
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1) count++;
            }
            return count;
        }

        /// <summary>
        /// 淘汰回收：所有单位回流卡池（isGift 不回流），清空列表。
        /// 按星级计算回流份数：1★→1, 2★→2, 3★→4（即 1 &lt;&lt; (star-1)）。
        /// </summary>
        public static void RecoverAllToPool(MatchPlayer player, SharedPoolComponent pool)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (!u.IsGift && u.TemplateId > 0 && u.TemplateId <= AutoChessDefine.TotalUnitTemplates)
                {
                    int copies = 1 << (u.Star - 1);
                    pool.Remaining[u.TemplateId - 1] += copies;
                }
            }
            roster.Units.Clear();
        }

        /// <summary>
        /// 查找板凳最小空列。满则返回 -1。
        /// </summary>
        public static int FindFreeBenchCol(RosterComponent roster)
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
            return -1; // 板凳满
        }
    }
}
