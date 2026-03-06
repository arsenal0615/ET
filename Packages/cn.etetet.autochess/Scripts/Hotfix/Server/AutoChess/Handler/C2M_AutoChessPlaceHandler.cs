namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    public class C2M_AutoChessPlaceHandler : MessageLocationHandler<Unit, C2M_AutoChessPlace>
    {
        protected override async ETTask Run(Unit unit, C2M_AutoChessPlace message)
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

            if (!PhaseGateHelper.CanPlace(fsm.CurrentPhase))
            {
                SendError(unit, player, AutoChessOpError.PHASE_NOT_ALLOWED);
                await ETTask.CompletedTask;
                return;
            }

            if (message.Col < 0 || message.Col >= AutoChessDefine.BoardWidth
                || message.Row < 0 || message.Row >= AutoChessDefine.BoardHeight)
            {
                SendError(unit, player, AutoChessOpError.INVALID_POSITION);
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

            bool ok;
            if (targetUnit.Row < 0)
            {
                // 板凳→棋盘
                ok = PlacementService.TryPlaceToBoard(player, message.InstId, message.Col, message.Row, player.PopCap);
                if (!ok)
                {
                    AutoChessOpError errorCode;
                    if (RosterService.GetPopUsed(player) >= player.PopCap)
                    {
                        errorCode = AutoChessOpError.POPCAP_EXCEEDED;
                    }
                    else if (RosterService.FindAt(player, message.Col, message.Row) != null)
                    {
                        errorCode = AutoChessOpError.POS_OCCUPIED;
                    }
                    else
                    {
                        errorCode = AutoChessOpError.INVALID_POSITION;
                    }

                    SendError(unit, player, errorCode);
                    await ETTask.CompletedTask;
                    return;
                }
            }
            else
            {
                // 棋盘内移动
                ok = PlacementService.TryMoveOnBoard(player, message.InstId, message.Col, message.Row);
                if (!ok)
                {
                    AutoChessOpError errorCode;
                    if (RosterService.FindAt(player, message.Col, message.Row) != null)
                    {
                        errorCode = AutoChessOpError.POS_OCCUPIED;
                    }
                    else
                    {
                        errorCode = AutoChessOpError.INVALID_POSITION;
                    }

                    SendError(unit, player, errorCode);
                    await ETTask.CompletedTask;
                    return;
                }
            }

            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Place;
            result.ErrorCode = (int)AutoChessOpError.OK;
            result.MovedInstId = message.InstId;
            result.NewCol = message.Col;
            result.NewRow = message.Row;
            result.PopUsed = RosterService.GetPopUsed(player);
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);

            await ETTask.CompletedTask;
        }

        private static void SendError(Unit unit, MatchPlayer player, AutoChessOpError error)
        {
            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Place;
            result.ErrorCode = (int)error;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);
        }
    }
}
