using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 自走棋基础系统集成验证。
    /// 在服务端启动后可手动调用各验证方法。
    /// </summary>
    public static class AutoChessTestHelper
    {
        /// <summary>
        /// 运行所有验证
        /// </summary>
        public static void RunAllTests(Scene scene)
        {
            TestConfigLoader();
            TestPrngDeterminism(scene);
            TestEntityTree(scene);
            TestPhaseGate();
            TestEconomy(scene);
            Log.Info("[AutoChess] All integration tests passed!");
        }

        /// <summary>
        /// 验证配置加载器数量正确
        /// </summary>
        public static void TestConfigLoader()
        {
            AutoChessConfigLoader.Init();

            if (AutoChessConfigLoader.UnitCount != 24)
                throw new Exception($"UnitCount expected 24, got {AutoChessConfigLoader.UnitCount}");
            if (AutoChessConfigLoader.SynergyCount != 13)
                throw new Exception($"SynergyCount expected 13, got {AutoChessConfigLoader.SynergyCount}");
            if (AutoChessConfigLoader.SkillCount != 16)
                throw new Exception($"SkillCount expected 16, got {AutoChessConfigLoader.SkillCount}");

            Log.Info("[AutoChess] ConfigLoader test passed: 24 units, 13 synergies, 16 skills");
        }

        /// <summary>
        /// 验证 PRNG 确定性（同 seed 100 次一致）。
        /// DeterministicRngComponent 是 ComponentOf(MatchRoom)，
        /// 所以需要通过 MatchComponent -> MatchRoom 来承载。
        /// </summary>
        public static void TestPrngDeterminism(Scene scene)
        {
            uint testSeed = 12345;
            List<long> dummyPlayers = new List<long> { 9001, 9002 };

            // 创建临时 MatchComponent + MatchRoom 来承载 PRNG
            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            MatchRoom room1 = MatchRoomFactory.CreateMatch(matchComp, dummyPlayers, testSeed);

            // 第一次运行：记录 100 次结果
            DeterministicRngComponent rng1 = room1.GetComponent<DeterministicRngComponent>();
            int[] results1 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                results1[i] = rng1.NextInt(0, 1000);
            }

            // 清理第一个 room
            scene.RemoveComponent<MatchComponent>();

            // 第二次运行（同 seed）
            MatchComponent matchComp2 = scene.AddComponent<MatchComponent>();
            MatchRoom room2 = MatchRoomFactory.CreateMatch(matchComp2, dummyPlayers, testSeed);
            DeterministicRngComponent rng2 = room2.GetComponent<DeterministicRngComponent>();

            for (int i = 0; i < 100; i++)
            {
                int val = rng2.NextInt(0, 1000);
                if (val != results1[i])
                    throw new Exception($"PRNG mismatch at index {i}: expected {results1[i]}, got {val}");
            }

            // 清理第二个
            scene.RemoveComponent<MatchComponent>();

            // 验证子种子派生
            uint sub1 = XxHash32.Hash(testSeed, 1, (int)PrngPurpose.ShopReroll);
            uint sub2 = XxHash32.Hash(testSeed, 1, (int)PrngPurpose.Combat);
            uint sub3 = XxHash32.Hash(testSeed, 1, (int)PrngPurpose.ShopReroll); // 同用途+同回合

            if (sub1 == sub2)
                throw new Exception("Different purposes should produce different sub-seeds");
            if (sub1 != sub3)
                throw new Exception("Same purpose+round should produce same sub-seed");

            Log.Info("[AutoChess] PRNG determinism test passed: 100 values consistent, sub-seeds correct");
        }

        /// <summary>
        /// 验证 Entity 树结构
        /// </summary>
        public static void TestEntityTree(Scene scene)
        {
            // 创建 MatchComponent
            MatchComponent matchComp = scene.AddComponent<MatchComponent>();

            // 创建 Match（4 玩家）
            List<long> playerIds = new List<long> { 1001, 1002, 1003, 1004 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 42);

            // 验证结构
            if (room == null) throw new Exception("MatchRoom is null");
            if (room.GetComponent<DeterministicRngComponent>() == null)
                throw new Exception("DeterministicRngComponent missing");
            if (room.GetComponent<RoundFSMComponent>() == null)
                throw new Exception("RoundFSMComponent missing");

            int playerCount = 0;
            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer) playerCount++;
            }
            if (playerCount != 4)
                throw new Exception($"Expected 4 players, got {playerCount}");

            // 验证淘汰和排名
            room.StartMatch();
            room.EliminatePlayer(1004); // 第4名
            room.EliminatePlayer(1003); // 第3名
            room.EliminatePlayer(1002); // 第2名

            if (room.GetAlivePlayerCount() != 1)
                throw new Exception($"Expected 1 alive, got {room.GetAlivePlayerCount()}");

            room.EndMatch();

            if (room.FinalResults.Count != 4)
                throw new Exception($"Expected 4 results, got {room.FinalResults.Count}");
            if (room.FinalResults[0].Rank != 1)
                throw new Exception($"First result should be rank 1, got {room.FinalResults[0].Rank}");

            // 清理
            scene.RemoveComponent<MatchComponent>();

            Log.Info("[AutoChess] Entity tree test passed: structure, elimination, and ranking correct");
        }

        /// <summary>
        /// 验证操作门控矩阵
        /// </summary>
        public static void TestPhaseGate()
        {
            // Deployment 阶段全允许
            AssertGate(RoundPhase.Deployment, OperationType.Buy, true, "Deployment+Buy");
            AssertGate(RoundPhase.Deployment, OperationType.Sell, true, "Deployment+Sell");
            AssertGate(RoundPhase.Deployment, OperationType.Place, true, "Deployment+Place");
            AssertGate(RoundPhase.Deployment, OperationType.Swap, true, "Deployment+Swap");
            AssertGate(RoundPhase.Deployment, OperationType.Merge, true, "Deployment+Merge");

            // Battle 阶段：Buy/Sell 允许，Place/Swap/Merge 禁止
            AssertGate(RoundPhase.Battle, OperationType.Buy, true, "Battle+Buy");
            AssertGate(RoundPhase.Battle, OperationType.Sell, true, "Battle+Sell");
            AssertGate(RoundPhase.Battle, OperationType.Place, false, "Battle+Place");
            AssertGate(RoundPhase.Battle, OperationType.Swap, false, "Battle+Swap");
            AssertGate(RoundPhase.Battle, OperationType.Merge, false, "Battle+Merge");

            // 其他阶段全禁止
            foreach (RoundPhase phase in new[] { RoundPhase.None, RoundPhase.RoundStart, RoundPhase.PreBattle, RoundPhase.RoundEnd })
            {
                foreach (OperationType op in Enum.GetValues(typeof(OperationType)))
                {
                    AssertGate(phase, op, false, $"{phase}+{op}");
                }
            }

            Log.Info("[AutoChess] PhaseGate test passed: all phase x operation combinations correct");
        }

        /// <summary>
        /// 验证经济系统：5 个收支操作、clamp 边界、日志记录。
        /// </summary>
        public static void TestEconomy(Scene scene)
        {
            // 建立临时 MatchComponent + MatchRoom（2 玩家，seed=77777）
            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 3001, 3002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 77777);

            MatchPlayer p = room.FindPlayerById(3001);
            if (p == null) throw new Exception("FindPlayerById(3001) returned null");

            // --- GiveRoundIncome: 0 + 4 = 4 ---
            EconomyService.GiveRoundIncome(p, 1);
            if (p.Elixir != 4)
                throw new Exception($"GiveRoundIncome: expected 4, got {p.Elixir}");

            // --- TryDeductBuy: 4 - 3 = 1 ---
            bool ok = EconomyService.TryDeductBuy(p, 3, 1);
            if (!ok) throw new Exception("TryDeductBuy should succeed");
            if (p.Elixir != 1) throw new Exception($"TryDeductBuy: expected 1, got {p.Elixir}");

            // --- TryDeductBuy: insufficient (cost=5, have=1) ---
            bool fail = EconomyService.TryDeductBuy(p, 5, 1);
            if (fail) throw new Exception("TryDeductBuy should fail (insufficient)");
            if (p.Elixir != 1) throw new Exception("Elixir should not change on failed buy");

            // --- GiveSell: +max(3-1,0) = +2 → 3 ---
            EconomyService.GiveSell(p, 3, 1);
            if (p.Elixir != 3) throw new Exception($"GiveSell: expected 3, got {p.Elixir}");

            // --- GiveMerge: +1 → 4 ---
            EconomyService.GiveMerge(p, 1);
            if (p.Elixir != 4) throw new Exception($"GiveMerge: expected 4, got {p.Elixir}");

            // --- GiveCommanderPassive: +randInt(4,8) ---
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            int before = p.Elixir;
            EconomyService.GiveCommanderPassive(p, rng, 2);
            int bonus = p.Elixir - before;
            if (bonus < 4 || bonus > 8)
                throw new Exception($"CommanderPassive bonus out of range [4,8]: got {bonus}");

            // --- Clamp: 998 + 4 = 999（不超过 MaxElixir）---
            p.Elixir = 998;
            EconomyService.GiveRoundIncome(p, 10);
            if (p.Elixir != 999)
                throw new Exception($"Clamp: expected 999, got {p.Elixir}");

            // --- Clamp: cost=1 sell → max(0,0) = 0, Elixir 不变 ---
            p.Elixir = 0;
            EconomyService.GiveSell(p, 1, 10); // max(1-1,0)=0，Elixir 不变
            if (p.Elixir != 0)
                throw new Exception($"Clamp zero: expected 0, got {p.Elixir}");

            // --- EconomyLog 记录验证 ---
            EconomyLogComponent log = p.GetComponent<EconomyLogComponent>();
            if (log == null) throw new Exception("EconomyLogComponent missing on player");

            int round1Count = 0;
            foreach (EconomyDelta d in log.GetDeltasByRound(1))
                round1Count++;
            // Round 1: GiveRoundIncome(1) + TryDeductBuy(ok)(1) + GiveSell(1) + GiveMerge(1) = 4 条
            if (round1Count != 4)
                throw new Exception($"Round 1 deltas: expected 4, got {round1Count}");

            // --- LastRoundLosers 初始为空 ---
            if (room.LastRoundLosers.Count != 0)
                throw new Exception("LastRoundLosers should be empty at start");

            // 清理
            scene.RemoveComponent<MatchComponent>();

            Log.Info("[AutoChess] Economy test passed: income/buy/sell/merge/passive/clamp/log all correct");
        }

        private static void AssertGate(RoundPhase phase, OperationType op, bool expected, string label)
        {
            bool actual = RoundFSMComponentSystem.CanOperate(phase, op);
            if (actual != expected)
                throw new Exception($"PhaseGate {label}: expected {expected}, got {actual}");
        }
    }
}
