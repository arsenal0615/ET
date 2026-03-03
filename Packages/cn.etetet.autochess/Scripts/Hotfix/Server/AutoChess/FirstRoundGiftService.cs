using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 首回合礼包服务：为每名玩家随机赠送 1 个 2 费单位。
    /// 不扣卡池（isGift=true）。实际 UnitInstance 创建由 E4 负责。
    /// 结果写入 ShopComponent.FirstRoundGiftTemplateId，供 E4 读取。
    /// </summary>
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(MatchPlayer))]
    public static class FirstRoundGiftService
    {
        /// <summary>
        /// 为玩家选取礼包模板（2 费单位，基于 PlayerIndex 确定性）。
        /// 结果写入 player.GetComponent&lt;ShopComponent&gt;().FirstRoundGiftTemplateId。
        /// </summary>
        public static void SelectGiftTemplate(MatchPlayer player, DeterministicRngComponent rng)
        {
            // 收集所有 2 费模板 ID
            List<int> cost2Templates = new List<int>();
            for (int id = 1; id <= AutoChessDefine.TotalUnitTemplates; id++)
            {
                if (AutoChessConfigLoader.GetUnit(id).Cost == 2)
                    cost2Templates.Add(id);
            }

            if (cost2Templates.Count == 0)
                return;

            uint sub = rng.DeriveSubSeed(PrngPurpose.FirstGift, player.PlayerIndex);
            int idx = (int)(sub % (uint)cost2Templates.Count);
            int templateId = cost2Templates[idx];

            ShopComponent shop = player.GetComponent<ShopComponent>();
            shop.FirstRoundGiftTemplateId = templateId;
            // 注意：不预扣卡池（isGift=true）
        }
    }
}
