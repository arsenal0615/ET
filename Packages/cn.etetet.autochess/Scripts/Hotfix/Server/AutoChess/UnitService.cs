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
            // 先检查板凳是否有空位，避免购买后因板凳满导致经济损耗
            if (RosterService.GetBenchUsed(player) >= AutoChessDefine.BenchSize)
                return false;

            int templateId = ShopService.TryBuy(player, slotIndex, pool, rng, currentPhase, round);
            if (templateId <= 0) return false;

            RosterService.AddToBench(player, templateId, 1, false);

            // Deployment 阶段触发合成
            if (currentPhase == RoundPhase.Deployment)
            {
                MergeService.RunMergeChain(player, round);
                SynergyService.Recalculate(player);
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

            bool wasOnBoard = unit.Row >= 0;

            RosterService.Remove(player, unit);
            ShopService.TrySell(player, templateId, star, isGift, pool, round);

            if (wasOnBoard)
            {
                SynergyService.Recalculate(player);
            }
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
