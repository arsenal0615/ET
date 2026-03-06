using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(AutoChessClientComponent))]
    [FriendOf(typeof(AutoChessClientComponent))]
    public static partial class AutoChessClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AutoChessClientComponent self)
        {
            self.CurrentRound = 0;
            self.CurrentPhase = 0;
            self.PhaseEndTime = 0;
            self.Elixir = 0;
            self.PopCap = 0;
            self.PopUsed = 0;
            self.LastOpError = 0;
            self.OpponentId = 0;
            self.MatchEnded = false;

            self.ShopOffers = new List<AutoChessShopOfferProto>();
            self.BoardUnits = new List<AutoChessUnitInfoProto>();
            self.BenchUnits = new List<AutoChessUnitInfoProto>();
            self.Synergies = new List<AutoChessSynergyProto>();
            self.AllPlayers = new List<AutoChessPlayerPublicInfoProto>();
            self.PendingCombatEvents = new List<AutoChessCombatEventProto>();
            self.FinalRankings = new List<AutoChessPlayerPublicInfoProto>();
        }

        [EntitySystem]
        private static void Destroy(this AutoChessClientComponent self)
        {
            self.ShopOffers = null;
            self.BoardUnits = null;
            self.BenchUnits = null;
            self.Synergies = null;
            self.AllPlayers = null;
            self.PendingCombatEvents = null;
            self.FinalRankings = null;
        }

        public static void ApplyPhaseChange(this AutoChessClientComponent self, int round, int newPhase, long phaseEndTime)
        {
            self.CurrentRound = round;
            self.CurrentPhase = newPhase;
            self.PhaseEndTime = phaseEndTime;
        }

        public static void ApplyRoundState(this AutoChessClientComponent self, M2C_AutoChessRoundState message)
        {
            self.CurrentRound = message.Round;
            self.CurrentPhase = message.Phase;
            self.PhaseEndTime = message.PhaseEndTime;
            self.Elixir = message.Elixir;
            self.PopCap = message.PopCap;
            self.PopUsed = message.PopUsed;

            self.ShopOffers.Clear();
            self.ShopOffers.AddRange(message.ShopOffers);

            self.BoardUnits.Clear();
            self.BoardUnits.AddRange(message.BoardUnits);

            self.BenchUnits.Clear();
            self.BenchUnits.AddRange(message.BenchUnits);

            self.Synergies.Clear();
            self.Synergies.AddRange(message.Synergies);

            self.AllPlayers.Clear();
            self.AllPlayers.AddRange(message.AllPlayers);
        }

        public static void ApplyOpResult(this AutoChessClientComponent self, int errorCode, int elixir, int popUsed)
        {
            self.LastOpError = errorCode;

            if (errorCode == 0)
            {
                self.Elixir = elixir;
                self.PopUsed = popUsed;
            }
        }

        public static void ApplyCombatEvents(this AutoChessClientComponent self, long opponentId, List<AutoChessCombatEventProto> events)
        {
            self.OpponentId = opponentId;
            self.PendingCombatEvents.Clear();
            self.PendingCombatEvents.AddRange(events);
        }

        public static void ApplyRoundResult(this AutoChessClientComponent self, M2C_AutoChessRoundResult message)
        {
            foreach (AutoChessPlayerRoundResultProto result in message.Results)
            {
                for (int i = 0; i < self.AllPlayers.Count; i++)
                {
                    if (self.AllPlayers[i].PlayerId == result.PlayerId)
                    {
                        AutoChessPlayerPublicInfoProto p = self.AllPlayers[i];
                        p.Hp = result.HpAfter;
                        p.IsAlive = result.HpAfter > 0;
                        break;
                    }
                }
            }
        }

        public static void ApplyMatchResult(this AutoChessClientComponent self, List<AutoChessPlayerPublicInfoProto> rankings)
        {
            self.MatchEnded = true;
            self.FinalRankings.Clear();
            self.FinalRankings.AddRange(rankings);
        }
    }
}
