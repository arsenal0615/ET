using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    public static partial class MatchRoomSystem
    {
        [EntitySystem]
        private static void Awake(this MatchRoom self)
        {
            self.MatchSeed = 0;
            self.CurrentRound = 0;
            self.MatchState = MatchState.Waiting;
            self.FinalResults = new List<(long, int)>();
            self.LastRoundLosers = new List<long>();
        }

        [EntitySystem]
        private static void Destroy(this MatchRoom self)
        {
            self.FinalResults = null;
            self.LastRoundLosers = null;
        }

        public static void Init(this MatchRoom self, uint seed, List<long> playerIds)
        {
            self.MatchSeed = seed;
            self.CurrentRound = 0;
            self.MatchState = MatchState.Waiting;
            self.FinalResults.Clear();

            // 创建玩家
            foreach (long playerId in playerIds)
            {
                MatchPlayer player = self.AddChild<MatchPlayer, long>(playerId);
            }
        }

        public static void StartMatch(this MatchRoom self)
        {
            self.MatchState = MatchState.InGame;
            self.CurrentRound = 1;
        }

        public static void NextRound(this MatchRoom self)
        {
            self.CurrentRound++;
        }

        public static int GetAlivePlayerCount(this MatchRoom self)
        {
            int count = 0;
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        public static void EndMatch(this MatchRoom self)
        {
            self.MatchState = MatchState.Finished;

            // 最后存活的玩家获得第1名
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.IsAlive && player.Rank == 0)
                {
                    player.Rank = 1;
                    self.FinalResults.Add((player.PlayerId, 1));
                }
            }

            // 按 Rank 排序结果
            self.FinalResults.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        }

        public static void EliminatePlayer(this MatchRoom self, long playerId)
        {
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.PlayerId == playerId)
                {
                    player.IsAlive = false;
                    int aliveCount = self.GetAlivePlayerCount();
                    player.Rank = aliveCount + 1; // 当前存活人数+1 就是这个被淘汰者的排名
                    self.FinalResults.Add((playerId, player.Rank));
                    break;
                }
            }
        }

        public static int GetPopCap(this MatchRoom self)
        {
            int round = self.CurrentRound;
            int[] table = AutoChessDefine.PopCapByRound;
            if (round <= 0) return AutoChessDefine.InitialPopCap;
            int index = Math.Min(round - 1, table.Length - 1);
            return table[index];
        }

        /// <summary>
        /// 启动回合循环（协程，不等待）。
        /// 内部每个阶段会通过 NewContext 注入对应的 CancellationToken。
        /// </summary>
        public static void StartRoundLoop(this MatchRoom self)
        {
            RoundFSMComponent fsm = self.GetComponent<RoundFSMComponent>();
            fsm.StartRoundLoopAsync().Coroutine();
        }

        /// <summary>
        /// 枚举当前所有存活玩家。
        /// </summary>
        public static IEnumerable<MatchPlayer> GetAlivePlayers(this MatchRoom self)
        {
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.IsAlive)
                    yield return player;
            }
        }

        /// <summary>
        /// 按 PlayerId 查找玩家（不存在返回 null）。
        /// </summary>
        public static MatchPlayer FindPlayerById(this MatchRoom self, long playerId)
        {
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.PlayerId == playerId)
                    return player;
            }
            return null;
        }
    }
}
