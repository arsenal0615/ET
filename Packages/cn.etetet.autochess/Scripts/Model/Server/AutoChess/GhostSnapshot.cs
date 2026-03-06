using System.Collections.Generic;

namespace ET.Server
{
    [EnableClass]
    public class GhostSnapshot
    {
        public long PlayerId;
        public List<UnitInfo> Units = new();
        public TraitSnapshot Snapshot;
    }
}
