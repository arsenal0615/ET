using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 监听 PhaseChangedEvent：当阶段切换到 Battle 时，执行配对→战斗模拟→结算→广播。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(SynergyComponent))]
    [FriendOf(typeof(RosterComponent))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Battle : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            if (args.NewPhase != RoundPhase.Battle)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchComponent matchComp = scene.GetComponent<MatchComponent>();
            if (matchComp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchRoom room = matchComp.GetRoom(args.MatchRoomId);
            if (room == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();

            // 1. Pairing
            List<PairingResult> pairings = PairingService.MakePairings(room, rng);

            // 2. Run combat for each pairing
            var battleResults = new List<(PairingResult, CombatResult)>();
            int pairIndex = 0;

            foreach (PairingResult pairing in pairings)
            {
                var leftUnits = new List<CombatUnitState>();
                var rightUnits = new List<CombatUnitState>();
                TraitSnapshot leftSnapshot = null;
                TraitSnapshot rightSnapshot = null;

                // Init left side
                if (pairing.IsGhostMatch && pairing.GhostSide == 0)
                {
                    CombatUnitInit.InitFromGhost(room.LastGhost, 0, leftUnits);
                    leftSnapshot = room.LastGhost?.Snapshot;
                }
                else
                {
                    MatchPlayer leftPlayer = room.FindPlayerById(pairing.LeftPlayerId);
                    if (leftPlayer != null)
                    {
                        RosterComponent leftRoster = leftPlayer.GetComponent<RosterComponent>();
                        SynergyComponent leftSynergy = leftPlayer.GetComponent<SynergyComponent>();
                        leftSnapshot = leftSynergy?.Snapshot;
                        CombatUnitInit.InitSide(leftRoster, leftSnapshot, 0, leftUnits);
                    }
                }

                // Init right side
                if (pairing.IsGhostMatch && pairing.GhostSide == 1)
                {
                    CombatUnitInit.InitFromGhost(room.LastGhost, 1, rightUnits);
                    rightSnapshot = room.LastGhost?.Snapshot;
                }
                else
                {
                    MatchPlayer rightPlayer = room.FindPlayerById(pairing.RightPlayerId);
                    if (rightPlayer != null)
                    {
                        RosterComponent rightRoster = rightPlayer.GetComponent<RosterComponent>();
                        SynergyComponent rightSynergy = rightPlayer.GetComponent<SynergyComponent>();
                        rightSnapshot = rightSynergy?.Snapshot;
                        CombatUnitInit.InitSide(rightRoster, rightSnapshot, 1, rightUnits);
                    }
                }

                // Run combat
                CombatResult result = CombatSimulator.RunCombat(
                    leftUnits, rightUnits,
                    leftSnapshot, rightSnapshot,
                    rng, args.Round * 100 + pairIndex);

                battleResults.Add((pairing, result));
                pairIndex++;
            }

            // 3. 广播每场战斗的 CombatEvents 给参战双方
            foreach (var (pairing, result) in battleResults)
            {
                // 转换战斗事件
                var eventProtos = new List<AutoChessCombatEventProto>();
                foreach (CombatEvent e in result.Events)
                {
                    eventProtos.Add(AutoChessProtoHelper.ToCombatEventProto(e));
                }

                // 给左侧玩家发送（如果不是 Ghost）
                if (!pairing.IsGhostMatch || pairing.GhostSide != 0)
                {
                    MatchPlayer leftPlayer = room.FindPlayerById(pairing.LeftPlayerId);
                    if (leftPlayer != null && leftPlayer.IsAlive)
                    {
                        long leftOpponentId = pairing.IsGhostMatch && pairing.GhostSide == 1
                            ? (room.LastGhost != null ? room.LastGhost.PlayerId : 0)
                            : pairing.RightPlayerId;

                        M2C_AutoChessCombatEvents leftMsg = M2C_AutoChessCombatEvents.Create();
                        leftMsg.OpponentId = leftOpponentId;
                        leftMsg.Events.AddRange(eventProtos);
                        (leftMsg as MessageObject).IsFromPool = false;
                        AutoChessBroadcastHelper.SendToPlayer(scene, leftPlayer, leftMsg);
                    }
                }

                // 给右侧玩家发送（如果不是 Ghost）
                if (!pairing.IsGhostMatch || pairing.GhostSide != 1)
                {
                    MatchPlayer rightPlayer = room.FindPlayerById(pairing.RightPlayerId);
                    if (rightPlayer != null && rightPlayer.IsAlive)
                    {
                        long rightOpponentId = pairing.IsGhostMatch && pairing.GhostSide == 0
                            ? (room.LastGhost != null ? room.LastGhost.PlayerId : 0)
                            : pairing.LeftPlayerId;

                        M2C_AutoChessCombatEvents rightMsg = M2C_AutoChessCombatEvents.Create();
                        rightMsg.OpponentId = rightOpponentId;
                        rightMsg.Events.AddRange(eventProtos);
                        (rightMsg as MessageObject).IsFromPool = false;
                        AutoChessBroadcastHelper.SendToPlayer(scene, rightPlayer, rightMsg);
                    }
                }
            }

            // 4. 记录结算前各玩家 HP（用于计算 HpChange）
            var hpBefore = new Dictionary<long, int>();
            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                hpBefore[player.PlayerId] = player.Hp;
            }

            // 5. Settlement
            SettlementService.ProcessResults(room, battleResults);

            // 6. 广播 RoundResult
            M2C_AutoChessRoundResult roundResultMsg = M2C_AutoChessRoundResult.Create();

            foreach (var (pairing, result) in battleResults)
            {
                // 左侧（非 Ghost）
                if (!pairing.IsGhostMatch || pairing.GhostSide != 0)
                {
                    long leftOpponentId = pairing.IsGhostMatch && pairing.GhostSide == 1
                        ? (room.LastGhost != null ? room.LastGhost.PlayerId : 0)
                        : pairing.RightPlayerId;
                    CollectPlayerRoundResult(room, roundResultMsg, hpBefore, pairing.LeftPlayerId, leftOpponentId, result, true);
                }

                // 右侧（非 Ghost）
                if (!pairing.IsGhostMatch || pairing.GhostSide != 1)
                {
                    long rightOpponentId = pairing.IsGhostMatch && pairing.GhostSide == 0
                        ? (room.LastGhost != null ? room.LastGhost.PlayerId : 0)
                        : pairing.LeftPlayerId;
                    CollectPlayerRoundResult(room, roundResultMsg, hpBefore, pairing.RightPlayerId, rightOpponentId, result, false);
                }
            }

            // 收集淘汰玩家（HP <= 0 且不再存活）
            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer mp && !mp.IsAlive && hpBefore.ContainsKey(mp.PlayerId))
                {
                    // 本回合从存活变为淘汰
                    roundResultMsg.EliminatedPlayerIds.Add(mp.PlayerId);
                }
            }

            AutoChessBroadcastHelper.BroadcastToAlive(scene, room, roundResultMsg);

            // 7. Check if match should end (1 or 0 alive)
            if (room.GetAlivePlayerCount() <= 1)
            {
                room.EndMatch();

                // 广播 MatchResult 给所有玩家（含已淘汰）
                M2C_AutoChessMatchResult matchResultMsg = M2C_AutoChessMatchResult.Create();
                foreach (var (playerId, rank) in room.FinalResults)
                {
                    MatchPlayer p = room.FindPlayerById(playerId);
                    if (p != null)
                    {
                        matchResultMsg.Rankings.Add(AutoChessProtoHelper.ToPlayerPublicInfoProto(p));
                    }
                }

                AutoChessBroadcastHelper.BroadcastToAll(scene, room, matchResultMsg);
            }

            await ETTask.CompletedTask;
        }

        private static void CollectPlayerRoundResult(
            MatchRoom room,
            M2C_AutoChessRoundResult roundResultMsg,
            Dictionary<long, int> hpBefore,
            long playerId,
            long opponentId,
            CombatResult result,
            bool isLeftSide)
        {
            MatchPlayer p = room.FindPlayerById(playerId);
            if (p == null) return;

            bool won;
            if (result.Winner == CombatWinner.Draw)
            {
                won = false;
            }
            else
            {
                won = (isLeftSide && result.Winner == CombatWinner.Left)
                   || (!isLeftSide && result.Winner == CombatWinner.Right);
            }

            int hpBeforeVal = hpBefore.ContainsKey(playerId) ? hpBefore[playerId] : p.Hp;
            int hpChange = p.Hp - hpBeforeVal;

            AutoChessPlayerRoundResultProto resultProto = AutoChessPlayerRoundResultProto.Create();
            resultProto.PlayerId = playerId;
            resultProto.OpponentId = opponentId;
            resultProto.Won = won;
            resultProto.HpChange = hpChange;
            resultProto.HpAfter = p.Hp;
            roundResultMsg.Results.Add(resultProto);
        }
    }
}
