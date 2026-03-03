namespace ET.Server
{
    [EntitySystemOf(typeof(MatchPlayer))]
    [FriendOf(typeof(MatchPlayer))]
    public static partial class MatchPlayerSystem
    {
        [EntitySystem]
        private static void Awake(this MatchPlayer self, long playerId)
        {
            self.PlayerId = playerId;
            self.Hp = AutoChessDefine.InitialHp;
            self.Elixir = 0;
            self.PopCap = AutoChessDefine.InitialPopCap;
            self.IsAlive = true;
            self.Rank = 0;
            self.PlayerIndex = 0; // 工厂创建时会覆写为 0-3
        }

        [EntitySystem]
        private static void Destroy(this MatchPlayer self)
        {
        }
    }
}
