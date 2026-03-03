using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 玩家经济日志，记录本局所有圣水收支。ComponentOf MatchPlayer。
    /// </summary>
    [ComponentOf(typeof(MatchPlayer))]
    public class EconomyLogComponent : Entity, IAwake, IDestroy
    {
        public List<EconomyDelta> Deltas;
    }
}
