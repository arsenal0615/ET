using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(SynergyComponent))]
    [FriendOf(typeof(RosterComponent))]
    public static class SynergyService
    {
        /// <summary>
        /// 遍历棋盘单位统计标签 count，按阈值计算 level，更新 LiveSynergies。
        /// 有变化则发布 SynergyChangedEvent。
        /// </summary>
        public static void Recalculate(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            SynergyComponent synergy = player.GetComponent<SynergyComponent>();

            // 1. 统计棋盘上单位的 tag 计数
            Dictionary<string, int> tagCounts = new Dictionary<string, int>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row < 0) continue; // 板凳上的不计

                UnitTemplateDef def = AutoChessConfigLoader.GetUnit(u.TemplateId);
                foreach (string tag in def.Tags)
                {
                    if (tagCounts.ContainsKey(tag))
                    {
                        tagCounts[tag]++;
                    }
                    else
                    {
                        tagCounts[tag] = 1;
                    }
                }
            }

            // 2. 构建新的 LiveSynergies 列表
            List<SynergyEntry> newEntries = new List<SynergyEntry>();
            foreach (KeyValuePair<string, int> kv in tagCounts)
            {
                SynergyDef def = AutoChessConfigLoader.GetSynergy(kv.Key);
                int count = kv.Value;
                int level = CalcLevel(count, def.Thresholds);

                newEntries.Add(new SynergyEntry
                {
                    Tag = kv.Key,
                    Count = count,
                    Level = level,
                    Type = def.Type,
                });
            }

            // 3. 对比新旧是否有变化
            bool changed = HasChanged(synergy.LiveSynergies, newEntries);

            if (changed)
            {
                synergy.LiveSynergies = newEntries;
                EventSystem.Instance.Publish(player.Scene(),
                    new SynergyChangedEvent { MatchPlayerId = player.Id });
            }
        }

        /// <summary>
        /// 根据 count 和阈值数组计算等级。
        /// count &lt; thresholds[0] → 0, &lt; thresholds[1] → 1, else → 2
        /// </summary>
        private static int CalcLevel(int count, int[] thresholds)
        {
            if (count < thresholds[0]) return 0;
            if (thresholds.Length < 2 || count < thresholds[1]) return 1;
            return 2;
        }

        /// <summary>
        /// 对比两个 SynergyEntry 列表是否有变化（tag/count/level）。
        /// </summary>
        private static bool HasChanged(List<SynergyEntry> oldList, List<SynergyEntry> newList)
        {
            if (oldList == null || oldList.Count != newList.Count) return true;

            // 将旧列表按 tag 索引
            Dictionary<string, SynergyEntry> oldMap = new Dictionary<string, SynergyEntry>(oldList.Count);
            foreach (SynergyEntry entry in oldList)
            {
                oldMap[entry.Tag] = entry;
            }

            foreach (SynergyEntry newEntry in newList)
            {
                if (!oldMap.TryGetValue(newEntry.Tag, out SynergyEntry oldEntry)) return true;
                if (oldEntry.Count != newEntry.Count || oldEntry.Level != newEntry.Level) return true;
            }

            return false;
        }
    }
}
