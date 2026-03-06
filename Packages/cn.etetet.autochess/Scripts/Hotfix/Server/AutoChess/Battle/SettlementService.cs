using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(SynergyComponent))]
    public static class SettlementService
    {
        public static void ProcessResults(
            MatchRoom room,
            List<(PairingResult Pairing, CombatResult Result)> battleResults)
        {
            room.LastRoundLosers.Clear();

            // Track HP changes for tiebreaker
            var hpChanges = new List<(long PlayerId, int HpBefore, int HpAfter, int AliveEffective)>();

            foreach (var (pairing, result) in battleResults)
            {
                if (result.Winner == CombatWinner.Draw)
                {
                    // Draw: both sides lose 1 HP
                    ApplyHpLoss(room, pairing.LeftPlayerId, pairing, AutoChessDefine.DrawHpLoss, hpChanges, 0);
                    ApplyHpLoss(room, pairing.RightPlayerId, pairing, AutoChessDefine.DrawHpLoss, hpChanges, 0);
                }
                else if (result.Winner == CombatWinner.Left)
                {
                    // L wins, R loses
                    int damage = result.LeftAliveEffective + 1;

                    // If ghost match and ghost is L, winner is ghost — no one takes damage from ghost win
                    if (pairing.IsGhostMatch && pairing.GhostSide == 0)
                    {
                        // Ghost (L) won → live player (R) takes damage
                        ApplyHpLoss(room, pairing.RightPlayerId, pairing, damage, hpChanges, result.RightAliveEffective);
                    }
                    else if (pairing.IsGhostMatch && pairing.GhostSide == 1)
                    {
                        // Live player (L) won ghost (R) → no damage
                        RecordNoLoss(room, pairing.LeftPlayerId, hpChanges, result.LeftAliveEffective);
                    }
                    else
                    {
                        // Normal match
                        ApplyHpLoss(room, pairing.RightPlayerId, pairing, damage, hpChanges, result.RightAliveEffective);
                        room.LastRoundLosers.Add(pairing.RightPlayerId);
                    }
                }
                else // Right wins
                {
                    int damage = result.RightAliveEffective + 1;

                    if (pairing.IsGhostMatch && pairing.GhostSide == 1)
                    {
                        // Ghost (R) won → live player (L) takes damage
                        ApplyHpLoss(room, pairing.LeftPlayerId, pairing, damage, hpChanges, result.LeftAliveEffective);
                    }
                    else if (pairing.IsGhostMatch && pairing.GhostSide == 0)
                    {
                        // Live player (R) won ghost (L) → no damage
                        RecordNoLoss(room, pairing.RightPlayerId, hpChanges, result.RightAliveEffective);
                    }
                    else
                    {
                        ApplyHpLoss(room, pairing.LeftPlayerId, pairing, damage, hpChanges, result.LeftAliveEffective);
                        room.LastRoundLosers.Add(pairing.LeftPlayerId);
                    }
                }
            }

            // Collect newly eliminated players (HP <= 0)
            var newlyEliminated = new List<(long PlayerId, int Hp, int HpBefore, int AliveEffective)>();

            foreach (var change in hpChanges)
            {
                MatchPlayer player = room.FindPlayerById(change.PlayerId);
                if (player != null && player.IsAlive && player.Hp <= 0)
                {
                    newlyEliminated.Add((change.PlayerId, player.Hp, change.HpBefore, change.AliveEffective));
                }
            }

            // Sort eliminated by tiebreaker: hp DESC → hpBefore DESC → aliveEffective DESC → playerId ASC
            newlyEliminated.Sort((a, b) =>
            {
                int cmp = b.Hp.CompareTo(a.Hp); // hp DESC (higher = better rank)
                if (cmp != 0) return cmp;
                cmp = b.HpBefore.CompareTo(a.HpBefore);
                if (cmp != 0) return cmp;
                cmp = b.AliveEffective.CompareTo(a.AliveEffective);
                if (cmp != 0) return cmp;
                return a.PlayerId.CompareTo(b.PlayerId); // playerId ASC
            });

            // Record ghost snapshot from the last eliminated player
            if (newlyEliminated.Count > 0)
            {
                // Ghost = playerId 最大者 among same-round eliminations
                long ghostCandidateId = newlyEliminated[newlyEliminated.Count - 1].PlayerId;
                for (int i = 0; i < newlyEliminated.Count; i++)
                {
                    if (newlyEliminated[i].PlayerId > ghostCandidateId)
                        ghostCandidateId = newlyEliminated[i].PlayerId;
                }

                MatchPlayer ghostPlayer = room.FindPlayerById(ghostCandidateId);
                if (ghostPlayer != null)
                {
                    var ghostSnapshot = new GhostSnapshot { PlayerId = ghostCandidateId };
                    RosterComponent roster = ghostPlayer.GetComponent<RosterComponent>();
                    if (roster?.Units != null)
                    {
                        foreach (UnitInfo u in roster.Units)
                        {
                            if (u.Row >= 0) // board only
                            {
                                ghostSnapshot.Units.Add(u);
                            }
                        }
                    }

                    SynergyComponent synergy = ghostPlayer.GetComponent<SynergyComponent>();
                    ghostSnapshot.Snapshot = synergy?.Snapshot;
                    room.LastGhost = ghostSnapshot;
                }
            }

            // Eliminate in tiebreaker order (worst rank first = last in sorted list)
            for (int i = newlyEliminated.Count - 1; i >= 0; i--)
            {
                room.EliminatePlayer(newlyEliminated[i].PlayerId);
            }
        }

        private static void ApplyHpLoss(
            MatchRoom room, long playerId, PairingResult pairing,
            int damage, List<(long, int, int, int)> hpChanges, int aliveEffective)
        {
            MatchPlayer player = room.FindPlayerById(playerId);
            if (player == null || !player.IsAlive) return;
            if (pairing.IsGhostMatch && IsGhostPlayer(pairing, playerId)) return;

            int hpBefore = player.Hp;
            player.Hp -= damage;
            hpChanges.Add((playerId, hpBefore, player.Hp, aliveEffective));
        }

        private static void RecordNoLoss(
            MatchRoom room, long playerId,
            List<(long, int, int, int)> hpChanges, int aliveEffective)
        {
            MatchPlayer player = room.FindPlayerById(playerId);
            if (player == null) return;
            hpChanges.Add((playerId, player.Hp, player.Hp, aliveEffective));
        }

        private static bool IsGhostPlayer(PairingResult pairing, long playerId)
        {
            if (!pairing.IsGhostMatch) return false;
            if (pairing.GhostSide == 0) return playerId == pairing.LeftPlayerId;
            if (pairing.GhostSide == 1) return playerId == pairing.RightPlayerId;
            return false;
        }
    }
}
