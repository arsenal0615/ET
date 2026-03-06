namespace ET.Server
{
    [FriendOf(typeof(MatchPlayer))]
    public static class AutoChessBroadcastHelper
    {
        /// <summary>
        /// 向单个玩家推送消息。
        /// </summary>
        public static void SendToPlayer(Scene scene, MatchPlayer player, IMessage message)
        {
            if (player.MapUnitId == 0) return; // 未绑定 Unit（测试环境）

            Unit unit = scene.GetComponent<UnitComponent>()?.Get(player.MapUnitId);
            if (unit == null) return;

            MapMessageHelper.SendToClient(unit, message);
        }

        /// <summary>
        /// 广播给所有存活玩家。
        /// </summary>
        public static void BroadcastToAlive(Scene scene, MatchRoom room, IMessage message)
        {
            (message as MessageObject).IsFromPool = false;
            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                SendToPlayer(scene, player, message);
            }
        }

        /// <summary>
        /// 广播给所有玩家（含已淘汰）。
        /// </summary>
        public static void BroadcastToAll(Scene scene, MatchRoom room, IMessage message)
        {
            (message as MessageObject).IsFromPool = false;
            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer player)
                {
                    SendToPlayer(scene, player, message);
                }
            }
        }
    }
}
