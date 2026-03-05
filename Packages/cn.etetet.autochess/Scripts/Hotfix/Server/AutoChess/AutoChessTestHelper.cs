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
    }
}
