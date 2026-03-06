using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class AutoChessClientComponent : Entity, IAwake, IDestroy
    {
        public int CurrentRound;
        public int CurrentPhase;
        public long PhaseEndTime;

        public int Elixir;
        public int PopCap;
        public int PopUsed;

        public List<AutoChessShopOfferProto> ShopOffers;
        public List<AutoChessUnitInfoProto> BoardUnits;
        public List<AutoChessUnitInfoProto> BenchUnits;
        public List<AutoChessSynergyProto> Synergies;
        public List<AutoChessPlayerPublicInfoProto> AllPlayers;
        public List<AutoChessCombatEventProto> PendingCombatEvents;

        public int LastOpError;
        public long OpponentId;
        public bool MatchEnded;
        public List<AutoChessPlayerPublicInfoProto> FinalRankings;
    }
}
