using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RosterComponent))]
    [FriendOf(typeof(RosterComponent))]
    public static partial class RosterComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RosterComponent self)
        {
            self.Units = new List<UnitInfo>();
            self.NextInstId = 1;
        }

        [EntitySystem]
        private static void Destroy(this RosterComponent self)
        {
            self.Units.Clear();
            self.Units = null;
            self.NextInstId = 0;
        }
    }
}
