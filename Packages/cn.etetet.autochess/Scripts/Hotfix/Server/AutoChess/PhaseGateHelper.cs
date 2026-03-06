namespace ET.Server
{
    public static class PhaseGateHelper
    {
        public static bool CanBuy(RoundPhase phase)
        {
            return phase == RoundPhase.Deployment || phase == RoundPhase.Battle;
        }

        public static bool CanSell(RoundPhase phase)
        {
            return phase == RoundPhase.Deployment || phase == RoundPhase.Battle;
        }

        public static bool CanSellFromBoard(RoundPhase phase)
        {
            return phase == RoundPhase.Deployment;
        }

        public static bool CanPlace(RoundPhase phase)
        {
            return phase == RoundPhase.Deployment;
        }

        public static bool CanSwap(RoundPhase phase)
        {
            return phase == RoundPhase.Deployment;
        }
    }
}
