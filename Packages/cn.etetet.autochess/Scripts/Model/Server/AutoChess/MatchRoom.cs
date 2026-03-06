using System.Collections.Generic;

namespace ET.Server
{
    public enum MatchState
    {
        Waiting = 0,
        InGame = 1,
        Settling = 2,
        Finished = 3,
    }

    /// <summary>
    /// 单局对局实体，ChildOf MatchComponent。
    /// </summary>
    [ChildOf(typeof(MatchComponent))]
    public class MatchRoom : Entity, IAwake, IDestroy
    {
        public uint MatchSeed;
        public int CurrentRound;
        public MatchState MatchState;
        public List<(long PlayerId, int Rank)> FinalResults;
        public List<long> LastRoundLosers; // 上回合战败的 PlayerId，E8 结算写入
        public GhostSnapshot LastGhost; // 最近淘汰者阵容快照，用于 Ghost 配对
    }
}
