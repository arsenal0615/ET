namespace ET.Server
{
    [EntitySystemOf(typeof(AutoChessUnitComponent))]
    [FriendOf(typeof(AutoChessUnitComponent))]
    public static partial class AutoChessUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AutoChessUnitComponent self, long matchRoomId, int playerIndex, long playerId)
        {
            self.MatchRoomId = matchRoomId;
            self.PlayerIndex = playerIndex;
            self.PlayerId = playerId;
        }

        [EntitySystem]
        private static void Destroy(this AutoChessUnitComponent self)
        {
        }

        public static MatchRoom GetMatchRoom(this AutoChessUnitComponent self)
        {
            MatchComponent matchComp = self.Root().GetComponent<MatchComponent>();
            return matchComp?.GetRoom(self.MatchRoomId);
        }

        public static MatchPlayer GetMatchPlayer(this AutoChessUnitComponent self)
        {
            MatchRoom room = self.GetMatchRoom();
            return room?.FindPlayerById(self.PlayerId);
        }
    }
}
