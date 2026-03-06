namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    public class C2M_AutoChessSwapHandler : MessageLocationHandler<Unit, C2M_AutoChessSwap>
    {
        protected override async ETTask Run(Unit unit, C2M_AutoChessSwap message)
        {
            AutoChessUnitComponent acComp = unit.GetComponent<AutoChessUnitComponent>();
            MatchRoom room = acComp.GetMatchRoom();
            MatchPlayer player = acComp.GetMatchPlayer();

            if (room == null || player == null)
            {
                SendError(unit, player, AutoChessOpError.MATCH_NOT_FOUND);
                await ETTask.CompletedTask;
                return;
            }

            RoundFSMComponent fsm = room.GetComponent<RoundFSMComponent>();

            if (!PhaseGateHelper.CanSwap(fsm.CurrentPhase))
            {
                SendError(unit, player, AutoChessOpError.PHASE_NOT_ALLOWED);
                await ETTask.CompletedTask;
                return;
            }

            UnitInfo u1 = RosterService.FindByInstId(player, message.InstId1);
            UnitInfo u2 = RosterService.FindByInstId(player, message.InstId2);
            if (u1 == null || u2 == null)
            {
                SendError(unit, player, AutoChessOpError.UNIT_NOT_FOUND);
                await ETTask.CompletedTask;
                return;
            }

            bool ok = PlacementService.TrySwap(player, message.InstId1, message.InstId2);
            if (!ok)
            {
                SendError(unit, player, AutoChessOpError.INVALID_POSITION);
                await ETTask.CompletedTask;
                return;
            }

            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Swap;
            result.ErrorCode = (int)AutoChessOpError.OK;
            result.SwapInstId1 = message.InstId1;
            result.SwapInstId2 = message.InstId2;
            result.Swap1Col = u1.Col;
            result.Swap1Row = u1.Row;
            result.Swap2Col = u2.Col;
            result.Swap2Row = u2.Row;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);

            await ETTask.CompletedTask;
        }

        private static void SendError(Unit unit, MatchPlayer player, AutoChessOpError error)
        {
            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Swap;
            result.ErrorCode = (int)error;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);
        }
    }
}
