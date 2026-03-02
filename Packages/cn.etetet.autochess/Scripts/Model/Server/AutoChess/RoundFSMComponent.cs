namespace ET.Server
{
    [ComponentOf(typeof(MatchRoom))]
    public class RoundFSMComponent : Entity, IAwake, IDestroy
    {
        public RoundPhase CurrentPhase;
        public RoundPhase PreviousPhase;
        public long PhaseStartTime;  // 阶段开始时间戳(ms)
        public int PhaseDuration;    // 当前阶段时长(ms)
        public int CurrentRound;
        public ETCancellationToken CancellationToken;
    }
}
