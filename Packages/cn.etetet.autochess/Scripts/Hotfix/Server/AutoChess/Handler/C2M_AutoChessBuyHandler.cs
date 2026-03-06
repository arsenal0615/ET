using System.Collections.Generic;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(MatchRoom))]
    public class C2M_AutoChessBuyHandler : MessageLocationHandler<Unit, C2M_AutoChessBuy>
    {
        protected override async ETTask Run(Unit unit, C2M_AutoChessBuy message)
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

            if (!PhaseGateHelper.CanBuy(fsm.CurrentPhase))
            {
                SendError(unit, player, AutoChessOpError.PHASE_NOT_ALLOWED);
                await ETTask.CompletedTask;
                return;
            }

            if (message.SlotIndex < 0 || message.SlotIndex >= AutoChessDefine.ShopSlotCount)
            {
                SendError(unit, player, AutoChessOpError.INVALID_SLOT);
                await ETTask.CompletedTask;
                return;
            }

            RosterComponent roster = player.GetComponent<RosterComponent>();
            int unitCountBefore = roster.Units.Count;

            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            bool ok = UnitService.Buy(player, message.SlotIndex, pool, rng, fsm.CurrentPhase, room.CurrentRound);

            if (!ok)
            {
                int benchUsed = RosterService.GetBenchUsed(player);
                AutoChessOpError errorCode;
                if (benchUsed >= AutoChessDefine.BenchSize)
                {
                    errorCode = AutoChessOpError.BENCH_FULL;
                }
                else
                {
                    ShopComponent shop = player.GetComponent<ShopComponent>();
                    if (shop.Slots[message.SlotIndex].TemplateId <= 0)
                    {
                        errorCode = AutoChessOpError.INVALID_SLOT;
                    }
                    else
                    {
                        errorCode = AutoChessOpError.INSUFFICIENT_ELIXIR;
                    }
                }

                SendError(unit, player, errorCode);
                await ETTask.CompletedTask;
                return;
            }

            // 购买成功 — 构建 BuyResult
            UnitInfo newUnit = null;
            if (roster.Units.Count > unitCountBefore)
            {
                newUnit = roster.Units[roster.Units.Count - 1];
            }

            AutoChessBuyResultProto buyResult = AutoChessBuyResultProto.Create();
            buyResult.Elixir = player.Elixir;

            if (newUnit != null)
            {
                buyResult.NewUnit = AutoChessProtoHelper.ToUnitInfoProto(newUnit, newUnit.Row >= 0 ? 0 : 1);
            }

            ShopComponent shopAfter = player.GetComponent<ShopComponent>();
            if (shopAfter?.Slots != null)
            {
                for (int i = 0; i < shopAfter.Slots.Length; i++)
                {
                    buyResult.ShopOffers.Add(AutoChessProtoHelper.ToShopOfferProto(i, shopAfter.Slots[i]));
                }
            }

            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Buy;
            result.ErrorCode = (int)AutoChessOpError.OK;
            result.BuyResult = buyResult;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);

            await ETTask.CompletedTask;
        }

        private static void SendError(Unit unit, MatchPlayer player, AutoChessOpError error)
        {
            M2C_AutoChessOpResult result = M2C_AutoChessOpResult.Create();
            result.OpType = (int)OperationType.Buy;
            result.ErrorCode = (int)error;
            AutoChessBroadcastHelper.SendToPlayer(unit.Root(), player, result);
        }
    }
}
