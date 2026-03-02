using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(MatchComponent))]
    [FriendOf(typeof(MatchComponent))]
    public static partial class MatchComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MatchComponent self)
        {
        }

        public static MatchRoom CreateRoom(this MatchComponent self)
        {
            MatchRoom room = self.AddChild<MatchRoom>();
            return room;
        }

        public static MatchRoom GetRoom(this MatchComponent self, long roomId)
        {
            Entity child = self.GetChild(roomId);
            return child as MatchRoom;
        }

        public static void RemoveRoom(this MatchComponent self, long roomId)
        {
            self.RemoveChild(roomId);
        }
    }
}
