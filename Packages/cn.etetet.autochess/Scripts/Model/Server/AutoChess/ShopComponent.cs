namespace ET.Server
{
    /// <summary>
    /// 玩家商店状态，挂在 MatchPlayer 上。
    /// Slots[0..2] = 三个商店槽位。
    /// ShopRollIndex 全局递增，确保每次刷新使用唯一子种子。
    /// FirstRoundGiftTemplateId = Round 1 赠送的单位模板ID（-1=未赠送）。
    /// </summary>
    [ComponentOf(typeof(MatchPlayer))]
    public class ShopComponent : Entity, IAwake, IDestroy
    {
        public ShopOffer[] Slots;       // length = AutoChessDefine.ShopSlotCount (3)
        public int ShopRollIndex;       // 全局递增，从不重置
        public int FirstRoundGiftTemplateId; // -1 = 无礼包
    }
}
