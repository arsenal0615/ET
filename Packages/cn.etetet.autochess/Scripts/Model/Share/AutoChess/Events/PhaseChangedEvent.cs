namespace ET
{
    public struct PhaseChangedEvent
    {
        public long MatchRoomId;
        public RoundPhase OldPhase;
        public RoundPhase NewPhase;
        public int Round;
    }
}
