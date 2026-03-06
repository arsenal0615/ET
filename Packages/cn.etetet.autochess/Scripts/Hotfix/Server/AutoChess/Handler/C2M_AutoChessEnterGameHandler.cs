namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(SynergyComponent))]
    public class C2M_AutoChessEnterGameHandler : MessageLocationHandler<Unit, C2M_AutoChessEnterGame, M2C_AutoChessEnterGame>
    {
        protected override async ETTask Run(Unit unit, C2M_AutoChessEnterGame request, M2C_AutoChessEnterGame response)
        {
            // 1. 获取 AutoChessUnitComponent
            AutoChessUnitComponent acUnit = unit.GetComponent<AutoChessUnitComponent>();
            if (acUnit == null)
            {
                response.Error = ErrorCode.ERR_RpcFail;
                response.Message = "AutoChessUnitComponent not found";
                return;
            }

            // 2. 通过它找到 MatchRoom 和 MatchPlayer
            MatchRoom matchRoom = acUnit.GetMatchRoom();
            if (matchRoom == null)
            {
                response.Error = ErrorCode.ERR_RpcFail;
                response.Message = "MatchRoom not found";
                return;
            }

            MatchPlayer matchPlayer = acUnit.GetMatchPlayer();
            if (matchPlayer == null)
            {
                response.Error = ErrorCode.ERR_RpcFail;
                response.Message = "MatchPlayer not found";
                return;
            }

            // 3. 填充基础信息
            RoundFSMComponent fsm = matchRoom.GetComponent<RoundFSMComponent>();
            if (fsm != null)
            {
                response.Round = fsm.CurrentRound;
                response.Phase = (int)fsm.CurrentPhase;
                response.PhaseEndTime = fsm.PhaseStartTime + fsm.PhaseDuration;
            }
            else
            {
                response.Round = matchRoom.CurrentRound;
                response.Phase = (int)RoundPhase.None;
                response.PhaseEndTime = 0;
            }

            response.Elixir = matchPlayer.Elixir;
            response.PopCap = matchPlayer.PopCap;
            response.PopUsed = RosterService.GetPopUsed(matchPlayer);

            // 4. 填充商店信息
            ShopComponent shop = matchPlayer.GetComponent<ShopComponent>();
            if (shop?.Slots != null)
            {
                for (int i = 0; i < shop.Slots.Length; i++)
                {
                    response.ShopOffers.Add(AutoChessProtoHelper.ToShopOfferProto(i, shop.Slots[i]));
                }
            }

            // 5. 填充单位信息（棋盘+板凳）
            RosterComponent roster = matchPlayer.GetComponent<RosterComponent>();
            if (roster?.Units != null)
            {
                foreach (UnitInfo u in roster.Units)
                {
                    if (u.Row >= 0)
                    {
                        response.BoardUnits.Add(AutoChessProtoHelper.ToUnitInfoProto(u, 0));
                    }
                    else
                    {
                        response.BenchUnits.Add(AutoChessProtoHelper.ToUnitInfoProto(u, 1));
                    }
                }
            }

            // 6. 填充羁绊信息
            SynergyComponent synergy = matchPlayer.GetComponent<SynergyComponent>();
            if (synergy?.LiveSynergies != null)
            {
                foreach (SynergyEntry entry in synergy.LiveSynergies)
                {
                    response.Synergies.Add(AutoChessProtoHelper.ToSynergyProto(entry));
                }
            }

            // 7. 填充所有玩家公开信息
            foreach (Entity child in matchRoom.Children.Values)
            {
                if (child is MatchPlayer p)
                {
                    response.AllPlayers.Add(AutoChessProtoHelper.ToPlayerPublicInfoProto(p));
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
