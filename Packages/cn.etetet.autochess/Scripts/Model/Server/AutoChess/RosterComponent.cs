using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(MatchPlayer))]
    public class RosterComponent : Entity, IAwake, IDestroy
    {
        public List<UnitInfo> Units;
        public int NextInstId;  // 下一个 UnitInfo.InstId，从 1 自增
    }
}
