using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(SynergyComponent))]
    [FriendOf(typeof(RosterComponent))]
    public static class GoblinGiftService
    {
        /// <summary>
        /// RoundStart 时根据上一回合 Goblin 激活等级赠送哥布林单位到板凳。
        /// 返回实际赠送数量。
        /// </summary>
        public static int TryGiveGifts(MatchPlayer player, DeterministicRngComponent rng, int round)
        {
            SynergyComponent synComp = player.GetComponent<SynergyComponent>();
            int goblinLevel = synComp.LastGoblinLevel;
            if (goblinLevel == 0)
            {
                return 0;
            }

            SynergyDef def = AutoChessConfigLoader.GetSynergy("Goblin");
            int giftCount = (int)def.Effects[goblinLevel - 1].Param1;

            // 收集所有带 Goblin 标签的单位模板 Id
            List<int> goblinTemplates = new List<int>();
            for (int id = 1; id <= AutoChessDefine.TotalUnitTemplates; id++)
            {
                UnitTemplateDef unitDef = AutoChessConfigLoader.GetUnit(id);
                foreach (string tag in unitDef.Tags)
                {
                    if (tag == "Goblin")
                    {
                        goblinTemplates.Add(id);
                        break;
                    }
                }
            }

            if (goblinTemplates.Count == 0)
            {
                return 0;
            }

            int given = 0;
            for (int i = 0; i < giftCount; i++)
            {
                if (RosterService.GetBenchUsed(player) >= AutoChessDefine.BenchSize)
                {
                    break;
                }

                uint subSeed = rng.DeriveSubSeed(PrngPurpose.GoblinGift, round * 10 + i);
                int idx = (int)(subSeed % (uint)goblinTemplates.Count);
                int templateId = goblinTemplates[idx];

                RosterService.AddToBench(player, templateId, 1, true);
                given++;
            }

            return given;
        }
    }
}
