namespace ET.Server
{
    /// <summary>
    /// 监听 PhaseChangedEvent：驱动羁绊统计、快照生成和 Goblin 赠送。
    /// - Deployment: 对所有存活玩家 Recalculate
    /// - PreBattle: 对所有存活玩家 GenerateSnapshot
    /// - RoundStart (round >= 2): 对所有存活玩家 TryGiveGifts
    /// </summary>
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Synergy : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
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

            if (args.NewPhase == RoundPhase.Deployment)
            {
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    SynergyService.Recalculate(player);
                }
            }
            else if (args.NewPhase == RoundPhase.PreBattle)
            {
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    SynergyService.GenerateSnapshot(player);
                }
            }
            else if (args.NewPhase == RoundPhase.RoundStart && args.Round >= 2)
            {
                DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    GoblinGiftService.TryGiveGifts(player, rng, args.Round);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
