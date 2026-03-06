namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    public class Match2Map_CreateAutoChessGameHandler : MessageHandler<Scene, Match2Map_CreateAutoChessGame, Map2Match_CreateAutoChessGameResponse>
    {
        protected override async ETTask Run(Scene scene, Match2Map_CreateAutoChessGame request, Map2Match_CreateAutoChessGameResponse response)
        {
            // 1. 获取或创建 MatchComponent
            MatchComponent matchComponent = scene.GetComponent<MatchComponent>();
            if (matchComponent == null)
            {
                matchComponent = scene.AddComponent<MatchComponent>();
            }

            // 2. 通过工厂创建 MatchRoom（含玩家、组件、PRNG、FSM）
            MatchRoom matchRoom = MatchRoomFactory.CreateMatch(matchComponent, request.PlayerIds, request.MatchSeed);

            // 3. 获取或创建 UnitComponent，为每个玩家创建 Map Unit
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                unitComponent = scene.AddComponent<UnitComponent>();
            }

            foreach (Entity child in matchRoom.Children.Values)
            {
                if (child is not MatchPlayer player)
                {
                    continue;
                }

                // 创建 Map Unit（自走棋不需要 Move/AOI，只需要 MailBox 用于 Actor 通信）
                Unit unit = unitComponent.AddChildWithId<Unit, int>(player.PlayerId, 0);
                unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
                unit.AddComponent<AutoChessUnitComponent, long, int, long>(matchRoom.Id, player.PlayerIndex, player.PlayerId);
                unitComponent.Add(unit);

                player.MapUnitId = unit.Id;
            }

            // 4. 启动回合循环
            matchRoom.StartRoundLoop();

            // 5. 填充 response
            response.MatchRoomId = matchRoom.Id;

            await ETTask.CompletedTask;
        }
    }
}
