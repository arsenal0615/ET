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

            // 为每个玩家添加经济日志组件
            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer player)
                    player.AddComponent<EconomyLogComponent>();
            }

            // 添加 PRNG 组件
            room.AddComponent<DeterministicRngComponent, uint>(seed);

            // 添加回合状态机组件
            room.AddComponent<RoundFSMComponent>();

            return room;
        }
    }
}
