using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(SynergyComponent))]
    [FriendOf(typeof(SynergyComponent))]
    public static partial class SynergyComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SynergyComponent self)
        {
            self.LiveSynergies = new List<SynergyEntry>();
            self.LastGoblinLevel = 0;
        }

        [EntitySystem]
        private static void Destroy(this SynergyComponent self)
        {
            self.LiveSynergies = null;
            self.Snapshot = null;
            self.LastGoblinLevel = 0;
        }
    }
}
