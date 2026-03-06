using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    public static class PairingService
    {
        public static List<PairingResult> MakePairings(MatchRoom room, DeterministicRngComponent rng)
        {
            var results = new List<PairingResult>();
            var aliveIds = new List<long>();

            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                aliveIds.Add(player.PlayerId);
            }

            int aliveCount = aliveIds.Count;
            if (aliveCount < 2) return results;

            // Create a sub-RNG for pairing so we don't pollute the main stream
            uint pairSeed = rng.DeriveSubSeed(PrngPurpose.Pairing, room.CurrentRound);
            var pairRng = new SimpleRng(pairSeed);

            // Fisher-Yates shuffle
            for (int i = aliveCount - 1; i > 0; i--)
            {
                int j = pairRng.NextInt(0, i + 1);
                (aliveIds[i], aliveIds[j]) = (aliveIds[j], aliveIds[i]);
            }

            if (aliveCount == 4)
            {
                // 2 matches
                results.Add(CreatePairing(aliveIds[0], aliveIds[1]));
                results.Add(CreatePairing(aliveIds[2], aliveIds[3]));
            }
            else if (aliveCount == 3)
            {
                // 1 real match + 1 ghost match
                results.Add(CreatePairing(aliveIds[0], aliveIds[1]));

                if (room.LastGhost != null)
                {
                    long liveId = aliveIds[2];
                    long ghostId = room.LastGhost.PlayerId;
                    var ghostResult = new PairingResult();

                    if (liveId < ghostId)
                    {
                        ghostResult.LeftPlayerId = liveId;
                        ghostResult.RightPlayerId = ghostId;
                        ghostResult.GhostSide = 1; // R is ghost
                    }
                    else
                    {
                        ghostResult.LeftPlayerId = ghostId;
                        ghostResult.RightPlayerId = liveId;
                        ghostResult.GhostSide = 0; // L is ghost
                    }

                    ghostResult.IsGhostMatch = true;
                    results.Add(ghostResult);
                }
            }
            else if (aliveCount == 2)
            {
                results.Add(CreatePairing(aliveIds[0], aliveIds[1]));
            }

            return results;
        }

        private static PairingResult CreatePairing(long a, long b)
        {
            var result = new PairingResult();
            if (a < b)
            {
                result.LeftPlayerId = a;
                result.RightPlayerId = b;
            }
            else
            {
                result.LeftPlayerId = b;
                result.RightPlayerId = a;
            }

            return result;
        }
    }

}
