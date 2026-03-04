using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class MergeService
    {
        /// <summary>
        /// 执行完整合成链：从 1★ 开始扫描，直到一轮无合并。返回合成次数。
        /// 确定性排序 key: (star ASC, templateId ASC, locationPriority ASC [board=0,bench=1], row ASC, col ASC, instId ASC)
        /// </summary>
        public static int RunMergeChain(MatchPlayer player, int round)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int totalMerges = 0;

            while (true)
            {
                bool merged = false;

                // 按确定性顺序排序
                roster.Units.Sort(CompareForMerge);

                // 找第一对可合成单位
                for (int i = 0; i < roster.Units.Count - 1; i++)
                {
                    UnitInfo a = roster.Units[i];
                    if (a.Star >= AutoChessDefine.MaxStarLevel) continue;

                    for (int j = i + 1; j < roster.Units.Count; j++)
                    {
                        UnitInfo b = roster.Units[j];
                        if (b.TemplateId == a.TemplateId && b.Star == a.Star)
                        {
                            // 合并: a(primary) 保留, b(secondary) 消耗
                            a.Star++;
                            roster.Units.RemoveAt(j);
                            EconomyService.GiveMerge(player, round);
                            totalMerges++;
                            merged = true;
                            break;
                        }
                    }
                    if (merged) break;
                }

                if (!merged) break;
            }

            return totalMerges;
        }

        private static int CompareForMerge(UnitInfo a, UnitInfo b)
        {
            // star ASC
            int cmp = a.Star.CompareTo(b.Star);
            if (cmp != 0) return cmp;

            // templateId ASC
            cmp = a.TemplateId.CompareTo(b.TemplateId);
            if (cmp != 0) return cmp;

            // locationPriority ASC: board(row>=0)=0, bench(row==-1)=1
            int locA = a.Row >= 0 ? 0 : 1;
            int locB = b.Row >= 0 ? 0 : 1;
            cmp = locA.CompareTo(locB);
            if (cmp != 0) return cmp;

            // row ASC (板凳 row==-1 排前面，但 locationPriority 已区分)
            cmp = a.Row.CompareTo(b.Row);
            if (cmp != 0) return cmp;

            // col ASC
            cmp = a.Col.CompareTo(b.Col);
            if (cmp != 0) return cmp;

            // instId ASC
            return a.InstId.CompareTo(b.InstId);
        }
    }
}
