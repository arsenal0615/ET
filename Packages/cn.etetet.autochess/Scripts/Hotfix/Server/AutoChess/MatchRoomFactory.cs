using System.Collections.Generic;

namespace ET.Server
{
    public static class MatchRoomFactory
    {
        public static MatchRoom CreateMatch(MatchComponent matchComponent, List<long> playerIds, uint seed)
        {
            // 创建 MatchRoom
            MatchRoom room = matchComponent.CreateRoom();

            // 初始化（创建玩家等）
            room.Init(seed, playerIds);

            // 添加 PRNG 组件
            room.AddComponent<DeterministicRngComponent, uint>(seed);

            return room;
        }
    }
}
