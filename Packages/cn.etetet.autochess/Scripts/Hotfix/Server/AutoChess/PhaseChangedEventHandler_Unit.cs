namespace ET.Server
{
    /// <summary>
    /// Deployment: 触发合成链（处理上回合 Battle 阶段买入的重复单位）。
    /// PreBattle: 触发合法性校验与自动修正。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Unit : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            if (args.NewPhase != RoundPhase.Deployment && args.NewPhase != RoundPhase.PreBattle)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchComponent matchComp = scene.GetComponent<MatchComponent>();
            if (matchComp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchRoom room = matchComp.GetRoom(args.MatchRoomId);
            if (room == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            int popCap = room.GetPopCap();

            if (args.NewPhase == RoundPhase.Deployment)
            {
                // Deployment 阶段：触发合成链
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    MergeService.RunMergeChain(player, args.Round);
                }
            }
            else if (args.NewPhase == RoundPhase.PreBattle)
            {
                // PreBattle 阶段：校验 + 自动修正
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    PreBattleService.ValidateAndFix(player, pool, popCap, args.Round);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
