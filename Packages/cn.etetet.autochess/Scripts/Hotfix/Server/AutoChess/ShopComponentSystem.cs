namespace ET.Server
{
    [EntitySystemOf(typeof(ShopComponent))]
    [FriendOf(typeof(ShopComponent))]
    public static partial class ShopComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ShopComponent self)
        {
            self.Slots = new ShopOffer[AutoChessDefine.ShopSlotCount];
            for (int i = 0; i < AutoChessDefine.ShopSlotCount; i++)
                self.Slots[i] = new ShopOffer { TemplateId = -1 };
            self.ShopRollIndex = 0;
            self.FirstRoundGiftTemplateId = -1;
        }

        [EntitySystem]
        private static void Destroy(this ShopComponent self)
        {
            self.Slots = null;
        }
    }
}
