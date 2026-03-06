namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    public class C2M_AutoChessSellHandler : MessageLocationHandler<Unit, C2M_AutoChessSell>
    {
        protected override async ETTask Run(Unit unit, C2M_AutoChessSell message)
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

            if (!PhaseGateHelper.CanSell(fsm.CurrentPhase))
            {
                SendError(unit, player, AutoChessOpError.PHASE_NOT_ALLOWED);
                await ETTask.CompletedTask;
                return;
            }

            UnitInfo targetUnit = RosterService.FindByInstId(player, message.InstId);
            if (targetUnit == null)
            {
                SendError(unit, player, AutoChessOpError.UNIT_NOT_FOUND);
                await ETTask.CompletedTask;
                return;
            }

            if (targetUnit.Row >= 0 && !PhaseGateHelper.CanSellFromBoard(fsm.CurrentPhase))
            {
                SendError(unit, player, AutoChessOpError.CANNOT_SELL_BOARD_IN_BATTLE);
                await ETTask.CompletedTask;
                return;
            }

            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            UnitService.Sell(player, targetUnit, pool, room.CurrentRound);

            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Sell;
            result.ErrorCode = (int)AutoChessOpError.OK;
            result.RemovedInstId = message.InstId;
            result.Elixir = player.Elixir;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);

            await ETTask.CompletedTask;
        }

        private static void SendError(Unit unit, MatchPlayer player, AutoChessOpError error)
        {
            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Sell;
            result.ErrorCode = (int)error;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);
        }
    }
}
