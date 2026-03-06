using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 自走棋基础系统集成验证。
    /// 在服务端启动后可手动调用各验证方法。
    /// </summary>
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(SynergyComponent))]
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
            TestShop(scene);
            TestRoster(scene);
            TestPlacement(scene);
            TestMerge(scene);
            TestPreBattle(scene);
            TestUnitService(scene);
            TestEliminationRecovery(scene);
            TestSynergyCounting(scene);
            TestSynergySnapshot(scene);
            TestGoblinGift(scene);
            TestHexUtil();
            TestMana();
            TestTriggerChecker();
            TestTargetSelector();
            TestEffectApplier();
            TestSkillExecutor();
            TestSkillSystem();
            TestCombatSimulator();
            TestPairing(scene);
            TestSettlement(scene);
            TestCombatStartProcessor();
            TestFrenzy();
            TestProtoConversion();
            TestBroadcastHelper(scene);
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

        /// <summary>
        /// 验证商店与共享卡池：初始化、Offer 生成、购买、出售、礼包。
        /// </summary>
        public static void TestShop(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 5001, 5002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 99999);
            room.StartMatch();

            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();

            if (pool == null) throw new Exception("SharedPoolComponent missing on room");

            // --- 1. 卡池初始化：24 种各 8 份 ---
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
            {
                if (pool.Remaining[i] != AutoChessDefine.CopiesPerUnit)
                    throw new Exception($"Pool init: template {i + 1} expected {AutoChessDefine.CopiesPerUnit}, got {pool.Remaining[i]}");
            }

            // --- 2. ShopComponent 存在，PlayerIndex 正确 ---
            MatchPlayer p1 = room.FindPlayerById(5001);
            MatchPlayer p2 = room.FindPlayerById(5002);
            ShopComponent shop1 = p1.GetComponent<ShopComponent>();
            if (shop1 == null) throw new Exception("ShopComponent missing on player 5001");
            if (p1.PlayerIndex != 0) throw new Exception($"PlayerIndex p1 expected 0, got {p1.PlayerIndex}");
            if (p2.PlayerIndex != 1) throw new Exception($"PlayerIndex p2 expected 1, got {p2.PlayerIndex}");

            // --- 3. 生成 Offer：3 槽非空、无重复、来自 remaining>0 的模板 ---
            ShopService.GenerateOffersForPlayer(p1, pool, rng);
            int[] offerIds = new int[3];
            for (int i = 0; i < 3; i++)
            {
                offerIds[i] = shop1.Slots[i].TemplateId;
                if (offerIds[i] <= 0) throw new Exception($"Slot {i} is empty after GenerateOffers");
            }
            if (offerIds[0] == offerIds[1] || offerIds[1] == offerIds[2] || offerIds[0] == offerIds[2])
                throw new Exception("Offer slots contain duplicate templateIds");

            // --- 4. 预扣验证：3 个模板的 Remaining 各减了 1 ---
            foreach (int tid in offerIds)
            {
                int expected = AutoChessDefine.CopiesPerUnit - 1;
                if (pool.Remaining[tid - 1] != expected)
                    throw new Exception($"Pre-deduct: template {tid} expected {expected}, got {pool.Remaining[tid - 1]}");
            }

            // --- 5. 购买：圣水减少，全行重刷，旧 Offer 归还（除被买走的那个） ---
            p1.Elixir = 10;
            int slot0Template = shop1.Slots[0].TemplateId;
            int slot0Cost = AutoChessConfigLoader.GetUnit(slot0Template).Cost;
            int buyResult = ShopService.TryBuy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
            if (buyResult != slot0Template)
                throw new Exception($"TryBuy: expected templateId {slot0Template}, got {buyResult}");
            if (p1.Elixir != 10 - slot0Cost)
                throw new Exception($"TryBuy: elixir expected {10 - slot0Cost}, got {p1.Elixir}");
            // 买完后全行刷新，slot0Template 被消耗，其余两个旧 Offer 已归还
            if (pool.Remaining[slot0Template - 1] != AutoChessDefine.CopiesPerUnit - 1)
                throw new Exception($"After buy: bought template pool should be CopiesPerUnit-1");

            // --- 6. 购买失败：圣水不足 ---
            p1.Elixir = 0;
            int failResult = ShopService.TryBuy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
            if (failResult != -1) throw new Exception("TryBuy should fail when elixir=0");
            if (p1.Elixir != 0) throw new Exception("Elixir should not change on failed buy");

            // --- 7. 出售：圣水返还，卡池回流（1★） ---
            p1.Elixir = 0;
            int sellTemplateId = shop1.Slots[0].TemplateId; // 当前 slot 0 的模板
            if (sellTemplateId <= 0) sellTemplateId = 1; // fallback（测试用）
            int beforeRemaining = pool.Remaining[sellTemplateId - 1];
            int sellCost = AutoChessConfigLoader.GetUnit(sellTemplateId).Cost;
            ShopService.TrySell(p1, sellTemplateId, 1, false, pool, 1);
            int expectedRefund = Math.Max(sellCost - 1, 0);
            if (p1.Elixir != expectedRefund)
                throw new Exception($"TrySell: elixir expected {expectedRefund}, got {p1.Elixir}");
            if (pool.Remaining[sellTemplateId - 1] != beforeRemaining + 1)
                throw new Exception($"TrySell: pool not refilled for 1-star unit");

            // --- 8. 礼品出售不回流 ---
            int giftTemplate = 1;
            int beforeGiftRemaining = pool.Remaining[giftTemplate - 1];
            ShopService.TrySell(p1, giftTemplate, 1, true, pool, 1);
            if (pool.Remaining[giftTemplate - 1] != beforeGiftRemaining)
                throw new Exception("TrySell: gift unit should NOT refill pool");

            // --- 9. 首回合礼包：2 费单位，不扣卡池 ---
            int poolTotalBefore = 0;
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
                poolTotalBefore += pool.Remaining[i];

            FirstRoundGiftService.SelectGiftTemplate(p1, rng);
            ShopComponent shop = p1.GetComponent<ShopComponent>();
            int giftId = shop.FirstRoundGiftTemplateId;
            if (giftId <= 0) throw new Exception("FirstRoundGift templateId should be > 0");
            if (AutoChessConfigLoader.GetUnit(giftId).Cost != 2)
                throw new Exception($"FirstRoundGift should be cost-2, got cost={AutoChessConfigLoader.GetUnit(giftId).Cost}");

            int poolTotalAfter = 0;
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
                poolTotalAfter += pool.Remaining[i];
            if (poolTotalAfter != poolTotalBefore)
                throw new Exception("FirstRoundGift should NOT deduct pool");

            // --- 10. 淘汰时归还 Offer ---
            // p2 生成 Offer 后淘汰，验证预扣被归还
            ShopService.GenerateOffersForPlayer(p2, pool, rng);
            ShopComponent shop2 = p2.GetComponent<ShopComponent>();
            int p2Offer0 = shop2.Slots[0].TemplateId;
            int beforeElimRemaining = pool.Remaining[p2Offer0 - 1];
            room.EliminatePlayer(5002);
            if (pool.Remaining[p2Offer0 - 1] != beforeElimRemaining + 1)
                throw new Exception("EliminatePlayer should return p2's pre-deducted offers to pool");

            scene.RemoveComponent<MatchComponent>();

            Log.Info("[AutoChess] Shop test passed: pool init / offer generation / buy / sell / gift / elimination all correct");
        }

        /// <summary>
        /// 验证单位 Roster 管理：添加、移除、查找、统计。
        /// </summary>
        public static void TestRoster(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 6001, 6002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 88888);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(6001);
            RosterComponent roster = p1.GetComponent<RosterComponent>();
            if (roster == null) throw new Exception("RosterComponent missing on player");

            // --- 1. AddToBench: 添加到板凳最小空位 ---
            UnitInfo u1 = RosterService.AddToBench(p1, 1, 1, false);   // templateId=1, star=1
            if (u1 == null) throw new Exception("AddToBench returned null");
            if (u1.InstId != 1) throw new Exception($"First unit InstId should be 1, got {u1.InstId}");
            if (u1.Row != -1) throw new Exception("Bench unit Row should be -1");
            if (u1.Col != 0) throw new Exception("First bench unit Col should be 0");

            UnitInfo u2 = RosterService.AddToBench(p1, 2, 1, false);
            if (u2.Col != 1) throw new Exception("Second bench unit Col should be 1");

            // --- 2. 板凳满（5 个）后添加失败 ---
            RosterService.AddToBench(p1, 3, 1, false); // col=2
            RosterService.AddToBench(p1, 4, 1, false); // col=3
            RosterService.AddToBench(p1, 5, 1, false); // col=4
            UnitInfo overflow = RosterService.AddToBench(p1, 6, 1, false);
            if (overflow != null) throw new Exception("AddToBench should return null when bench full");

            // --- 3. 查找 ---
            UnitInfo found = RosterService.FindAt(p1, 0, -1);  // bench col=0
            if (found != u1) throw new Exception("FindAt bench(0,-1) should return u1");

            UnitInfo byId = RosterService.FindByInstId(p1, 2);
            if (byId != u2) throw new Exception("FindByInstId(2) should return u2");

            // --- 4. 统计 ---
            if (RosterService.GetPopUsed(p1) != 0)
                throw new Exception("PopUsed should be 0 (all on bench)");
            if (RosterService.GetBenchUsed(p1) != 5)
                throw new Exception("BenchUsed should be 5");

            // --- 5. Remove ---
            RosterService.Remove(p1, u1);
            if (RosterService.FindByInstId(p1, 1) != null)
                throw new Exception("u1 should be removed");
            if (RosterService.GetBenchUsed(p1) != 4)
                throw new Exception("BenchUsed should be 4 after remove");

            // --- 6. AddToBench 填补空位（col=0 空出来了）---
            UnitInfo u6 = RosterService.AddToBench(p1, 6, 1, false);
            if (u6.Col != 0) throw new Exception($"Should fill col=0 gap, got col={u6.Col}");

            // --- 7. isGift 标记保留 ---
            RosterService.Remove(p1, u6);  // 清出空位
            UnitInfo gift = RosterService.AddToBench(p1, 7, 1, true);
            if (!gift.IsGift) throw new Exception("Gift unit IsGift should be true");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] Roster test passed: add/remove/find/stats all correct");
        }

        /// <summary>
        /// 验证摆位操作：Place/Move/Swap + 门控 + 校验。
        /// </summary>
        public static void TestPlacement(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 7001, 7002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 55555);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(7001);
            int popCap = room.GetPopCap(); // Round 1 → 3

            // 准备：板凳放 3 个单位
            UnitInfo u1 = RosterService.AddToBench(p1, 1, 1, false);
            UnitInfo u2 = RosterService.AddToBench(p1, 2, 1, false);
            UnitInfo u3 = RosterService.AddToBench(p1, 3, 1, false);

            // --- 1. PlaceToBoard: 板凳→棋盘 ---
            bool ok = PlacementService.TryPlaceToBoard(p1, u1.InstId, 0, 0, popCap);
            if (!ok) throw new Exception("PlaceToBoard should succeed");
            if (u1.Row != 0 || u1.Col != 0) throw new Exception("u1 should be at (0,0)");
            if (RosterService.GetPopUsed(p1) != 1) throw new Exception("PopUsed should be 1");

            // --- 2. PlaceToBoard: 超人口失败 ---
            PlacementService.TryPlaceToBoard(p1, u2.InstId, 1, 0, popCap);
            PlacementService.TryPlaceToBoard(p1, u3.InstId, 2, 0, popCap);
            UnitInfo u4 = RosterService.AddToBench(p1, 4, 1, false);
            if (u4 == null) throw new Exception("AddToBench for u4 should succeed");
            bool overPop = PlacementService.TryPlaceToBoard(p1, u4.InstId, 3, 0, popCap);
            if (overPop) throw new Exception("PlaceToBoard should fail (popCap exceeded)");

            // --- 3. PlaceToBoard: 目标格被占 ---
            bool occupied = PlacementService.TryPlaceToBoard(p1, u4.InstId, 0, 0, 10); // popCap 放开
            if (occupied) throw new Exception("PlaceToBoard should fail (target occupied)");

            // --- 4. MoveOnBoard: 棋盘内移动 ---
            bool moveOk = PlacementService.TryMoveOnBoard(p1, u1.InstId, 4, 2);
            if (!moveOk) throw new Exception("MoveOnBoard should succeed");
            if (u1.Col != 4 || u1.Row != 2) throw new Exception("u1 should be at (4,2)");

            // --- 5. MoveOnBoard: 非棋盘单位失败 ---
            bool moveBench = PlacementService.TryMoveOnBoard(p1, u4.InstId, 5, 0);
            if (moveBench) throw new Exception("MoveOnBoard should fail (unit on bench)");

            // --- 6. Swap: 棋盘↔棋盘 ---
            bool swapOk = PlacementService.TrySwap(p1, u1.InstId, u2.InstId);
            if (!swapOk) throw new Exception("Swap board↔board should succeed");
            // u1 was at (4,2), u2 was at (1,0) → swapped
            if (u1.Col != 1 || u1.Row != 0) throw new Exception("u1 should be at u2's old pos");
            if (u2.Col != 4 || u2.Row != 2) throw new Exception("u2 should be at u1's old pos");

            // --- 7. Swap: 棋盘↔板凳 ---
            bool swapMixed = PlacementService.TrySwap(p1, u1.InstId, u4.InstId);
            if (!swapMixed) throw new Exception("Swap board↔bench should succeed");

            // --- 8. Swap: 同一个单位失败 ---
            bool swapSelf = PlacementService.TrySwap(p1, u1.InstId, u1.InstId);
            if (swapSelf) throw new Exception("Swap same unit should fail");

            // --- 9. 坐标越界失败 ---
            bool outOfBounds = PlacementService.TryPlaceToBoard(p1, u1.InstId, 10, 10, 10);
            if (outOfBounds) throw new Exception("PlaceToBoard out of bounds should fail");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] Placement test passed: place/move/swap/validation all correct");
        }

        /// <summary>
        /// 验证合成：基础合成、连锁合成、圣水返还、星级上限、确定性。
        /// </summary>
        public static void TestMerge(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 8001, 8002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 66666);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(8001);
            p1.Elixir = 0;

            // --- 1. 基础合成：2 个 1★ 同模板 → 1 个 2★ ---
            UnitInfo a1 = RosterService.AddToBench(p1, 1, 1, false);
            UnitInfo a2 = RosterService.AddToBench(p1, 1, 1, false);
            int merges = MergeService.RunMergeChain(p1, 1);
            if (merges != 1) throw new Exception($"Expected 1 merge, got {merges}");

            // primary（先扫到的，InstId 较小）保留，star=2
            RosterComponent roster = p1.GetComponent<RosterComponent>();
            if (roster.Units.Count != 1) throw new Exception($"Expected 1 unit after merge, got {roster.Units.Count}");
            UnitInfo merged = roster.Units[0];
            if (merged.Star != 2) throw new Exception($"Merged unit should be 2★, got {merged.Star}");
            if (merged.InstId != a1.InstId) throw new Exception("Primary (lower InstId) should survive");

            // 圣水 +1
            if (p1.Elixir != 1) throw new Exception($"Elixir should be 1 after merge, got {p1.Elixir}");

            // --- 2. 不同模板不合成 ---
            RosterService.AddToBench(p1, 2, 1, false);
            RosterService.AddToBench(p1, 3, 1, false);
            int noMerge = MergeService.RunMergeChain(p1, 1);
            if (noMerge != 0) throw new Exception("Different templates should not merge");

            // --- 3. 连锁合成：4 个 1★ → 2 个 2★ → 不合成（已有一个2★=3个） ---
            // 清空
            roster.Units.Clear();
            p1.Elixir = 0;

            // 已有 1 个 2★（from test 1 scenario, 重建）
            UnitInfo b1 = RosterService.AddToBench(p1, 5, 1, false);
            UnitInfo b2 = RosterService.AddToBench(p1, 5, 1, false);
            UnitInfo b3 = RosterService.AddToBench(p1, 5, 1, false);
            UnitInfo b4 = RosterService.AddToBench(p1, 5, 1, false);

            int chainMerges = MergeService.RunMergeChain(p1, 1);
            // 4 个 1★ → 第一对合成为 2★ (+1圣水) → 第二对合成为 2★ (+1圣水)
            // → 2 个 2★ → 合成为 3★ (+1圣水) = 共 3 次合成
            if (chainMerges != 3) throw new Exception($"Chain merge: expected 3 merges, got {chainMerges}");
            if (roster.Units.Count != 1) throw new Exception($"Chain merge: expected 1 unit, got {roster.Units.Count}");
            if (roster.Units[0].Star != 3) throw new Exception($"Chain merge: expected 3★, got {roster.Units[0].Star}");
            if (p1.Elixir != 3) throw new Exception($"Chain merge: expected 3 elixir, got {p1.Elixir}");

            // --- 4. MaxStarLevel 限制：3★ 不再合成 ---
            roster.Units.Clear();
            RosterService.AddToBench(p1, 10, 3, false);
            RosterService.AddToBench(p1, 10, 3, false);
            int maxMerge = MergeService.RunMergeChain(p1, 1);
            if (maxMerge != 0) throw new Exception("3★ units should not merge further");

            // --- 5. 合成保持位置（primary 位置不变）---
            roster.Units.Clear();
            UnitInfo boardUnit = RosterService.AddToBench(p1, 8, 1, false);
            boardUnit.Col = 3; boardUnit.Row = 2; // 模拟放到棋盘
            UnitInfo benchUnit = RosterService.AddToBench(p1, 8, 1, false);
            MergeService.RunMergeChain(p1, 1);
            UnitInfo survivor = roster.Units[0];
            if (survivor.Col != 3 || survivor.Row != 2)
                throw new Exception("Primary should keep its position after merge");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] Merge test passed: basic/chain/maxStar/position all correct");
        }

        /// <summary>
        /// 验证 PreBattle 校验：超人口修正、板凳满强制出售。
        /// </summary>
        public static void TestPreBattle(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 9001, 9002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 77777);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(9001);
            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            p1.Elixir = 100;
            int popCap = 2; // 强制低人口上限以测试

            // --- 1. 超人口修正：3 个棋盘单位，popCap=2，应移 1 个到板凳 ---
            UnitInfo u1 = RosterService.AddToBench(p1, 1, 2, false); // 2★ cost-2 (高优先)
            u1.Col = 0; u1.Row = 0;
            UnitInfo u2 = RosterService.AddToBench(p1, 8, 1, false); // 1★ cost-3 (中优先)
            u2.Col = 1; u2.Row = 0;
            UnitInfo u3 = RosterService.AddToBench(p1, 2, 1, false); // 1★ cost-2 (低优先)
            u3.Col = 2; u3.Row = 0;
            // 手动修正 Row（AddToBench 设为 -1，需要模拟棋盘状态）
            // 注意：上面用了 AddToBench 然后手动设了 Row >= 0 来模拟棋盘单位

            PreBattleService.ValidateAndFix(p1, pool, popCap, 1);
            int boardCount = RosterService.GetPopUsed(p1);
            if (boardCount != popCap)
                throw new Exception($"After PreBattle fix: expected {popCap} on board, got {boardCount}");

            // u3 应该被移到板凳（最低优先级：1★ cost-2）
            if (u3.Row != -1)
                throw new Exception("Lowest priority unit should be moved to bench");

            // --- 2. 板凳满 + 超人口 → 强制出售 ---
            RosterComponent roster = p1.GetComponent<RosterComponent>();
            roster.Units.Clear();
            p1.Elixir = 0;

            // 棋盘放 3 个，板凳放满 5 个
            for (int i = 0; i < 3; i++)
            {
                UnitInfo bu = RosterService.AddToBench(p1, i + 1, 1, false);
                bu.Col = i; bu.Row = 0; // 放到棋盘
            }
            for (int i = 0; i < 5; i++)
            {
                RosterService.AddToBench(p1, i + 10, 1, false);
            }

            int poolTotalBefore = 0;
            for (int k = 0; k < AutoChessDefine.TotalUnitTemplates; k++)
                poolTotalBefore += pool.Remaining[k];

            PreBattleService.ValidateAndFix(p1, pool, popCap, 1);

            // 棋盘应该只有 popCap 个
            if (RosterService.GetPopUsed(p1) != popCap)
                throw new Exception($"After forced sell: expected {popCap} on board");

            // 强制出售的单位应该回流了卡池
            int poolTotalAfter = 0;
            for (int k = 0; k < AutoChessDefine.TotalUnitTemplates; k++)
                poolTotalAfter += pool.Remaining[k];
            if (poolTotalAfter <= poolTotalBefore)
                throw new Exception("Forced sell should return units to pool");

            // 强制出售应该给了圣水
            if (p1.Elixir <= 0)
                throw new Exception("Forced sell should give elixir refund");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] PreBattle test passed: over-pop fix / forced sell all correct");
        }

        /// <summary>
        /// 验证 UnitService 门面：Buy 创建 UnitInfo + Sell 移除 UnitInfo。
        /// </summary>
        public static void TestUnitService(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 10001, 10002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 11111);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(10001);
            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            p1.Elixir = 50;

            // 生成商店 Offer
            ShopService.GenerateOffersForPlayer(p1, pool, rng);
            ShopComponent shop = p1.GetComponent<ShopComponent>();
            int slot0Template = shop.Slots[0].TemplateId;

            // --- 1. Buy：创建 UnitInfo 入板凳 ---
            bool buyOk = UnitService.Buy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
            if (!buyOk) throw new Exception("UnitService.Buy should succeed");
            if (RosterService.GetBenchUsed(p1) != 1) throw new Exception("Should have 1 bench unit after buy");

            // 验证 UnitInfo 属性
            RosterComponent roster = p1.GetComponent<RosterComponent>();
            UnitInfo bought = roster.Units[0];
            if (bought.TemplateId != slot0Template) throw new Exception("Bought unit templateId mismatch");
            if (bought.Star != 1) throw new Exception("Bought unit should be 1★");
            if (bought.IsGift) throw new Exception("Bought unit should not be gift");
            if (bought.Row != -1) throw new Exception("Bought unit should be on bench");

            // --- 2. Sell：移除 UnitInfo + 经济/卡池处理 ---
            int elixirBefore = p1.Elixir;
            int poolBefore = pool.Remaining[bought.TemplateId - 1];
            UnitService.Sell(p1, bought, pool, 1);
            if (RosterService.GetBenchUsed(p1) != 0) throw new Exception("Should have 0 bench units after sell");
            if (p1.Elixir <= elixirBefore) throw new Exception("Sell should refund elixir");
            if (pool.Remaining[bought.TemplateId - 1] != poolBefore + 1) throw new Exception("Sell should return 1 copy to pool");

            // --- 3. Buy 失败：圣水不足 ---
            p1.Elixir = 0;
            ShopService.GenerateOffersForPlayer(p1, pool, rng);
            bool buyFail = UnitService.Buy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
            if (buyFail) throw new Exception("UnitService.Buy should fail when no elixir");
            if (RosterService.GetBenchUsed(p1) != 0) throw new Exception("No unit should be created on failed buy");

            // --- 4. Buy 失败：板凳满（5 个）---
            p1.Elixir = 50;
            for (int i = 0; i < 5; i++)
                RosterService.AddToBench(p1, i + 1, 1, false);
            ShopService.GenerateOffersForPlayer(p1, pool, rng);
            int elixirBeforeFull = p1.Elixir;
            bool buyFullBench = UnitService.Buy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
            if (buyFullBench) throw new Exception("UnitService.Buy should fail when bench full");
            if (p1.Elixir != elixirBeforeFull) throw new Exception("Elixir should not change when bench full");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] UnitService test passed: buy/sell integration correct");
        }

        /// <summary>
        /// 验证淘汰时回收棋盘/板凳单位到卡池。
        /// </summary>
        public static void TestEliminationRecovery(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 11001, 11002 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 44444);
            room.StartMatch();

            MatchPlayer p2 = room.FindPlayerById(11002);
            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();

            // 给 p2 放几个单位
            UnitInfo b1 = RosterService.AddToBench(p2, 1, 1, false);  // 1★ → 回流 1 份
            UnitInfo b2 = RosterService.AddToBench(p2, 2, 2, false);  // 2★ → 回流 2 份
            UnitInfo gift = RosterService.AddToBench(p2, 3, 1, true); // gift → 不回流

            int pool1Before = pool.Remaining[0]; // templateId=1
            int pool2Before = pool.Remaining[1]; // templateId=2
            int pool3Before = pool.Remaining[2]; // templateId=3

            room.EliminatePlayer(11002);

            // 验证回收
            if (pool.Remaining[0] != pool1Before + 1)
                throw new Exception("1★ unit should return 1 copy");
            if (pool.Remaining[1] != pool2Before + 2)
                throw new Exception("2★ unit should return 2 copies");
            if (pool.Remaining[2] != pool3Before)
                throw new Exception("Gift unit should NOT return to pool");

            // Roster 应该清空
            RosterComponent roster = p2.GetComponent<RosterComponent>();
            if (roster.Units.Count != 0)
                throw new Exception("Roster should be empty after elimination");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] Elimination recovery test passed: roster units returned to pool correctly");
        }

        /// <summary>
        /// 验证羁绊统计：tag 计数、level 计算、实时更新。
        /// </summary>
        public static void TestSynergyCounting(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 12001, 12002, 12003, 12004 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 12121);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(12001);
            SynergyComponent synergy = p1.GetComponent<SynergyComponent>();
            if (synergy == null) throw new Exception("SynergyComponent missing on player");

            // 添加单位到棋盘：迷你皮卡(Id=1, P.E.K.K.A/Brutalist) 和 皮卡超人(Id=10, P.E.K.K.A/Brawler)
            UnitInfo u1 = RosterService.AddToBench(p1, 1, 1, false);
            u1.Row = 0; u1.Col = 0;
            UnitInfo u2 = RosterService.AddToBench(p1, 10, 1, false);
            u2.Row = 1; u2.Col = 0;
            // 板凳：野蛮人(Id=4, Clan/Brawler)
            UnitInfo u3 = RosterService.AddToBench(p1, 4, 1, false);

            SynergyService.Recalculate(p1);

            // P.E.K.K.A: 2个棋盘 → count=2, thresholds=[2,4] → level=1
            SynergyEntry pekka = FindSynergyByTag(synergy.LiveSynergies, "P.E.K.K.A");
            if (pekka == null) throw new Exception("P.E.K.K.A synergy not found");
            if (pekka.Count != 2) throw new Exception($"P.E.K.K.A count expected 2, got {pekka.Count}");
            if (pekka.Level != 1) throw new Exception($"P.E.K.K.A level expected 1, got {pekka.Level}");

            // Brutalist: 1个棋盘 → count=1, thresholds=[2,4] → level=0
            SynergyEntry brutalist = FindSynergyByTag(synergy.LiveSynergies, "Brutalist");
            if (brutalist == null) throw new Exception("Brutalist synergy not found");
            if (brutalist.Count != 1) throw new Exception($"Brutalist count expected 1, got {brutalist.Count}");
            if (brutalist.Level != 0) throw new Exception($"Brutalist level expected 0, got {brutalist.Level}");

            // Brawler: 只有皮卡超人1个在棋盘（野蛮人在板凳不计） → count=1, level=0
            SynergyEntry brawler = FindSynergyByTag(synergy.LiveSynergies, "Brawler");
            if (brawler == null) throw new Exception("Brawler synergy not found");
            if (brawler.Count != 1) throw new Exception($"Brawler count expected 1, got {brawler.Count}");
            if (brawler.Level != 0) throw new Exception($"Brawler level expected 0, got {brawler.Level}");

            // 将野蛮人移到棋盘 → Brawler count=2, level=1
            u3.Row = 2; u3.Col = 0;
            SynergyService.Recalculate(p1);
            brawler = FindSynergyByTag(synergy.LiveSynergies, "Brawler");
            if (brawler.Count != 2) throw new Exception($"Brawler count expected 2 after move, got {brawler.Count}");
            if (brawler.Level != 1) throw new Exception($"Brawler level expected 1 after move, got {brawler.Level}");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] SynergyCounting test passed: tag counting, level calc, real-time update correct");
        }

        /// <summary>
        /// 验证快照生成：ActiveSynergies、UnitModifiers 静态效果。
        /// </summary>
        public static void TestSynergySnapshot(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 13001, 13002, 13003, 13004 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 13131);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(13001);

            // 棋盘放 2 个 Brawler：皮卡超人(Id=10, P.E.K.K.A/Brawler) 和 野蛮人(Id=4, Clan/Brawler)
            UnitInfo u1 = RosterService.AddToBench(p1, 10, 1, false);
            u1.Row = 0; u1.Col = 0;
            UnitInfo u2 = RosterService.AddToBench(p1, 4, 1, false);
            u2.Row = 1; u2.Col = 0;

            SynergyService.GenerateSnapshot(p1);

            SynergyComponent synergy = p1.GetComponent<SynergyComponent>();
            TraitSnapshot snapshot = synergy.Snapshot;
            if (snapshot == null) throw new Exception("Snapshot should not be null");

            // ActiveSynergies 应包含 Brawler level=1
            SynergyEntry activeBrawler = null;
            foreach (SynergyEntry e in snapshot.ActiveSynergies)
            {
                if (e.Tag == "Brawler") activeBrawler = e;
            }
            if (activeBrawler == null) throw new Exception("Brawler not in ActiveSynergies");
            if (activeBrawler.Level != 1) throw new Exception($"Brawler active level expected 1, got {activeBrawler.Level}");

            // UnitModifiers: Brawler level=1 → HpMultiplier = 1.0 * (1 + 0.5) = 1.5
            if (!snapshot.UnitModifiers.ContainsKey(u1.InstId))
                throw new Exception("u1 missing from UnitModifiers");
            UnitBattleModifiers mods1 = snapshot.UnitModifiers[u1.InstId];
            float expectedHp = 1.5f;
            if (Math.Abs(mods1.HpMultiplier - expectedHp) > 0.001f)
                throw new Exception($"u1 HpMultiplier expected {expectedHp}, got {mods1.HpMultiplier}");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] SynergySnapshot test passed: snapshot generation and static effects correct");
        }

        /// <summary>
        /// 验证 Goblin 经济羁绊：赠送单位到板凳。
        /// </summary>
        public static void TestGoblinGift(Scene scene)
        {
            AutoChessConfigLoader.Init();

            MatchComponent matchComp = scene.AddComponent<MatchComponent>();
            List<long> playerIds = new List<long> { 14001, 14002, 14003, 14004 };
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, playerIds, 14141);
            room.StartMatch();

            MatchPlayer p1 = room.FindPlayerById(14001);
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            SynergyComponent synergy = p1.GetComponent<SynergyComponent>();

            // 手动设置 LastGoblinLevel = 1（模拟上回合有 Goblin 激活）
            synergy.LastGoblinLevel = 1;

            int benchBefore = RosterService.GetBenchUsed(p1);
            int given = GoblinGiftService.TryGiveGifts(p1, rng, 2);

            // Goblin level=1 → Effects[0].Param1=1 → 赠送 1 个
            if (given != 1) throw new Exception($"GoblinGift: expected 1 gift, got {given}");
            if (RosterService.GetBenchUsed(p1) != benchBefore + 1)
                throw new Exception("GoblinGift: bench should have 1 more unit");

            // 验证赠送的单位是 isGift=true 且有 Goblin 标签
            RosterComponent roster = p1.GetComponent<RosterComponent>();
            UnitInfo giftUnit = roster.Units[roster.Units.Count - 1];
            if (!giftUnit.IsGift) throw new Exception("Gift unit should have IsGift=true");

            UnitTemplateDef giftDef = AutoChessConfigLoader.GetUnit(giftUnit.TemplateId);
            bool hasGoblinTag = false;
            foreach (string tag in giftDef.Tags)
            {
                if (tag == "Goblin") { hasGoblinTag = true; break; }
            }
            if (!hasGoblinTag) throw new Exception("Gift unit should have Goblin tag");

            // LastGoblinLevel=0 → 不赠送
            synergy.LastGoblinLevel = 0;
            int noGift = GoblinGiftService.TryGiveGifts(p1, rng, 3);
            if (noGift != 0) throw new Exception("GoblinGift: should give 0 when LastGoblinLevel=0");

            scene.RemoveComponent<MatchComponent>();
            Log.Info("[AutoChess] GoblinGift test passed: gift giving and validation correct");
        }

        private static SynergyEntry FindSynergyByTag(List<SynergyEntry> entries, string tag)
        {
            if (entries == null) return null;
            foreach (SynergyEntry e in entries)
            {
                if (e.Tag == tag) return e;
            }
            return null;
        }
        /// <summary>
        /// 验证 HexUtil 坐标转换和距离计算
        /// </summary>
        public static void TestHexUtil()
        {
            // 相邻格距离 = 1
            int dist1 = HexUtil.HexDistance(0, 0, 1, 0);
            if (dist1 != 1)
                throw new Exception($"HexDistance adjacent expected 1, got {dist1}");

            // 对角格距离 = 2: (0,0)->(1,2)
            int dist2 = HexUtil.HexDistance(0, 0, 1, 2);
            if (dist2 != 2)
                throw new Exception($"HexDistance diagonal expected 2, got {dist2}");

            // (0,0) -> (7,4) 距离
            // axial(0,0)=(0,0), axial(7,4)=(5,4), dist=(|5|+|4|+|9|)/2=9
            int dist3 = HexUtil.HexDistance(0, 0, 7, 4);
            if (dist3 != 9)
                throw new Exception($"HexDistance (0,0)-(7,4) expected 9, got {dist3}");

            // GetNeighbors(0,0) — 偶行，6 个邻居中应有合法的
            List<int> ncols = new List<int>();
            List<int> nrows = new List<int>();
            HexUtil.GetNeighbors(0, 0, ncols, nrows);
            if (ncols.Count != 6)
                throw new Exception($"GetNeighbors(0,0) expected 6 neighbors, got {ncols.Count}");

            // 过滤出界后，(0,0) 偶行邻居: (+1,0),(0,-1),(-1,-1),(-1,0),(-1,+1),(0,+1)
            // 合法的只有 (1,0) 和 (0,1)
            int inBoundsCount = 0;
            for (int i = 0; i < ncols.Count; i++)
            {
                if (HexUtil.IsInBounds(ncols[i], nrows[i]))
                    inBoundsCount++;
            }
            if (inBoundsCount != 2)
                throw new Exception($"GetNeighbors(0,0) in-bounds expected 2, got {inBoundsCount}");

            // IsInBounds 边界检查
            if (!HexUtil.IsInBounds(0, 0))
                throw new Exception("IsInBounds(0,0) should be true");
            if (!HexUtil.IsInBounds(7, 4))
                throw new Exception("IsInBounds(7,4) should be true");
            if (HexUtil.IsInBounds(8, 0))
                throw new Exception("IsInBounds(8,0) should be false");
            if (HexUtil.IsInBounds(-1, 0))
                throw new Exception("IsInBounds(-1,0) should be false");

            Log.Info("[AutoChess] TestHexUtil passed.");
        }

        /// <summary>
        /// 验证 ManaService 法力值累积/消耗/溢出保护
        /// </summary>
        public static void TestMana()
        {
            var unit = new CombatUnitState
            {
                Mana = 0,
                ManaGainOnAttack = AutoChessDefine.DefaultManaGainOnAttack, // 10
                ManaGainOnHit = AutoChessDefine.DefaultManaGainOnHit,       // 6
            };

            // GainOnAttack 10次 → mana=100, IsFull=true
            for (int i = 0; i < 10; i++)
            {
                ManaService.GainOnAttack(unit);
            }
            if (unit.Mana != AutoChessDefine.ManaMax)
                throw new Exception($"TestMana: after 10x GainOnAttack expected {AutoChessDefine.ManaMax}, got {unit.Mana}");
            if (!ManaService.IsFull(unit))
                throw new Exception("TestMana: IsFull should be true at ManaMax");

            // Consume → mana=0
            ManaService.Consume(unit);
            if (unit.Mana != 0)
                throw new Exception($"TestMana: after Consume expected 0, got {unit.Mana}");

            // GainOnHit 溢出保护: mana=95, +6 → clamped to 100
            unit.Mana = 95;
            ManaService.GainOnHit(unit);
            if (unit.Mana != AutoChessDefine.ManaMax)
                throw new Exception($"TestMana: after GainOnHit from 95 expected {AutoChessDefine.ManaMax}, got {unit.Mana}");

            Log.Info("[AutoChess] TestMana passed.");
        }

        /// <summary>
        /// 验证 TriggerChecker 各触发类型判定
        /// </summary>
        public static void TestTriggerChecker()
        {
            // === CombatStart ===
            var unit = new CombatUnitState { IsAlive = true, Trigger = new TriggerState() };
            var skill = new SkillDefData { TriggerType = SkillTriggerType.CombatStart };

            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.CombatStart, 0))
                throw new Exception("TriggerChecker: CombatStart should trigger at tick 0");
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.CombatStart, 1))
                throw new Exception("TriggerChecker: CombatStart should NOT trigger at tick > 0");

            // === OnHitCount ===
            unit.Trigger.Reset();
            skill = new SkillDefData { TriggerType = SkillTriggerType.OnHitCount, HitCountRequired = 3 };

            TriggerChecker.IncrementHitCount(unit);
            TriggerChecker.IncrementHitCount(unit);
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.OnHitCount, 10))
                throw new Exception("TriggerChecker: OnHitCount should NOT trigger at 2 hits (need 3)");

            TriggerChecker.IncrementHitCount(unit);
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.OnHitCount, 10))
                throw new Exception("TriggerChecker: OnHitCount should trigger at 3 hits");
            if (unit.Trigger.HitCount != 0)
                throw new Exception("TriggerChecker: OnHitCount should reset counter after trigger");

            // === OnHpBelow ===
            unit.Trigger.Reset();
            unit.MaxHp = 100;
            unit.Hp = 60;
            skill = new SkillDefData { TriggerType = SkillTriggerType.OnHpBelow, HpThresholdPct = 0.5f };

            if (TriggerChecker.Check(unit, skill, SkillTriggerType.OnHpBelow, 5))
                throw new Exception("TriggerChecker: OnHpBelow should NOT trigger when Hp=60 >= 50");

            unit.Hp = 40;
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.OnHpBelow, 5))
                throw new Exception("TriggerChecker: OnHpBelow should trigger when Hp=40 < 50");
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.OnHpBelow, 6))
                throw new Exception("TriggerChecker: OnHpBelow should NOT trigger twice");

            // === OnKill ===
            unit.Trigger.Reset();
            skill = new SkillDefData { TriggerType = SkillTriggerType.OnKill, ChainLimit = 2 };

            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.OnKill, 10))
                throw new Exception("TriggerChecker: OnKill should trigger (1st)");
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.OnKill, 20))
                throw new Exception("TriggerChecker: OnKill should trigger (2nd)");
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.OnKill, 30))
                throw new Exception("TriggerChecker: OnKill should NOT trigger after ChainLimit reached");

            // === Interval ===
            unit.Trigger.Reset();
            skill = new SkillDefData { TriggerType = SkillTriggerType.Interval, IntervalSeconds = 2.0f };
            // 2.0s * 20 ticks/s = 40 ticks required
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.Interval, 39))
                throw new Exception("TriggerChecker: Interval should NOT trigger at tick 39 (need 40)");
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.Interval, 40))
                throw new Exception("TriggerChecker: Interval should trigger at tick 40");
            if (unit.Trigger.LastIntervalTick != 40)
                throw new Exception("TriggerChecker: Interval should update LastIntervalTick to 40");

            // === Event mismatch ===
            skill = new SkillDefData { TriggerType = SkillTriggerType.AttackTrait };
            if (TriggerChecker.Check(unit, skill, SkillTriggerType.OnDeath, 0))
                throw new Exception("TriggerChecker: mismatched triggerEvent should return false");

            // === AttackTrait / ManaFull / OnDeath ===
            skill = new SkillDefData { TriggerType = SkillTriggerType.AttackTrait };
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.AttackTrait, 0))
                throw new Exception("TriggerChecker: AttackTrait should always trigger");
            skill = new SkillDefData { TriggerType = SkillTriggerType.ManaFull };
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.ManaFull, 0))
                throw new Exception("TriggerChecker: ManaFull should always trigger");
            skill = new SkillDefData { TriggerType = SkillTriggerType.OnDeath };
            if (!TriggerChecker.Check(unit, skill, SkillTriggerType.OnDeath, 0))
                throw new Exception("TriggerChecker: OnDeath should always trigger");

            Log.Info("[AutoChess] TestTriggerChecker passed.");
        }

        public static void TestTargetSelector()
        {
            // 构建单位
            var caster = new CombatUnitState
            {
                InstId = 1, Col = 0, Row = 0, Side = 0, IsAlive = true, Range = 3,
                Hp = 200, MaxHp = 200,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            // enemy1: 距离1 (col=1,row=0), hp=100
            var enemy1 = new CombatUnitState
            {
                InstId = 10, Col = 1, Row = 0, Side = 1, IsAlive = true,
                Hp = 100, MaxHp = 100,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            // enemy2: 距离3 (col=3,row=0), hp=50
            var enemy2 = new CombatUnitState
            {
                InstId = 11, Col = 3, Row = 0, Side = 1, IsAlive = true,
                Hp = 50, MaxHp = 100,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            // enemy3: 距离2 (col=2,row=0), hp=80
            var enemy3 = new CombatUnitState
            {
                InstId = 12, Col = 2, Row = 0, Side = 1, IsAlive = true,
                Hp = 80, MaxHp = 100,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            var allUnits = new List<CombatUnitState> { caster, enemy1, enemy2, enemy3 };

            // === NearestEnemy: 应返回 enemy1（距离最近=1） ===
            var skill = new SkillDefData { TargetType = SkillTargetType.NearestEnemy };
            var targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 10)
                throw new Exception($"TargetSelector: NearestEnemy expected InstId=10, got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === NearestEnemy tiebreaker: 相同距离按 InstId 升序 ===
            var enemySameDist = new CombatUnitState
            {
                InstId = 5, Col = 1, Row = 0, Side = 1, IsAlive = true,
                Hp = 100, MaxHp = 100,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var unitsWithTie = new List<CombatUnitState> { caster, enemy1, enemySameDist };
            targets = TargetSelector.Select(caster, skill, unitsWithTie);
            if (targets.Count != 1 || targets[0].InstId != 5)
                throw new Exception($"TargetSelector: NearestEnemy tiebreaker expected InstId=5, got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === 隐身单位被排除 ===
            var invisEnemy = new CombatUnitState
            {
                InstId = 2, Col = 0, Row = 1, Side = 1, IsAlive = true,
                Hp = 10, MaxHp = 100,
                Buffs = new List<ActiveBuff> { new ActiveBuff { Type = BuffType.Invisibility, RemainingTicks = 10 } },
                Trigger = new TriggerState()
            };
            var unitsWithInvis = new List<CombatUnitState> { caster, invisEnemy, enemy2 };
            targets = TargetSelector.Select(caster, skill, unitsWithInvis);
            // invisEnemy 被排除，应返回 enemy2
            if (targets.Count != 1 || targets[0].InstId != 11)
                throw new Exception($"TargetSelector: invisible enemy should be excluded, got InstId={( targets.Count > 0 ? targets[0].InstId : -1)}");

            // === Self: 返回自己 ===
            skill = new SkillDefData { TargetType = SkillTargetType.Self };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 1)
                throw new Exception($"TargetSelector: Self expected InstId=1, got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === LowestHp: 返回血量最低的（enemy2 hp=50） ===
            skill = new SkillDefData { TargetType = SkillTargetType.LowestHp };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 11)
                throw new Exception($"TargetSelector: LowestHp expected InstId=11 (hp=50), got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === FarthestInRadius: 半径2内最远的（enemy3 距离=2） ===
            skill = new SkillDefData
            {
                TargetType = SkillTargetType.FarthestInRadius,
                Effects = new SkillEffectData[] { new SkillEffectData { Value2 = 2f } }
            };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 12)
                throw new Exception($"TargetSelector: FarthestInRadius(2) expected InstId=12 (dist=2), got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === FarthestInRange: caster.Range=3 范围内最远的（enemy2 距离=3） ===
            skill = new SkillDefData { TargetType = SkillTargetType.FarthestInRange };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 11)
                throw new Exception($"TargetSelector: FarthestInRange(3) expected InstId=11 (dist=3), got {(targets.Count > 0 ? targets[0].InstId : -1)}");

            // === MultiTargets: N=2 时返回最近的 2 个 (enemy1 dist=1, enemy3 dist=2) ===
            skill = new SkillDefData
            {
                TargetType = SkillTargetType.MultiTargets,
                Effects = new SkillEffectData[] { new SkillEffectData { Value2 = 2f } }
            };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 2)
                throw new Exception($"TargetSelector: MultiTargets(2) expected 2, got {targets.Count}");
            if (targets[0].InstId != 10 || targets[1].InstId != 12)
                throw new Exception($"TargetSelector: MultiTargets(2) expected [10,12], got [{targets[0].InstId},{targets[1].InstId}]");

            // === ClusterLargest: 3 个敌人聚集 vs 1 个孤立 ===
            // clustered: (2,0),(3,0),(2,1) 互相距离<=1; isolated: (7,0)
            var ce1 = new CombatUnitState { InstId = 20, Col = 2, Row = 0, Side = 1, IsAlive = true, Hp = 100, MaxHp = 100, Buffs = new List<ActiveBuff>(), Trigger = new TriggerState() };
            var ce2 = new CombatUnitState { InstId = 21, Col = 3, Row = 0, Side = 1, IsAlive = true, Hp = 100, MaxHp = 100, Buffs = new List<ActiveBuff>(), Trigger = new TriggerState() };
            var ce3 = new CombatUnitState { InstId = 22, Col = 2, Row = 1, Side = 1, IsAlive = true, Hp = 100, MaxHp = 100, Buffs = new List<ActiveBuff>(), Trigger = new TriggerState() };
            var isolated = new CombatUnitState { InstId = 30, Col = 7, Row = 0, Side = 1, IsAlive = true, Hp = 100, MaxHp = 100, Buffs = new List<ActiveBuff>(), Trigger = new TriggerState() };
            var clusterUnits = new List<CombatUnitState> { caster, ce1, ce2, ce3, isolated };
            skill = new SkillDefData
            {
                TargetType = SkillTargetType.ClusterLargest,
                Effects = new SkillEffectData[] { new SkillEffectData { Value2 = 1f } }
            };
            targets = TargetSelector.Select(caster, skill, clusterUnits);
            // 聚集区域应该包含 3 个 (ce1,ce2,ce3)，不包含 isolated
            if (targets.Count != 3)
                throw new Exception($"TargetSelector: ClusterLargest expected 3, got {targets.Count}");
            bool hasIsolated = false;
            for (int i = 0; i < targets.Count; i++) { if (targets[i].InstId == 30) hasIsolated = true; }
            if (hasIsolated)
                throw new Exception("TargetSelector: ClusterLargest should not include isolated enemy");

            // === AreaRadius: radius=1 时返回 caster 相邻格的敌人 ===
            // caster at (0,0), enemy1 at (1,0) dist=1, enemy2 at (3,0) dist=3, enemy3 at (2,0) dist=2
            skill = new SkillDefData
            {
                TargetType = SkillTargetType.AreaRadius,
                Effects = new SkillEffectData[] { new SkillEffectData { Value2 = 1f } }
            };
            targets = TargetSelector.Select(caster, skill, allUnits);
            if (targets.Count != 1 || targets[0].InstId != 10)
                throw new Exception($"TargetSelector: AreaRadius(1) expected [10], got count={targets.Count}");

            Log.Info("[AutoChess] TestTargetSelector passed.");
        }

        public static void TestEffectApplier()
        {
            // === Damage: atk=100, Value1=1.5, DamageMultiplier=1.0 → damage=150, HP 500→350 ===
            var caster = new CombatUnitState
            {
                InstId = 1, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Atk = 100, Hp = 500, MaxHp = 500, DamageMultiplier = 1.0f,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            var target1 = new CombatUnitState
            {
                InstId = 10, Col = 1, Row = 0, Side = 1, IsAlive = true,
                Hp = 500, MaxHp = 500, DamageReduction = 0f,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };

            var damageEffect = new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 1.5f };
            var targets = new List<CombatUnitState> { target1 };
            var allUnits = new List<CombatUnitState> { caster, target1 };

            var results = EffectApplier.Apply(caster, targets, damageEffect, allUnits, 0);
            if (results.Count != 1)
                throw new Exception($"EffectApplier Damage: expected 1 result, got {results.Count}");
            if (results[0].Value != 150)
                throw new Exception($"EffectApplier Damage: expected 150 damage, got {results[0].Value}");
            if (target1.Hp != 350)
                throw new Exception($"EffectApplier Damage: expected HP=350, got {target1.Hp}");

            // === Damage with DamageReduction=0.4 → actual = floor(150*0.6) = 90 ===
            target1.Hp = 500;
            target1.DamageReduction = 0.4f;
            results = EffectApplier.Apply(caster, targets, damageEffect, allUnits, 0);
            if (results[0].Value != 90)
                throw new Exception($"EffectApplier Damage+Reduction: expected 90 damage, got {results[0].Value}");
            if (target1.Hp != 410)
                throw new Exception($"EffectApplier Damage+Reduction: expected HP=410, got {target1.Hp}");

            // === Damage: 致死 → IsAlive=false ===
            target1.Hp = 50;
            target1.DamageReduction = 0f;
            results = EffectApplier.Apply(caster, targets, damageEffect, allUnits, 0);
            if (target1.IsAlive)
                throw new Exception("EffectApplier Damage: target should be dead");
            if (target1.Hp != 0)
                throw new Exception($"EffectApplier Damage: HP should be 0, got {target1.Hp}");

            // === Stun: buff 添加成功 ===
            var stunTarget = new CombatUnitState
            {
                InstId = 20, Col = 2, Row = 0, Side = 1, IsAlive = true,
                Hp = 300, MaxHp = 300,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var stunEffect = new SkillEffectData { EffectType = SkillEffectType.Stun, Duration = 1.5f };
            var stunTargets = new List<CombatUnitState> { stunTarget };
            var stunAllUnits = new List<CombatUnitState> { caster, stunTarget };

            results = EffectApplier.Apply(caster, stunTargets, stunEffect, stunAllUnits, 0);
            int expectedStunTicks = (int)(1.5f * AutoChessDefine.CombatTickRate); // 30
            if (results.Count != 1 || results[0].Value != expectedStunTicks)
                throw new Exception($"EffectApplier Stun: expected ticks={expectedStunTicks}, got {(results.Count > 0 ? results[0].Value : -1)}");
            if (!stunTarget.IsStunned)
                throw new Exception("EffectApplier Stun: target should be stunned");

            // === Knockback: 推 2 格 ===
            // caster at (0,0), target at (1,0) — 推远离 caster 方向
            var kbCaster = new CombatUnitState
            {
                InstId = 30, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Hp = 500, MaxHp = 500,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var kbTarget = new CombatUnitState
            {
                InstId = 31, Col = 1, Row = 0, Side = 1, IsAlive = true,
                Hp = 300, MaxHp = 300,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var kbEffect = new SkillEffectData { EffectType = SkillEffectType.Knockback, Value1 = 2f };
            var kbTargets = new List<CombatUnitState> { kbTarget };
            var kbAllUnits = new List<CombatUnitState> { kbCaster, kbTarget };

            results = EffectApplier.Apply(kbCaster, kbTargets, kbEffect, kbAllUnits, 0);
            if (results.Count != 1)
                throw new Exception($"EffectApplier Knockback: expected 1 result, got {results.Count}");
            // 目标应该被推了 2 格，距离 caster 更远
            int kbDist = HexUtil.HexDistance(kbCaster.Col, kbCaster.Row, kbTarget.Col, kbTarget.Row);
            if (kbDist < 3)
                throw new Exception($"EffectApplier Knockback: expected distance>=3 after 2-step knockback, got {kbDist}");

            // === Knockback: 边界阻挡 — 在边界附近推 ===
            var kbEdgeTarget = new CombatUnitState
            {
                InstId = 32, Col = 7, Row = 0, Side = 1, IsAlive = true,
                Hp = 300, MaxHp = 300,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var kbEdgeCaster = new CombatUnitState
            {
                InstId = 33, Col = 6, Row = 0, Side = 0, IsAlive = true,
                Hp = 500, MaxHp = 500,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var kbEdgeTargets = new List<CombatUnitState> { kbEdgeTarget };
            var kbEdgeAllUnits = new List<CombatUnitState> { kbEdgeCaster, kbEdgeTarget };
            var kbEdgeEffect = new SkillEffectData { EffectType = SkillEffectType.Knockback, Value1 = 5f };

            results = EffectApplier.Apply(kbEdgeCaster, kbEdgeTargets, kbEdgeEffect, kbEdgeAllUnits, 0);
            // 目标应该停在棋盘内
            if (!HexUtil.IsInBounds(kbEdgeTarget.Col, kbEdgeTarget.Row))
                throw new Exception($"EffectApplier Knockback: target went out of bounds ({kbEdgeTarget.Col},{kbEdgeTarget.Row})");

            // === Invisibility ===
            var invisTarget = new CombatUnitState
            {
                InstId = 40, Col = 3, Row = 0, Side = 0, IsAlive = true,
                Hp = 200, MaxHp = 200,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var invisEffect = new SkillEffectData { EffectType = SkillEffectType.Invisibility, Duration = 2.0f };
            var invisTargets = new List<CombatUnitState> { invisTarget };
            var invisAllUnits = new List<CombatUnitState> { caster, invisTarget };

            results = EffectApplier.Apply(caster, invisTargets, invisEffect, invisAllUnits, 0);
            if (!invisTarget.IsInvisible)
                throw new Exception("EffectApplier Invisibility: target should be invisible");
            int expectedInvisTicks = (int)(2.0f * AutoChessDefine.CombatTickRate); // 40
            if (results.Count != 1 || results[0].Value != expectedInvisTicks)
                throw new Exception($"EffectApplier Invisibility: expected ticks={expectedInvisTicks}, got {(results.Count > 0 ? results[0].Value : -1)}");

            // === SpeedBuff ===
            var speedTarget = new CombatUnitState
            {
                InstId = 50, Col = 4, Row = 0, Side = 0, IsAlive = true,
                Hp = 200, MaxHp = 200,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var speedEffect = new SkillEffectData { EffectType = SkillEffectType.SpeedBuff, Value1 = 1.5f, Duration = 3.0f };
            var speedTargets = new List<CombatUnitState> { speedTarget };
            var speedAllUnits = new List<CombatUnitState> { caster, speedTarget };

            results = EffectApplier.Apply(caster, speedTargets, speedEffect, speedAllUnits, 0);
            int expectedSpeedTicks = (int)(3.0f * AutoChessDefine.CombatTickRate); // 60
            if (results.Count != 1 || results[0].Value != expectedSpeedTicks)
                throw new Exception($"EffectApplier SpeedBuff: expected ticks={expectedSpeedTicks}, got {(results.Count > 0 ? results[0].Value : -1)}");
            if (speedTarget.Buffs.Count != 1 || speedTarget.Buffs[0].Type != BuffType.SpeedBuff)
                throw new Exception("EffectApplier SpeedBuff: buff not added correctly");
            if (Math.Abs(speedTarget.Buffs[0].Value1 - 1.5f) > 0.001f)
                throw new Exception($"EffectApplier SpeedBuff: expected Value1=1.5, got {speedTarget.Buffs[0].Value1}");

            // === Summon: 召唤 1 个单位 ===
            var sumCaster = new CombatUnitState
            {
                InstId = 60, TemplateId = 5, Star = 2, Col = 3, Row = 3, Side = 0, IsAlive = true,
                Atk = 200, Hp = 1000, MaxHp = 1000, AtkSpeed = 1.0f, Range = 1, MoveSpeed = 1.0f,
                DamageMultiplier = 1.0f,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var sumAllUnits = new List<CombatUnitState> { sumCaster };
            int beforeCount = sumAllUnits.Count;
            var summonEffect = new SkillEffectData { EffectType = SkillEffectType.Summon, Value1 = 1f };
            results = EffectApplier.Apply(sumCaster, new List<CombatUnitState>(), summonEffect, sumAllUnits, 0);
            if (sumAllUnits.Count != beforeCount + 1)
                throw new Exception($"EffectApplier Summon: expected {beforeCount + 1} units, got {sumAllUnits.Count}");
            var summoned = sumAllUnits[sumAllUnits.Count - 1];
            if (summoned.IsEffectiveForDamageCount)
                throw new Exception("EffectApplier Summon: summoned unit should have IsEffectiveForDamageCount=false");
            if (summoned.MaxHp != (int)(sumCaster.MaxHp * 0.3f))
                throw new Exception($"EffectApplier Summon: expected MaxHp={sumCaster.MaxHp * 0.3f}, got {summoned.MaxHp}");
            if (summoned.Side != sumCaster.Side)
                throw new Exception("EffectApplier Summon: summoned unit should be on caster's side");

            // === Clone: 2 个克隆体, Value2=0.5 ===
            var cloneCaster = new CombatUnitState
            {
                InstId = 70, TemplateId = 6, Star = 2, Col = 3, Row = 3, Side = 0, IsAlive = true,
                Atk = 150, Hp = 800, MaxHp = 800, AtkSpeed = 1.0f, Range = 1, MoveSpeed = 1.0f,
                DamageMultiplier = 1.0f,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            var cloneAllUnits = new List<CombatUnitState> { cloneCaster };
            int casterHpBefore = cloneCaster.Hp;
            var cloneEffect = new SkillEffectData { EffectType = SkillEffectType.Clone, Value1 = 2f, Value2 = 0.5f };
            results = EffectApplier.Apply(cloneCaster, new List<CombatUnitState>(), cloneEffect, cloneAllUnits, 0);
            if (cloneAllUnits.Count != 3)
                throw new Exception($"EffectApplier Clone: expected 3 units, got {cloneAllUnits.Count}");
            int expectedCloneHp = (int)(casterHpBefore * 0.5f);
            if (cloneAllUnits[1].Hp != expectedCloneHp)
                throw new Exception($"EffectApplier Clone: expected clone HP={expectedCloneHp}, got {cloneAllUnits[1].Hp}");
            // caster HP = 800 * (1 - 0.5*2) = 0
            int expectedCasterHp = (int)(casterHpBefore * (1f - 0.5f * 2));
            if (cloneCaster.Hp != expectedCasterHp)
                throw new Exception($"EffectApplier Clone: expected caster HP={expectedCasterHp}, got {cloneCaster.Hp}");
            if (cloneAllUnits[1].IsEffectiveForDamageCount)
                throw new Exception("EffectApplier Clone: clone should have IsEffectiveForDamageCount=false");

            // === HealOverTime: 添加后 tick 20 次，HP 恢复 ===
            var hotTarget = new CombatUnitState
            {
                InstId = 80, Col = 2, Row = 2, Side = 0, IsAlive = true,
                Hp = 500, MaxHp = 1000,
                Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
            };
            // Value1=0.05 表示每秒恢复 5% MaxHp，Duration=2.0s → 40 ticks
            var hotEffect = new SkillEffectData { EffectType = SkillEffectType.HealOverTime, Value1 = 0.05f, Duration = 2.0f };
            var hotTargets = new List<CombatUnitState> { hotTarget };
            var hotAllUnits = new List<CombatUnitState> { hotTarget };
            results = EffectApplier.Apply(caster, hotTargets, hotEffect, hotAllUnits, 0);
            if (hotTarget.Buffs.Count != 1 || hotTarget.Buffs[0].Type != BuffType.HealOverTime)
                throw new Exception("EffectApplier HealOverTime: buff not added correctly");
            // tick 20 次（1秒），每 tick 恢复 floor(1000 * 0.05 / 20) = 2 HP
            int hpBefore = hotTarget.Hp;
            for (int ti = 0; ti < 20; ti++)
            {
                EffectApplier.TickBuff(hotTarget);
            }
            int expectedHeal = (int)(hotTarget.MaxHp * 0.05f / AutoChessDefine.CombatTickRate) * 20;
            if (hotTarget.Hp != hpBefore + expectedHeal)
                throw new Exception($"EffectApplier HealOverTime: expected HP={hpBefore + expectedHeal} after 20 ticks, got {hotTarget.Hp}");

            // === TickBuff: Stun buff tick 到 0 后消失 ===
            var tickUnit = new CombatUnitState
            {
                InstId = 90, Col = 1, Row = 1, Side = 0, IsAlive = true,
                Hp = 300, MaxHp = 300,
                Buffs = new List<ActiveBuff>
                {
                    new ActiveBuff { Type = BuffType.Stun, RemainingTicks = 3 }
                },
                Trigger = new TriggerState()
            };
            if (!tickUnit.IsStunned)
                throw new Exception("EffectApplier TickBuff: unit should be stunned initially");
            EffectApplier.TickBuff(tickUnit);
            EffectApplier.TickBuff(tickUnit);
            EffectApplier.TickBuff(tickUnit);
            if (tickUnit.IsStunned)
                throw new Exception("EffectApplier TickBuff: stun should be removed after 3 ticks");
            if (tickUnit.Buffs.Count != 0)
                throw new Exception($"EffectApplier TickBuff: expected 0 buffs, got {tickUnit.Buffs.Count}");

            Log.Info("[AutoChess] TestEffectApplier passed.");
        }

        /// <summary>
        /// SkillExecutor 管线集成测试
        /// </summary>
        public static void TestSkillExecutor()
        {
            // === Test 1: CombatStart 技能完整管线 ===
            // Skill 4 = 冲锋, CombatStart, FarthestInRange, Damage(2.0)+Stun(1.0s)
            var caster1 = new CombatUnitState
            {
                InstId = 1, SkillDefId = 4, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Atk = 100, Hp = 500, MaxHp = 500, Range = 3,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                FacingCol = 2, FacingRow = 0,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var enemy1 = new CombatUnitState
            {
                InstId = 10, Col = 2, Row = 0, Side = 1, IsAlive = true,
                Hp = 500, MaxHp = 500, DamageReduction = 0f, Range = 1,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var allUnits1 = new System.Collections.Generic.List<CombatUnitState> { caster1, enemy1 };

            var result1 = SkillExecutor.TryExecute(caster1, SkillTriggerType.CombatStart, allUnits1, null, 0);
            if (result1.Status != SkillExecutionStatus.Executed)
                throw new System.Exception($"SkillExecutor CombatStart: expected Executed, got {result1.Status}");
            if (result1.EffectResults.Count < 1)
                throw new System.Exception($"SkillExecutor CombatStart: expected at least 1 effect result, got {result1.EffectResults.Count}");
            // Damage: floor(100 * 2.0 * 1.0) = 200, enemy HP: 500 - 200 = 300
            if (enemy1.Hp != 300)
                throw new System.Exception($"SkillExecutor CombatStart: expected enemy HP=300, got {enemy1.Hp}");
            // Stun buff should be applied
            if (!enemy1.IsStunned)
                throw new System.Exception("SkillExecutor CombatStart: enemy should be stunned");

            // === Test 2: Interval 技能在 tick=0 不触发 ===
            // Skill 6 = 电场, Interval 3s, caster.Trigger.LastIntervalTick=0
            var caster2 = new CombatUnitState
            {
                InstId = 2, SkillDefId = 6, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Atk = 50, Hp = 300, MaxHp = 300, Range = 2,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var enemy2 = new CombatUnitState
            {
                InstId = 20, Col = 1, Row = 0, Side = 1, IsAlive = true,
                Hp = 200, MaxHp = 200, DamageReduction = 0f, Range = 1,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var allUnits2 = new System.Collections.Generic.List<CombatUnitState> { caster2, enemy2 };

            // tick=0, Interval requires 3s * TickRate ticks to pass, so tick=0 - lastTick(0) = 0 < required
            // But! Interval check: (0 - 0) >= requiredTicks. RequiredTicks = 3*5 = 15. 0 >= 15 = false.
            var result2 = SkillExecutor.TryExecute(caster2, SkillTriggerType.Interval, allUnits2, null, 0);
            if (result2.Status != SkillExecutionStatus.NotTriggered)
                throw new System.Exception($"SkillExecutor Interval tick=0: expected NotTriggered, got {result2.Status}");

            // === Test 3: 所有敌人死亡 → NoTargets ===
            var caster3 = new CombatUnitState
            {
                InstId = 3, SkillDefId = 4, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Atk = 100, Hp = 500, MaxHp = 500, Range = 3,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                FacingCol = 2, FacingRow = 0,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var deadEnemy = new CombatUnitState
            {
                InstId = 30, Col = 2, Row = 0, Side = 1, IsAlive = false,
                Hp = 0, MaxHp = 500, Range = 1,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var allUnits3 = new System.Collections.Generic.List<CombatUnitState> { caster3, deadEnemy };

            var result3 = SkillExecutor.TryExecute(caster3, SkillTriggerType.CombatStart, allUnits3, null, 0);
            if (result3.Status != SkillExecutionStatus.NoTargets)
                throw new System.Exception($"SkillExecutor NoTargets: expected NoTargets, got {result3.Status}");

            // === Test 4: SkillDefId=0 → NotTriggered ===
            var casterNoSkill = new CombatUnitState
            {
                InstId = 4, SkillDefId = 0, Col = 0, Row = 0, Side = 0, IsAlive = true,
                Atk = 50, Hp = 300, MaxHp = 300, Range = 1,
                DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                Buffs = new System.Collections.Generic.List<ActiveBuff>(),
                Trigger = new TriggerState()
            };

            var result4 = SkillExecutor.TryExecute(casterNoSkill, SkillTriggerType.CombatStart,
                new System.Collections.Generic.List<CombatUnitState> { casterNoSkill }, null, 0);
            if (result4.Status != SkillExecutionStatus.NotTriggered)
                throw new System.Exception($"SkillExecutor NoSkill: expected NotTriggered, got {result4.Status}");

            Log.Info("[AutoChess] TestSkillExecutor passed.");
        }

        /// <summary>
        /// 集成测试：完整技能管线端到端验证。
        /// 覆盖 6 个典型技能场景 + Buff 生命周期。
        /// </summary>
        public static void TestSkillSystem()
        {
            AutoChessConfigLoader.Init();

            // === 场景 1: 近战爆发 (SkillId=1, AttackTrait → NearestEnemy → Damage) ===
            {
                var caster = new CombatUnitState
                {
                    InstId = 1, TemplateId = 1, Star = 1, Hp = 500, MaxHp = 500,
                    Atk = 100, AtkSpeed = 1.0f, Range = 1, MoveSpeed = 1.0f,
                    CritChance = 0f, Mana = 0, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                    Col = 3, Row = 3, FacingCol = 3, FacingRow = 2,
                    IsAlive = true, Side = 0, SkillDefId = 1,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                // 近敌在相邻格
                var nearEnemy = new CombatUnitState
                {
                    InstId = 10, Col = 3, Row = 2, Side = 1, IsAlive = true,
                    Hp = 400, MaxHp = 400, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                // 远敌不应被选中
                var farEnemy = new CombatUnitState
                {
                    InstId = 11, Col = 6, Row = 6, Side = 1, IsAlive = true,
                    Hp = 400, MaxHp = 400, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var allUnits = new List<CombatUnitState> { caster, nearEnemy, farEnemy };

                var result = SkillExecutor.TryExecute(caster, SkillTriggerType.AttackTrait, allUnits, null, 5);
                if (result.Status != SkillExecutionStatus.Executed)
                    throw new Exception($"SkillSystem Skill1: expected Executed, got {result.Status}");

                // Skill1: Damage Value1=1.5, damage = floor(100 * 1.5 * 1.0) = 150
                // nearEnemy HP: 400 - 150 = 250
                if (nearEnemy.Hp != 250)
                    throw new Exception($"SkillSystem Skill1: expected nearEnemy HP=250, got {nearEnemy.Hp}");
                // farEnemy 不受影响
                if (farEnemy.Hp != 400)
                    throw new Exception($"SkillSystem Skill1: farEnemy should be untouched, HP={farEnemy.Hp}");
            }

            // === 场景 2: 王子冲锋 (SkillId=4, CombatStart → FarthestInRange → Damage+Stun) ===
            {
                var caster = new CombatUnitState
                {
                    InstId = 2, TemplateId = 4, Star = 1, Hp = 800, MaxHp = 800,
                    Atk = 200, AtkSpeed = 1.5f, Range = 3, MoveSpeed = 1.0f,
                    CritChance = 0.15f, Mana = 0, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                    Col = 0, Row = 0, FacingCol = 3, FacingRow = 0,
                    IsAlive = true, Side = 0, SkillDefId = 4,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                // 近敌 (距离1)
                var closeEnemy = new CombatUnitState
                {
                    InstId = 20, Col = 1, Row = 0, Side = 1, IsAlive = true,
                    Hp = 600, MaxHp = 600, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                // 远敌 (距离3, 在 Range 内, 应被 FarthestInRange 选中)
                var farEnemy = new CombatUnitState
                {
                    InstId = 21, Col = 3, Row = 0, Side = 1, IsAlive = true,
                    Hp = 600, MaxHp = 600, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var allUnits = new List<CombatUnitState> { caster, closeEnemy, farEnemy };

                var result = SkillExecutor.TryExecute(caster, SkillTriggerType.CombatStart, allUnits, null, 0);
                if (result.Status != SkillExecutionStatus.Executed)
                    throw new Exception($"SkillSystem Skill4: expected Executed, got {result.Status}");

                // Damage: floor(200 * 2.0 * 1.0) = 400, farEnemy HP: 600 - 400 = 200
                if (farEnemy.Hp != 200)
                    throw new Exception($"SkillSystem Skill4: expected farEnemy HP=200, got {farEnemy.Hp}");
                // closeEnemy 不受影响 (FarthestInRange 只选最远1个)
                if (closeEnemy.Hp != 600)
                    throw new Exception($"SkillSystem Skill4: closeEnemy should be untouched, HP={closeEnemy.Hp}");
                // Stun buff
                if (!farEnemy.IsStunned)
                    throw new Exception("SkillSystem Skill4: farEnemy should be stunned");
                // Stun Duration=1.0s → ticks = 1.0 * 20 = 20
                if (farEnemy.Buffs[0].RemainingTicks != 20)
                    throw new Exception($"SkillSystem Skill4: expected stun 20 ticks, got {farEnemy.Buffs[0].RemainingTicks}");
            }

            // === 场景 3: 女巫召唤 (SkillId=9, Interval → Self → Summon, 验证 allUnits 增长) ===
            {
                var caster = new CombatUnitState
                {
                    InstId = 3, TemplateId = 9, Star = 1, Hp = 400, MaxHp = 400,
                    Atk = 80, AtkSpeed = 1.0f, Range = 2, MoveSpeed = 1.0f,
                    CritChance = 0f, Mana = 0, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                    Col = 3, Row = 3, FacingCol = 3, FacingRow = 2,
                    IsAlive = true, Side = 0, SkillDefId = 9,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var enemy = new CombatUnitState
                {
                    InstId = 30, Col = 5, Row = 5, Side = 1, IsAlive = true,
                    Hp = 300, MaxHp = 300, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var allUnits = new List<CombatUnitState> { caster, enemy };

                // Skill9: Interval=5.0s → requiredTicks = 5.0 * 20 = 100
                // tick=0, LastIntervalTick=0, (0-0)=0 >= 100? No → NotTriggered...
                // Wait: (0-0)=0 >= 100 → false. Need tick >= 100.
                var result0 = SkillExecutor.TryExecute(caster, SkillTriggerType.Interval, allUnits, null, 0);
                if (result0.Status != SkillExecutionStatus.NotTriggered)
                    throw new Exception($"SkillSystem Skill9 tick=0: expected NotTriggered, got {result0.Status}");

                // tick=100: (100-0)=100 >= 100 → true, triggers
                int countBefore = allUnits.Count;
                var result1 = SkillExecutor.TryExecute(caster, SkillTriggerType.Interval, allUnits, null, 100);
                if (result1.Status != SkillExecutionStatus.Executed)
                    throw new Exception($"SkillSystem Skill9 tick=100: expected Executed, got {result1.Status}");
                // Summon Value1=1 → 1 unit added
                if (allUnits.Count != countBefore + 1)
                    throw new Exception($"SkillSystem Skill9: expected allUnits +1, got {allUnits.Count - countBefore}");
                // Summoned unit should be on same side
                var summoned = allUnits[allUnits.Count - 1];
                if (summoned.Side != caster.Side)
                    throw new Exception($"SkillSystem Skill9: summoned unit side mismatch");
                if (!summoned.IsAlive)
                    throw new Exception("SkillSystem Skill9: summoned unit should be alive");
            }

            // === 场景 4: 骷髅龙克隆 (SkillId=11, OnDeath → Self → Clone) ===
            {
                var caster = new CombatUnitState
                {
                    InstId = 4, TemplateId = 11, Star = 2, Hp = 200, MaxHp = 600,
                    Atk = 120, AtkSpeed = 1.0f, Range = 1, MoveSpeed = 1.0f,
                    CritChance = 0f, Mana = 0, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                    Col = 3, Row = 3, FacingCol = 3, FacingRow = 2,
                    IsAlive = true, Side = 0, SkillDefId = 11,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var enemy = new CombatUnitState
                {
                    InstId = 40, Col = 6, Row = 6, Side = 1, IsAlive = true,
                    Hp = 500, MaxHp = 500, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var allUnits = new List<CombatUnitState> { caster, enemy };
                int countBefore = allUnits.Count;

                var result = SkillExecutor.TryExecute(caster, SkillTriggerType.OnDeath, allUnits, null, 50);
                if (result.Status != SkillExecutionStatus.Executed)
                    throw new Exception($"SkillSystem Skill11: expected Executed, got {result.Status}");

                // Clone Value1=2, Value2=0.5 → 2 clones, each HP = floor(200 * 0.5) = 100
                if (allUnits.Count != countBefore + 2)
                    throw new Exception($"SkillSystem Skill11: expected allUnits +2, got {allUnits.Count - countBefore}");

                var clone1 = allUnits[countBefore];
                var clone2 = allUnits[countBefore + 1];
                if (clone1.Hp != 100)
                    throw new Exception($"SkillSystem Skill11: clone1 HP expected 100, got {clone1.Hp}");
                if (clone2.Hp != 100)
                    throw new Exception($"SkillSystem Skill11: clone2 HP expected 100, got {clone2.Hp}");
                if (clone1.Side != caster.Side || clone2.Side != caster.Side)
                    throw new Exception("SkillSystem Skill11: clones should be same side as caster");
                if (clone1.Atk != caster.Atk)
                    throw new Exception($"SkillSystem Skill11: clone Atk should match caster, got {clone1.Atk}");
            }

            // === 场景 5: 法力满触发 (SkillId=12, ManaFull → ClusterLargest → Projectile) ===
            {
                var caster = new CombatUnitState
                {
                    InstId = 5, TemplateId = 12, Star = 1, Hp = 500, MaxHp = 500,
                    Atk = 150, AtkSpeed = 1.0f, Range = 4, MoveSpeed = 1.0f,
                    CritChance = 0f, Mana = 100, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                    Col = 0, Row = 0, FacingCol = 3, FacingRow = 3,
                    IsAlive = true, Side = 0, SkillDefId = 12,
                    DamageMultiplier = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                // 3 个敌人聚集在一起 (Projectile Value2=3 选最远3个)
                var e1 = new CombatUnitState
                {
                    InstId = 50, Col = 4, Row = 4, Side = 1, IsAlive = true,
                    Hp = 500, MaxHp = 500, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };
                var e2 = new CombatUnitState
                {
                    InstId = 51, Col = 5, Row = 4, Side = 1, IsAlive = true,
                    Hp = 500, MaxHp = 500, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };
                var e3 = new CombatUnitState
                {
                    InstId = 52, Col = 4, Row = 5, Side = 1, IsAlive = true,
                    Hp = 500, MaxHp = 500, Range = 1, DamageReduction = 0f,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>(), Trigger = new TriggerState()
                };

                var allUnits = new List<CombatUnitState> { caster, e1, e2, e3 };

                var result = SkillExecutor.TryExecute(caster, SkillTriggerType.ManaFull, allUnits, null, 10);
                if (result.Status != SkillExecutionStatus.Executed)
                    throw new Exception($"SkillSystem Skill12: expected Executed, got {result.Status}");

                // Projectile: Value1=2.0, damage = floor(150 * 2.0 * 1.0) = 300 per target
                // Value2=3 → hits up to 3 targets
                int hitCount = 0;
                for (int i = 0; i < result.EffectResults.Count; i++)
                {
                    if (result.EffectResults[i].EffectType == SkillEffectType.Projectile)
                        hitCount++;
                }
                if (hitCount < 1)
                    throw new Exception($"SkillSystem Skill12: expected at least 1 projectile hit, got {hitCount}");

                // Mana should be consumed after ManaFull
                if (caster.Mana != 0)
                    throw new Exception($"SkillSystem Skill12: expected Mana=0 after ManaFull, got {caster.Mana}");
            }

            // === 场景 6: Buff 生命周期 (Stun 持续 N ticks 后消失) ===
            {
                var unit = new CombatUnitState
                {
                    InstId = 6, Col = 2, Row = 2, Side = 0, IsAlive = true,
                    Hp = 300, MaxHp = 300, Range = 1,
                    DamageMultiplier = 1.0f, AtkSpeed = 1.0f, MoveSpeed = 1.0f,
                    Buffs = new List<ActiveBuff>
                    {
                        new ActiveBuff { Type = BuffType.Stun, RemainingTicks = 3 }
                    },
                    Trigger = new TriggerState()
                };

                // 初始状态: stunned
                if (!unit.IsStunned)
                    throw new Exception("SkillSystem Buff: unit should start stunned");

                // Tick 1: RemainingTicks 3→2
                SkillExecutor.TickBuffs(unit);
                if (!unit.IsStunned)
                    throw new Exception("SkillSystem Buff: unit should still be stunned after tick 1");
                if (unit.Buffs[0].RemainingTicks != 2)
                    throw new Exception($"SkillSystem Buff: expected 2 ticks after tick 1, got {unit.Buffs[0].RemainingTicks}");

                // Tick 2: RemainingTicks 2→1
                SkillExecutor.TickBuffs(unit);
                if (!unit.IsStunned)
                    throw new Exception("SkillSystem Buff: unit should still be stunned after tick 2");

                // Tick 3: RemainingTicks 1→0 → removed
                SkillExecutor.TickBuffs(unit);
                if (unit.IsStunned)
                    throw new Exception("SkillSystem Buff: stun should have expired after tick 3");
                if (unit.Buffs.Count != 0)
                    throw new Exception($"SkillSystem Buff: expected 0 buffs after expiry, got {unit.Buffs.Count}");
            }

            Log.Info("[AutoChess] TestSkillSystem passed.");
        }

        // ===== BATTLE SYSTEM TESTS =====

        /// <summary>
        /// 验证 CombatSimulator 基本战斗循环：2v2 确定性结果。
        /// </summary>
        public static void TestCombatSimulator()
        {
            AutoChessConfigLoader.Init();

            // 2v2: L has stronger units, should win
            var leftUnits = new List<CombatUnitState>
            {
                MakeUnit(1, 0, col: 0, row: 3, atk: 200, hp: 800, range: 1, atkSpeed: 1.0f),
                MakeUnit(2, 0, col: 1, row: 3, atk: 150, hp: 600, range: 1, atkSpeed: 1.0f),
            };
            var rightUnits = new List<CombatUnitState>
            {
                MakeUnit(10, 1, col: 0, row: 1, atk: 80, hp: 300, range: 1, atkSpeed: 1.0f),
                MakeUnit(11, 1, col: 1, row: 1, atk: 60, hp: 250, range: 1, atkSpeed: 1.0f),
            };

            CombatResult result = CombatSimulator.RunCombat(leftUnits, rightUnits, null, null, null, 1);

            if (result.Winner != CombatWinner.Left)
                throw new Exception($"CombatSimulator 2v2: expected Left wins, got {result.Winner}");
            if (result.Events.Count < 4)
                throw new Exception($"CombatSimulator 2v2: expected >= 4 events, got {result.Events.Count}");
            if (result.LeftAliveEffective < 1)
                throw new Exception("CombatSimulator 2v2: expected at least 1 L alive");

            // Verify determinism: same inputs → same result
            var leftUnits2 = new List<CombatUnitState>
            {
                MakeUnit(1, 0, col: 0, row: 3, atk: 200, hp: 800, range: 1, atkSpeed: 1.0f),
                MakeUnit(2, 0, col: 1, row: 3, atk: 150, hp: 600, range: 1, atkSpeed: 1.0f),
            };
            var rightUnits2 = new List<CombatUnitState>
            {
                MakeUnit(10, 1, col: 0, row: 1, atk: 80, hp: 300, range: 1, atkSpeed: 1.0f),
                MakeUnit(11, 1, col: 1, row: 1, atk: 60, hp: 250, range: 1, atkSpeed: 1.0f),
            };

            CombatResult result2 = CombatSimulator.RunCombat(leftUnits2, rightUnits2, null, null, null, 1);

            if (result2.Winner != result.Winner)
                throw new Exception("CombatSimulator determinism: winner mismatch");
            if (result2.Events.Count != result.Events.Count)
                throw new Exception("CombatSimulator determinism: event count mismatch");

            Log.Info("[AutoChess] TestCombatSimulator passed.");
        }

        /// <summary>
        /// 验证 PairingService：4人配对、3人幽灵配对。
        /// </summary>
        public static void TestPairing(Scene scene)
        {
            // 4 players: should get 2 pairings
            var room = CreateTestRoom(4, scene);
            var rng = room.GetComponent<DeterministicRngComponent>();

            var results4 = PairingService.MakePairings(room, rng);
            if (results4.Count != 2)
                throw new Exception($"Pairing 4p: expected 2 pairs, got {results4.Count}");

            // All 4 IDs should appear exactly once
            var ids = new HashSet<long>();
            foreach (var p in results4)
            {
                ids.Add(p.LeftPlayerId);
                ids.Add(p.RightPlayerId);
            }
            if (ids.Count != 4)
                throw new Exception($"Pairing 4p: expected 4 unique IDs, got {ids.Count}");

            // L < R in each pair
            foreach (var p in results4)
            {
                if (p.LeftPlayerId >= p.RightPlayerId)
                    throw new Exception($"Pairing: L({p.LeftPlayerId}) should be < R({p.RightPlayerId})");
            }

            // 3 players with ghost: should get 1 real + 1 ghost match
            // Eliminate first alive player
            foreach (MatchPlayer mp in room.GetAlivePlayers())
            {
                room.EliminatePlayer(mp.PlayerId);
                break;
            }
            var lastGhost = new GhostSnapshot { PlayerId = 9999 };
            room.LastGhost = lastGhost;
            room.CurrentRound = 2;

            var results3 = PairingService.MakePairings(room, rng);
            if (results3.Count != 2)
                throw new Exception($"Pairing 3p: expected 2 pairs, got {results3.Count}");

            bool hasGhostMatch = false;
            foreach (var p in results3)
            {
                if (p.IsGhostMatch) hasGhostMatch = true;
            }
            if (!hasGhostMatch)
                throw new Exception("Pairing 3p: expected a ghost match");

            // 2 players: should get 1 pairing
            foreach (MatchPlayer mp in room.GetAlivePlayers())
            {
                room.EliminatePlayer(mp.PlayerId);
                break;
            }
            room.LastGhost = null;
            room.CurrentRound = 3;

            var results2 = PairingService.MakePairings(room, rng);
            if (results2.Count != 1)
                throw new Exception($"Pairing 2p: expected 1 pair, got {results2.Count}");

            // Cleanup
            scene.RemoveComponent<MatchComponent>();

            Log.Info("[AutoChess] TestPairing passed.");
        }

        /// <summary>
        /// 验证 SettlementService：胜方扣血、平局各扣1、淘汰排序。
        /// </summary>
        public static void TestSettlement(Scene scene)
        {
            var room = CreateTestRoom(4, scene);

            // All players start with 10 HP
            foreach (var p in room.GetAlivePlayers())
            {
                p.Hp = 10;
            }

            var playerIds = new List<long>();
            foreach (MatchPlayer mp in room.GetAlivePlayers())
            {
                playerIds.Add(mp.PlayerId);
            }
            long p0 = playerIds[0];
            long p1 = playerIds[1];
            long p2 = playerIds[2];
            long p3 = playerIds[3];

            // Match 1: L=p0 wins with 2 alive effective, R=p1 loses → p1 takes 2+1=3 damage
            // Match 2: Draw between p2 and p3 → each takes 1 damage
            var battleResults = new List<(PairingResult, CombatResult)>
            {
                (new PairingResult { LeftPlayerId = p0, RightPlayerId = p1 },
                 new CombatResult { Winner = CombatWinner.Left, LeftAliveEffective = 2, RightAliveEffective = 0 }),
                (new PairingResult { LeftPlayerId = p2, RightPlayerId = p3 },
                 new CombatResult { Winner = CombatWinner.Draw, LeftAliveEffective = 1, RightAliveEffective = 1 }),
            };

            SettlementService.ProcessResults(room, battleResults);

            MatchPlayer mp0 = room.FindPlayerById(p0);
            MatchPlayer mp1 = room.FindPlayerById(p1);
            MatchPlayer mp2 = room.FindPlayerById(p2);
            MatchPlayer mp3 = room.FindPlayerById(p3);

            if (mp0.Hp != 10)
                throw new Exception($"Settlement: p0 HP expected 10, got {mp0.Hp}");
            if (mp1.Hp != 7) // 10 - 3
                throw new Exception($"Settlement: p1 HP expected 7, got {mp1.Hp}");
            if (mp2.Hp != 9) // 10 - 1
                throw new Exception($"Settlement: p2 HP expected 9, got {mp2.Hp}");
            if (mp3.Hp != 9) // 10 - 1
                throw new Exception($"Settlement: p3 HP expected 9, got {mp3.Hp}");

            // Test elimination: deal lethal damage to p1
            var lethalResults = new List<(PairingResult, CombatResult)>
            {
                (new PairingResult { LeftPlayerId = p0, RightPlayerId = p1 },
                 new CombatResult { Winner = CombatWinner.Left, LeftAliveEffective = 3, RightAliveEffective = 0 }),
                (new PairingResult { LeftPlayerId = p2, RightPlayerId = p3 },
                 new CombatResult { Winner = CombatWinner.Left, LeftAliveEffective = 2, RightAliveEffective = 0 }),
            };

            SettlementService.ProcessResults(room, lethalResults);

            mp1 = room.FindPlayerById(p1);
            if (mp1.Hp > 0)
                throw new Exception($"Settlement: p1 should be dead, HP={mp1.Hp}");
            if (mp1.IsAlive)
                throw new Exception("Settlement: p1 should not be alive after lethal damage");

            // Cleanup
            scene.RemoveComponent<MatchComponent>();

            Log.Info("[AutoChess] TestSettlement passed.");
        }

        /// <summary>
        /// 验证 CombatStartProcessor: 刺客跳后排和亡灵诅咒。
        /// </summary>
        public static void TestCombatStartProcessor()
        {
            // === Assassin Jump ===
            {
                var assassin = MakeUnit(1, 0, col: 3, row: 4, atk: 100, hp: 400, range: 1, atkSpeed: 1.0f);
                assassin.Tags = new[] { "Assassin" };

                var backEnemy = MakeUnit(10, 1, col: 3, row: 0, atk: 80, hp: 300, range: 1, atkSpeed: 1.0f);

                var allUnits = new List<CombatUnitState> { assassin, backEnemy };
                var events = new List<CombatEvent>();
                int eventIdx = 0;

                bool[,] occupied = new bool[AutoChessDefine.BoardWidth, AutoChessDefine.BoardHeight];
                occupied[3, 4] = true;
                occupied[3, 0] = true;

                var leftSnap = new TraitSnapshot
                {
                    ActiveSynergies = new List<SynergyEntry>
                    {
                        new SynergyEntry { Tag = "Assassin", Level = 1 }
                    }
                };

                CombatStartProcessor.Process(allUnits, leftSnap, null, events, ref eventIdx, occupied);

                // Assassin should have jumped near backEnemy
                if (assassin.Row == 4)
                    throw new Exception("CombatStart Assassin: should have jumped from row 4");
                if (assassin.Row > 1)
                    throw new Exception($"CombatStart Assassin: expected row <= 1, got {assassin.Row}");

                bool hasMoveEvent = false;
                foreach (var e in events)
                {
                    if (e.EventType == CombatEventType.Move && e.SourceInstId == 1) hasMoveEvent = true;
                }
                if (!hasMoveEvent)
                    throw new Exception("CombatStart Assassin: expected Move event");
            }

            // === Undead Curse ===
            {
                var ally = MakeUnit(1, 0, col: 0, row: 3, atk: 100, hp: 400, range: 1, atkSpeed: 1.0f);
                var enemy1 = MakeUnit(10, 1, col: 0, row: 1, atk: 80, hp: 1000, range: 1, atkSpeed: 1.0f);
                var enemy2 = MakeUnit(11, 1, col: 1, row: 1, atk: 60, hp: 800, range: 1, atkSpeed: 1.0f);
                var enemy3 = MakeUnit(12, 1, col: 2, row: 1, atk: 50, hp: 600, range: 1, atkSpeed: 1.0f);

                var allUnits = new List<CombatUnitState> { ally, enemy1, enemy2, enemy3 };
                var events = new List<CombatEvent>();
                int eventIdx = 0;

                bool[,] occupied = new bool[AutoChessDefine.BoardWidth, AutoChessDefine.BoardHeight];

                // Level 1 undead: curse top 2 by maxHp, 75% HP
                var leftSnap = new TraitSnapshot
                {
                    ActiveSynergies = new List<SynergyEntry>
                    {
                        new SynergyEntry { Tag = "Undead", Level = 1 }
                    }
                };

                CombatStartProcessor.Process(allUnits, leftSnap, null, events, ref eventIdx, occupied);

                // enemy1 (1000 HP) cursed → 750
                if (enemy1.MaxHp != 750)
                    throw new Exception($"CombatStart Undead: enemy1 MaxHp expected 750, got {enemy1.MaxHp}");
                // enemy2 (800 HP) cursed → 600
                if (enemy2.MaxHp != 600)
                    throw new Exception($"CombatStart Undead: enemy2 MaxHp expected 600, got {enemy2.MaxHp}");
                // enemy3 should not be cursed (only top 2 at level 1)
                if (enemy3.MaxHp != 600)
                    throw new Exception($"CombatStart Undead: enemy3 MaxHp should be 600, got {enemy3.MaxHp}");
                if (!enemy1.IsCursedByUndead)
                    throw new Exception("CombatStart Undead: enemy1 should be cursed");
            }

            Log.Info("[AutoChess] TestCombatStartProcessor passed.");
        }

        /// <summary>
        /// 验证狂暴机制：tick >= 400 时攻速翻倍。
        /// </summary>
        public static void TestFrenzy()
        {
            // Create 1v1 with very high HP so battle lasts to frenzy
            var leftUnits = new List<CombatUnitState>
            {
                MakeUnit(1, 0, col: 3, row: 4, atk: 10, hp: 50000, range: 1, atkSpeed: 1.0f),
            };
            var rightUnits = new List<CombatUnitState>
            {
                MakeUnit(10, 1, col: 3, row: 0, atk: 10, hp: 50000, range: 1, atkSpeed: 1.0f),
            };

            CombatResult result = CombatSimulator.RunCombat(leftUnits, rightUnits, null, null, null, 1);

            // With 50000 HP each and 10 atk, battle should hit frenzy or time out
            // Verify frenzy event exists
            bool hasFrenzy = false;
            foreach (var e in result.Events)
            {
                if (e.EventType == CombatEventType.FrenzyStart)
                {
                    hasFrenzy = true;
                    if (e.Tick != AutoChessDefine.FrenzyStartTick)
                        throw new Exception($"Frenzy: expected at tick {AutoChessDefine.FrenzyStartTick}, got {e.Tick}");
                }
            }
            if (!hasFrenzy)
                throw new Exception("Frenzy: expected FrenzyStart event in long battle");

            Log.Info("[AutoChess] TestFrenzy passed.");
        }

        // ===== HELPER =====

        private static CombatUnitState MakeUnit(int instId, int side, int col, int row, int atk, int hp, int range, float atkSpeed)
        {
            return new CombatUnitState
            {
                InstId = instId,
                TemplateId = 1,
                Star = 1,
                Side = side,
                Col = col,
                Row = row,
                FacingCol = col,
                FacingRow = side == 0 ? 0 : AutoChessDefine.BoardHeight - 1,
                Atk = atk,
                Hp = hp,
                MaxHp = hp,
                AtkSpeed = atkSpeed,
                Range = range,
                MoveSpeed = 1.0f,
                CritChance = 0f,
                Mana = 0,
                ManaGainOnAttack = 10,
                ManaGainOnHit = 6,
                IsAlive = true,
                IsEffectiveForDamageCount = true,
                DamageMultiplier = 1.0f,
                AtkSpeedMultiplier = 1.0f,
                HpMultiplier = 1.0f,
                Buffs = new List<ActiveBuff>(),
                Trigger = new TriggerState(),
                LastAttackTick = -999,
                LastMoveTick = -999,
                AttackTargetInstId = -1,
            };
        }

        /// <summary>
        /// 验证 Proto 转换辅助方法字段映射正确。
        /// </summary>
        public static void TestProtoConversion()
        {
            // 1. CombatEvent → Proto
            var evt = new CombatEvent
            {
                I = 7,
                Tick = 42,
                EventType = CombatEventType.Damage,
                SourceInstId = 1,
                TargetInstId = 2,
                Amount = 50,
                HpAfter = 30,
                IsCrit = true,
                Col = 3,
                Row = 4,
                TemplateId = 101,
                Star = 2,
                Side = 1,
                MaxHp = 100,
                BType = BuffType.None,
                DurationTicks = 0,
                DamageType = CombatDamageType.Normal,
                SkillDefId = 5,
                Winner = CombatWinner.Draw,
                TimeUp = false,
                LeftAliveEffective = 3,
                RightAliveEffective = 2,
            };

            AutoChessCombatEventProto proto = AutoChessProtoHelper.ToCombatEventProto(evt);
            if (proto.I != 7) throw new Exception($"Proto I mismatch: {proto.I}");
            if (proto.Tick != 42) throw new Exception($"Proto Tick mismatch: {proto.Tick}");
            if (proto.EventType != (int)CombatEventType.Damage) throw new Exception("Proto EventType mismatch");
            if (proto.SourceInstId != 1) throw new Exception("Proto SourceInstId mismatch");
            if (proto.TargetInstId != 2) throw new Exception("Proto TargetInstId mismatch");
            if (proto.Amount != 50) throw new Exception("Proto Amount mismatch");
            if (proto.HpAfter != 30) throw new Exception("Proto HpAfter mismatch");
            if (!proto.IsCrit) throw new Exception("Proto IsCrit mismatch");
            if (proto.Col != 3) throw new Exception("Proto Col mismatch");
            if (proto.Row != 4) throw new Exception("Proto Row mismatch");
            if (proto.TemplateId != 101) throw new Exception("Proto TemplateId mismatch");
            if (proto.Star != 2) throw new Exception("Proto Star mismatch");
            if (proto.Side != 1) throw new Exception("Proto Side mismatch");
            if (proto.MaxHp != 100) throw new Exception("Proto MaxHp mismatch");
            if (proto.DamageType != (int)CombatDamageType.Normal) throw new Exception("Proto DamageType mismatch");
            if (proto.SkillDefId != 5) throw new Exception("Proto SkillDefId mismatch");
            if (proto.LeftAliveEffective != 3) throw new Exception("Proto LeftAliveEffective mismatch");
            if (proto.RightAliveEffective != 2) throw new Exception("Proto RightAliveEffective mismatch");

            // 2. UnitInfo → Proto
            var unit = new UnitInfo { InstId = 10, TemplateId = 201, Star = 3, Col = 5, Row = 2 };
            AutoChessUnitInfoProto unitProto = AutoChessProtoHelper.ToUnitInfoProto(unit, 0);
            if (unitProto.InstId != 10) throw new Exception("UnitProto InstId mismatch");
            if (unitProto.TemplateId != 201) throw new Exception("UnitProto TemplateId mismatch");
            if (unitProto.Star != 3) throw new Exception("UnitProto Star mismatch");
            if (unitProto.Col != 5) throw new Exception("UnitProto Col mismatch");
            if (unitProto.Row != 2) throw new Exception("UnitProto Row mismatch");
            if (unitProto.Location != 0) throw new Exception("UnitProto Location mismatch");

            // 板凳
            AutoChessUnitInfoProto benchProto = AutoChessProtoHelper.ToUnitInfoProto(unit, 1);
            if (benchProto.Location != 1) throw new Exception("BenchProto Location mismatch");

            // 3. MatchPlayer → PlayerPublicInfoProto
            // 无法直接构造 MatchPlayer（Entity），跳过此验证

            Log.Info("[AutoChess] ProtoConversion test passed: all field mappings correct");
        }

        /// <summary>
        /// 验证 BroadcastHelper 在 MapUnitId=0 时不崩溃（优雅跳过）。
        /// </summary>
        public static void TestBroadcastHelper(Scene scene)
        {
            MatchComponent matchComp = scene.GetComponent<MatchComponent>() ?? scene.AddComponent<MatchComponent>();
            MatchRoom room = MatchRoomFactory.CreateMatch(matchComp, new List<long> { 9001, 9002 }, 99999);

            // 所有 MatchPlayer 的 MapUnitId 默认为 0（未绑定 Unit）
            // BroadcastToAlive 应该不崩溃，静默跳过所有玩家
            M2C_AutoChessPhaseChange testMsg = M2C_AutoChessPhaseChange.Create();
            testMsg.Round = 1;
            testMsg.NewPhase = (int)RoundPhase.Deployment;
            testMsg.PhaseEndTime = 0;

            // 不应抛出异常
            AutoChessBroadcastHelper.BroadcastToAlive(scene, room, testMsg);
            AutoChessBroadcastHelper.BroadcastToAll(scene, room, testMsg);

            // SendToPlayer with MapUnitId=0 — 应静默跳过
            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                AutoChessBroadcastHelper.SendToPlayer(scene, player, testMsg);
            }

            // 清理
            room.Dispose();

            Log.Info("[AutoChess] BroadcastHelper test passed: MapUnitId=0 gracefully skipped");
        }

        private static MatchRoom CreateTestRoom(int playerCount, Scene scene = null)
        {
            var dummyPlayers = new List<long>();
            for (int i = 0; i < playerCount; i++)
            {
                dummyPlayers.Add(1000 + i);
            }

            if (scene == null)
            {
                // This variant won't work for tests needing scene;
                // caller should provide scene
                throw new Exception("CreateTestRoom requires a Scene");
            }

            MatchComponent matchComp = scene.GetComponent<MatchComponent>() ?? scene.AddComponent<MatchComponent>();
            return MatchRoomFactory.CreateMatch(matchComp, dummyPlayers, 42);
        }
    }
}
