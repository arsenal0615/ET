using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(EconomyLogComponent))]
    [FriendOf(typeof(EconomyLogComponent))]
    public static partial class EconomyLogComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EconomyLogComponent self)
        {
            self.Deltas = new List<EconomyDelta>();
        }

        [EntitySystem]
        private static void Destroy(this EconomyLogComponent self)
        {
            self.Deltas = null;
        }

        /// <summary>
        /// 追加一条经济记录（由 EconomyService 调用）。
        /// </summary>
        public static void AddDelta(this EconomyLogComponent self, EconomyDelta delta)
        {
            self.Deltas.Add(delta);
        }

        /// <summary>
        /// 按回合查询经济流水。
        /// </summary>
        public static IEnumerable<EconomyDelta> GetDeltasByRound(this EconomyLogComponent self, int round)
        {
            foreach (EconomyDelta delta in self.Deltas)
            {
                if (delta.Round == round)
                    yield return delta;
            }
        }
    }
}
