using System;
using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(SynergyComponent))]
    [FriendOf(typeof(RosterComponent))]
    public static class SynergyService
    {
        /// <summary>
        /// PreBattle 时基于 LiveSynergies 生成冻结快照。
        /// 计算静态效果写入 UnitBattleModifiers，标记动态效果，更新 LastGoblinLevel。
        /// </summary>
        public static void GenerateSnapshot(MatchPlayer player)
        {
            // 1. 确保 LiveSynergies 最新
            Recalculate(player);

            SynergyComponent synergy = player.GetComponent<SynergyComponent>();
            RosterComponent roster = player.GetComponent<RosterComponent>();

            // 2. 创建快照，复制激活的羁绊（level > 0）
            TraitSnapshot snapshot = new TraitSnapshot();
            foreach (SynergyEntry entry in synergy.LiveSynergies)
            {
                if (entry.Level > 0)
                {
                    snapshot.ActiveSynergies.Add(new SynergyEntry
                    {
                        Tag = entry.Tag,
                        Count = entry.Count,
                        Level = entry.Level,
                        Type = entry.Type,
                    });
                }
            }

            // 3. 构建 ActiveSynergies 的 tag 索引，便于查找
            Dictionary<string, SynergyEntry> activeMap = new Dictionary<string, SynergyEntry>();
            foreach (SynergyEntry entry in snapshot.ActiveSynergies)
            {
                activeMap[entry.Tag] = entry;
            }

            // 4. 遍历棋盘单位，为每个 instId 构建 UnitSynergyMap
            foreach (UnitInfo unit in roster.Units)
            {
                if (unit.Row < 0) continue; // 板凳上的不参与

                UnitTemplateDef unitDef = AutoChessConfigLoader.GetUnit(unit.TemplateId);
                List<SynergyEntry> unitSynergies = new List<SynergyEntry>();

                foreach (string tag in unitDef.Tags)
                {
                    if (activeMap.TryGetValue(tag, out SynergyEntry activeEntry))
                    {
                        unitSynergies.Add(activeEntry);
                    }
                }

                snapshot.UnitSynergyMap[unit.InstId] = unitSynergies;

                // 5. 对每个单位调 ApplyStaticEffects 生成 UnitBattleModifiers
                UnitBattleModifiers mods = new UnitBattleModifiers();
                ApplyStaticEffects(unit, unitSynergies, mods);
                snapshot.UnitModifiers[unit.InstId] = mods;
            }

            // 6. 更新 LastGoblinLevel
            synergy.LastGoblinLevel = 0;
            foreach (SynergyEntry entry in snapshot.ActiveSynergies)
            {
                if (entry.Tag == "Goblin")
                {
                    synergy.LastGoblinLevel = entry.Level;
                    break;
                }
            }

            // 7. 赋值快照
            synergy.Snapshot = snapshot;
        }

        /// <summary>
        /// 对单位应用静态羁绊效果，将结果写入 UnitBattleModifiers。
        /// </summary>
        private static void ApplyStaticEffects(UnitInfo unit, List<SynergyEntry> unitSynergies, UnitBattleModifiers mods)
        {
            foreach (SynergyEntry entry in unitSynergies)
            {
                if (entry.Type != SynergyType.Static) continue;
                if (entry.Level <= 0) continue;

                SynergyDef def = AutoChessConfigLoader.GetSynergy(entry.Tag);
                SynergyEffectParam effect = def.Effects[entry.Level - 1];

                switch (entry.Tag)
                {
                    case "Brawler":
                        mods.HpMultiplier *= (1f + effect.Param1);
                        break;
                    case "Giant":
                        mods.DamageReduction = Math.Max(mods.DamageReduction, effect.Param1);
                        break;
                    case "Noble":
                        if (unit.Row <= AutoChessDefine.FrontRowMax)
                        {
                            mods.DamageReduction = Math.Max(mods.DamageReduction, effect.Param1);
                        }
                        else
                        {
                            mods.DamageMultiplier *= (1f + effect.Param1);
                        }
                        break;
                    case "Blaster":
                        mods.RangeBonus += (int)effect.Param2;
                        mods.DistanceDamagePerHex = effect.Param1;
                        break;
                    case "Brutalist":
                        mods.AtkSpeedMultiplier *= (1f - effect.Param1);
                        break;
                }
            }
        }

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
