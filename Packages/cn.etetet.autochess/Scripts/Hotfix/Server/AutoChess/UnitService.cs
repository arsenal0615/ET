namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(ShopComponent))]
    public static class UnitService
    {
        /// <summary>
        /// 购买：ShopService.TryBuy + RosterService.AddToBench + 可选合成。
        /// </summary>
        public static bool Buy(MatchPlayer player, int slotIndex,
            SharedPoolComponent pool, DeterministicRngComponent rng,
            RoundPhase currentPhase, int round)
        {
            int templateId = ShopService.TryBuy(player, slotIndex, pool, rng, currentPhase, round);
            if (templateId <= 0) return false;

            UnitInfo unit = RosterService.AddToBench(player, templateId, 1, false);
            if (unit == null)
            {
                // 板凳满，退回购买（回流圣水和卡池）
                ShopService.TrySell(player, templateId, 1, false, pool, round);
                return false;
            }

            // Deployment 阶段触发合成
            if (currentPhase == RoundPhase.Deployment)
            {
                MergeService.RunMergeChain(player, round);
            }

            return true;
        }

        /// <summary>
        /// 出售：RosterService.Remove + ShopService.TrySell。
        /// </summary>
        public static void Sell(MatchPlayer player, UnitInfo unit,
            SharedPoolComponent pool, int round)
        {
            int templateId = unit.TemplateId;
            int star = unit.Star;
            bool isGift = unit.IsGift;

            RosterService.Remove(player, unit);
            ShopService.TrySell(player, templateId, star, isGift, pool, round);
        }

        /// <summary>
        /// 首回合赠送：将已选好的礼包模板创建为 UnitInfo 入板凳。
        /// </summary>
        public static void CreateFirstRoundGift(MatchPlayer player)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();
            int giftTemplateId = shop.FirstRoundGiftTemplateId;
            if (giftTemplateId <= 0) return;

            RosterService.AddToBench(player, giftTemplateId, 1, true);
        }
    }
}
