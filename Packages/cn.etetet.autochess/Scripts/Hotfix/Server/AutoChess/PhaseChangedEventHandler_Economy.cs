namespace ET.Server
{
    /// <summary>
    /// 监听 PhaseChangedEvent：当阶段切换到 RoundStart 时发放回合收入和统帅被动。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Economy : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            // 只处理 RoundStart 阶段
            if (args.NewPhase != RoundPhase.RoundStart)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 查找对应的 MatchRoom
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

            int round = args.Round;
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();

            // 1. 所有存活玩家 +4 圣水（回合基础收入）
            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                EconomyService.GiveRoundIncome(player, round);
            }

            // 2. 上回合战败者统帅被动（首回合跳过，无战斗结果）
            if (round > 1 && rng != null)
            {
                foreach (long loserId in room.LastRoundLosers)
                {
                    MatchPlayer loser = room.FindPlayerById(loserId);
                    if (loser != null && loser.IsAlive)
                    {
                        EconomyService.GiveCommanderPassive(loser, rng, round);
                    }
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
