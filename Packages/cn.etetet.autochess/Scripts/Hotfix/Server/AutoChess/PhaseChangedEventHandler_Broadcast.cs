namespace ET.Server
{
    /// <summary>
    /// 监听 PhaseChangedEvent：每次阶段切换时广播 M2C_AutoChessPhaseChange，
    /// Deployment 阶段开始时额外推送 M2C_AutoChessRoundState 个人快照。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Broadcast : AEvent<Scene, PhaseChangedEvent>
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

            RoundFSMComponent fsm = room.GetComponent<RoundFSMComponent>();

            // 1. 所有阶段切换：广播 PhaseChange
            M2C_AutoChessPhaseChange phaseMsg = M2C_AutoChessPhaseChange.Create();
            phaseMsg.Round = args.Round;
            phaseMsg.NewPhase = (int)args.NewPhase;
            phaseMsg.PhaseEndTime = fsm.PhaseStartTime + fsm.PhaseDuration;
            AutoChessBroadcastHelper.BroadcastToAlive(scene, room, phaseMsg);

            // 2. Deployment 阶段开始：给每个存活玩家推送完整 RoundState 快照
            //    此时 Economy/Shop 已在 RoundStart 阶段处理完毕
            if (args.NewPhase == RoundPhase.Deployment)
            {
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    M2C_AutoChessRoundState stateMsg = AutoChessProtoHelper.BuildRoundState(room, player);
                    (stateMsg as MessageObject).IsFromPool = false;
                    AutoChessBroadcastHelper.SendToPlayer(scene, player, stateMsg);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
