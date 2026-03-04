# A1 单位系统 Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 实现自走棋单位实例的完整生命周期——Roster 管理、摆位操作、自动合成、PreBattle 校验，并集成到现有商店/经济系统。

**Architecture:** 新增 RosterComponent（ComponentOf MatchPlayer）存储 List\<UnitInfo\>（平坦列表，n<=13）。6 个静态服务类分工协作：RosterService（增删查）、PlacementService（Place/Move/Swap）、MergeService（确定性合成链）、PreBattleService（校验修正）、UnitService（门面协调）。PhaseChangedEventHandler_Unit 在 Deployment/PreBattle 阶段触发合成和校验。

**Tech Stack:** C# / .NET 8 / ET9 ECS / cn.etetet.autochess 包

**Impact:**
- Modules/Assemblies: `cn.etetet.autochess` — Model/Server（新增 2 文件）、Hotfix/Server（新增 7 文件、修改 3 文件）
- Code generation changes: No
- New data models: UnitInfo（纯 C# 类 [EnableClass]）、RosterComponent（ComponentOf: MatchPlayer）
- New messages/protocols: None（E9 负责）

**Rules:** ecs-patterns, code-templates, code-review-checklist

**Design Ref:** docs/changes/a1-unit/design.md

---

## 1. 数据模型

- [x] 1.1 创建 UnitInfo 数据类

**Context:**
- Why: 定义单位实例的值对象，所有后续服务类都依赖此数据结构

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/UnitInfo.cs`

**Steps:**
1. 创建文件，包含以下完整代码：

```csharp
namespace ET.Server
{
    /// <summary>
    /// 单位实例数据。纯 C# 类，非 Entity。
    /// Col/Row 表示位置：棋盘 Row>=0, Col=[0,7]；板凳 Row==-1, Col=[0,4]。
    /// </summary>
    [EnableClass]
    public class UnitInfo
    {
        public int InstId;       // per-player 自增 ID，用于确定性排序
        public int TemplateId;   // 单位模板 ID（1-24）
        public int Star;         // 星级（1/2/3）
        public bool IsGift;      // 礼品单位（出售/淘汰不回流卡池）
        public int Col;          // 列坐标
        public int Row;          // 行坐标（-1=板凳）
    }
}
```

2. 编译检查：`dotnet build Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/`（或全项目 `dotnet build`）
3. Commit: `feat(a1-unit): add UnitInfo data class`

- [x] 1.2 创建 RosterComponent 及其 System

**Context:**
- Depends: 1.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs`
- Why: 挂在 MatchPlayer 上的组件，持有该玩家所有单位实例列表和 InstId 自增计数器

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/RosterComponent.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/RosterComponentSystem.cs`

**Steps:**
1. 创建 RosterComponent（Model/Server）：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(MatchPlayer))]
    public class RosterComponent : Entity, IAwake, IDestroy
    {
        public List<UnitInfo> Units;
        public int NextInstId;  // 下一个 UnitInfo.InstId，从 1 自增
    }
}
```

2. 创建 RosterComponentSystem（Hotfix/Server）：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RosterComponent))]
    [FriendOf(typeof(RosterComponent))]
    public static partial class RosterComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RosterComponent self)
        {
            self.Units = new List<UnitInfo>();
            self.NextInstId = 1;
        }

        [EntitySystem]
        private static void Destroy(this RosterComponent self)
        {
            self.Units.Clear();
            self.Units = null;
            self.NextInstId = 0;
        }
    }
}
```

3. 编译检查
4. Commit: `feat(a1-unit): add RosterComponent with Awake/Destroy`

- [x] 1.3 扩展 MatchRoomFactory 添加 RosterComponent

**Context:**
- Depends: 1.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`
- Why: 每个 MatchPlayer 创建时需要初始化 RosterComponent

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`

**Steps:**
1. 在 MatchRoomFactory.CreateMatch 的 foreach 循环中，`player.AddComponent<ShopComponent>()` 之后添加：

```csharp
player.AddComponent<RosterComponent>();
```

2. 编译检查
3. Commit: `feat(a1-unit): initialize RosterComponent in MatchRoomFactory`

---

## 2. RosterService 核心服务

- [x] 2.1 创建 RosterService 及其测试（AddToBench + Remove + 查询）

**Context:**
- Depends: 1.3
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`, `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: 单位增删查的唯一入口，所有操作（购买、出售、合成、淘汰）都通过此服务

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/RosterService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 先在 AutoChessTestHelper 中编写 TestRoster 方法骨架（测试先行）：

```csharp
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
```

2. 在 RunAllTests 中 TestShop 之后添加 `TestRoster(scene);`

3. 创建 RosterService（Hotfix/Server）：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class RosterService
    {
        /// <summary>
        /// 添加单位到板凳最小空位。板凳满则返回 null。
        /// </summary>
        public static UnitInfo AddToBench(MatchPlayer player, int templateId, int star, bool isGift)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();

            // 找板凳最小空位
            int freeCol = FindFreeBenchCol(roster);
            if (freeCol < 0) return null;

            UnitInfo unit = new UnitInfo
            {
                InstId = roster.NextInstId++,
                TemplateId = templateId,
                Star = star,
                IsGift = isGift,
                Col = freeCol,
                Row = -1,
            };
            roster.Units.Add(unit);
            return unit;
        }

        /// <summary>
        /// 移除单位。
        /// </summary>
        public static void Remove(MatchPlayer player, UnitInfo unit)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            roster.Units.Remove(unit);
        }

        /// <summary>
        /// 按棋盘/板凳坐标查找。
        /// </summary>
        public static UnitInfo FindAt(MatchPlayer player, int col, int row)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Col == col && u.Row == row) return u;
            }
            return null;
        }

        /// <summary>
        /// 按 InstId 查找。
        /// </summary>
        public static UnitInfo FindByInstId(MatchPlayer player, int instId)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.InstId == instId) return u;
            }
            return null;
        }

        /// <summary>
        /// 获取棋盘上的单位列表（Row >= 0）。
        /// </summary>
        public static List<UnitInfo> GetBoardUnits(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            List<UnitInfo> result = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) result.Add(u);
            }
            return result;
        }

        /// <summary>
        /// 获取板凳上的单位列表（Row == -1）。
        /// </summary>
        public static List<UnitInfo> GetBenchUnits(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            List<UnitInfo> result = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1) result.Add(u);
            }
            return result;
        }

        /// <summary>
        /// 棋盘上单位数（= 人口使用量）。
        /// </summary>
        public static int GetPopUsed(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int count = 0;
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) count++;
            }
            return count;
        }

        /// <summary>
        /// 板凳上单位数。
        /// </summary>
        public static int GetBenchUsed(MatchPlayer player)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int count = 0;
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1) count++;
            }
            return count;
        }

        /// <summary>
        /// 淘汰回收：所有单位回流卡池（isGift 不回流），清空列表。
        /// 按星级计算回流份数：1★→1, 2★→2, 3★→4（即 1 << (star-1)）。
        /// </summary>
        public static void RecoverAllToPool(MatchPlayer player, SharedPoolComponent pool)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            foreach (UnitInfo u in roster.Units)
            {
                if (!u.IsGift && u.TemplateId > 0 && u.TemplateId <= AutoChessDefine.TotalUnitTemplates)
                {
                    int copies = 1 << (u.Star - 1);
                    pool.Remaining[u.TemplateId - 1] += copies;
                }
            }
            roster.Units.Clear();
        }

        // --- Private helpers ---

        private static int FindFreeBenchCol(RosterComponent roster)
        {
            bool[] occupied = new bool[AutoChessDefine.BenchSize];
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1 && u.Col >= 0 && u.Col < AutoChessDefine.BenchSize)
                    occupied[u.Col] = true;
            }
            for (int i = 0; i < AutoChessDefine.BenchSize; i++)
            {
                if (!occupied[i]) return i;
            }
            return -1; // 板凳满
        }
    }
}
```

4. 编译检查
5. Commit: `feat(a1-unit): add RosterService with AddToBench/Remove/Find/Stats/Recover`

---

## 3. PlacementService 摆位操作

- [x] 3.1 创建 PlacementService 及其测试（Place/Move/Swap）

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/HexCoord.cs`
- Why: 实现 Story 4.2 的三种摆位操作，含阶段门控和坐标合法性校验

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PlacementService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 AutoChessTestHelper 中编写 TestPlacement 方法：

```csharp
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
```

2. 在 RunAllTests 中添加 `TestPlacement(scene);`

3. 创建 PlacementService（Hotfix/Server）：

```csharp
namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class PlacementService
    {
        /// <summary>
        /// 板凳→棋盘。检查人口上限 + 目标格空 + 坐标合法。
        /// </summary>
        public static bool TryPlaceToBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow, int popCap)
        {
            // 坐标合法性
            if (!new HexCoord(targetCol, targetRow).IsValid()) return false;

            UnitInfo unit = RosterService.FindByInstId(player, unitInstId);
            if (unit == null) return false;

            // 必须在板凳上
            if (unit.Row != -1) return false;

            // 人口上限
            if (RosterService.GetPopUsed(player) >= popCap) return false;

            // 目标格空
            if (RosterService.FindAt(player, targetCol, targetRow) != null) return false;

            unit.Col = targetCol;
            unit.Row = targetRow;
            return true;
        }

        /// <summary>
        /// 棋盘内移动。单位必须在棋盘上，目标格必须空。
        /// </summary>
        public static bool TryMoveOnBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow)
        {
            if (!new HexCoord(targetCol, targetRow).IsValid()) return false;

            UnitInfo unit = RosterService.FindByInstId(player, unitInstId);
            if (unit == null) return false;

            // 必须在棋盘上
            if (unit.Row < 0) return false;

            // 目标格空
            if (RosterService.FindAt(player, targetCol, targetRow) != null) return false;

            unit.Col = targetCol;
            unit.Row = targetRow;
            return true;
        }

        /// <summary>
        /// 任意两位置互换（棋盘↔棋盘、棋盘↔板凳、板凳↔板凳）。
        /// </summary>
        public static bool TrySwap(MatchPlayer player, int instId1, int instId2)
        {
            if (instId1 == instId2) return false;

            UnitInfo u1 = RosterService.FindByInstId(player, instId1);
            UnitInfo u2 = RosterService.FindByInstId(player, instId2);
            if (u1 == null || u2 == null) return false;

            // 交换坐标
            (u1.Col, u2.Col) = (u2.Col, u1.Col);
            (u1.Row, u2.Row) = (u2.Row, u1.Row);
            return true;
        }
    }
}
```

4. 编译检查
5. Commit: `feat(a1-unit): add PlacementService with Place/Move/Swap`

---

## 4. MergeService 自动合成

- [x] 4.1 创建 MergeService 及其测试（确定性合成链）

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyService.cs`, `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: 实现 Story 4.3 的 2 合 1 升星 + 连锁合成，确定性扫描顺序是自走棋公平性的关键

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MergeService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 AutoChessTestHelper 中编写 TestMerge 方法：

```csharp
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
```

2. 在 RunAllTests 中添加 `TestMerge(scene);`

3. 创建 MergeService（Hotfix/Server）：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class MergeService
    {
        /// <summary>
        /// 执行完整合成链：从 1★ 开始扫描，直到一轮无合并。返回合成次数。
        /// 确定性排序 key: (star ASC, templateId ASC, locationPriority ASC [board=0,bench=1], row ASC, col ASC, instId ASC)
        /// </summary>
        public static int RunMergeChain(MatchPlayer player, int round)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();
            int totalMerges = 0;

            while (true)
            {
                bool merged = false;

                // 按确定性顺序排序
                roster.Units.Sort(CompareForMerge);

                // 找第一对可合成单位
                for (int i = 0; i < roster.Units.Count - 1; i++)
                {
                    UnitInfo a = roster.Units[i];
                    if (a.Star >= AutoChessDefine.MaxStarLevel) continue;

                    for (int j = i + 1; j < roster.Units.Count; j++)
                    {
                        UnitInfo b = roster.Units[j];
                        if (b.TemplateId == a.TemplateId && b.Star == a.Star)
                        {
                            // 合并: a(primary) 保留, b(secondary) 消耗
                            a.Star++;
                            roster.Units.RemoveAt(j);
                            EconomyService.GiveMerge(player, round);
                            totalMerges++;
                            merged = true;
                            break;
                        }
                    }
                    if (merged) break;
                }

                if (!merged) break;
            }

            return totalMerges;
        }

        private static int CompareForMerge(UnitInfo a, UnitInfo b)
        {
            // star ASC
            int cmp = a.Star.CompareTo(b.Star);
            if (cmp != 0) return cmp;

            // templateId ASC
            cmp = a.TemplateId.CompareTo(b.TemplateId);
            if (cmp != 0) return cmp;

            // locationPriority ASC: board(row>=0)=0, bench(row==-1)=1
            int locA = a.Row >= 0 ? 0 : 1;
            int locB = b.Row >= 0 ? 0 : 1;
            cmp = locA.CompareTo(locB);
            if (cmp != 0) return cmp;

            // row ASC (板凳 row==-1 排前面，但 locationPriority 已区分)
            cmp = a.Row.CompareTo(b.Row);
            if (cmp != 0) return cmp;

            // col ASC
            cmp = a.Col.CompareTo(b.Col);
            if (cmp != 0) return cmp;

            // instId ASC
            return a.InstId.CompareTo(b.InstId);
        }
    }
}
```

4. 编译检查
5. Commit: `feat(a1-unit): add MergeService with deterministic chain merge`

---

## 5. PreBattleService 校验与修正

- [x] 5.1 创建 PreBattleService 及其测试

**Context:**
- Depends: 2.1, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`
- Why: 实现 Story 4.4，确保进入战斗时阵容合法（人口不超限、无坐标冲突）

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PreBattleService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 AutoChessTestHelper 中编写 TestPreBattle 方法：

```csharp
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
```

2. 在 RunAllTests 中添加 `TestPreBattle(scene);`

3. 创建 PreBattleService（Hotfix/Server）：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class PreBattleService
    {
        /// <summary>
        /// PreBattle 校验 + 自动修正。
        /// 超人口单位按保留优先级（star DESC, cost DESC, row ASC, col ASC, instId ASC）排序，
        /// 优先级最低的移到板凳；板凳满则强制出售。
        /// </summary>
        public static void ValidateAndFix(MatchPlayer player, SharedPoolComponent pool, int popCap, int round)
        {
            RosterComponent roster = player.GetComponent<RosterComponent>();

            // 获取棋盘单位，按保留优先级降序排序
            List<UnitInfo> boardUnits = new List<UnitInfo>();
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row >= 0) boardUnits.Add(u);
            }

            if (boardUnits.Count <= popCap) return;

            // 按保留优先级降序排序（高优先级在前）
            boardUnits.Sort(CompareRetainPriority);

            // 超出 popCap 的单位需要移走（列表末尾 = 最低优先级）
            for (int i = boardUnits.Count - 1; i >= popCap; i--)
            {
                UnitInfo victim = boardUnits[i];

                // 尝试移到板凳
                int freeCol = FindFreeBenchCol(roster);
                if (freeCol >= 0)
                {
                    victim.Col = freeCol;
                    victim.Row = -1;
                }
                else
                {
                    // 板凳满，强制出售
                    RosterService.Remove(player, victim);
                    ShopService.TrySell(player, victim.TemplateId, victim.Star, victim.IsGift, pool, round);
                }
            }
        }

        /// <summary>
        /// 保留优先级（降序）：star DESC → cost DESC → row ASC → col ASC → instId ASC
        /// </summary>
        private static int CompareRetainPriority(UnitInfo a, UnitInfo b)
        {
            // star DESC
            int cmp = b.Star.CompareTo(a.Star);
            if (cmp != 0) return cmp;

            // cost DESC
            int costA = AutoChessConfigLoader.GetUnit(a.TemplateId).Cost;
            int costB = AutoChessConfigLoader.GetUnit(b.TemplateId).Cost;
            cmp = costB.CompareTo(costA);
            if (cmp != 0) return cmp;

            // row ASC
            cmp = a.Row.CompareTo(b.Row);
            if (cmp != 0) return cmp;

            // col ASC
            cmp = a.Col.CompareTo(b.Col);
            if (cmp != 0) return cmp;

            // instId ASC
            return a.InstId.CompareTo(b.InstId);
        }

        private static int FindFreeBenchCol(RosterComponent roster)
        {
            bool[] occupied = new bool[AutoChessDefine.BenchSize];
            foreach (UnitInfo u in roster.Units)
            {
                if (u.Row == -1 && u.Col >= 0 && u.Col < AutoChessDefine.BenchSize)
                    occupied[u.Col] = true;
            }
            for (int i = 0; i < AutoChessDefine.BenchSize; i++)
            {
                if (!occupied[i]) return i;
            }
            return -1;
        }
    }
}
```

4. 编译检查
5. Commit: `feat(a1-unit): add PreBattleService with over-pop fix and forced sell`

---

## 6. UnitService 门面 + 事件处理器

- [x] 6.1 创建 UnitService 门面类及其测试（Buy/Sell 集成）

**Context:**
- Depends: 2.1, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`
- Why: 协调 ShopService + RosterService + MergeService 的组合调用，是 E9 Handler 的唯一入口

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/UnitService.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 AutoChessTestHelper 中编写 TestUnitService 方法：

```csharp
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

    // --- 3. Buy 失败不创建 UnitInfo ---
    p1.Elixir = 0;
    ShopService.GenerateOffersForPlayer(p1, pool, rng);
    bool buyFail = UnitService.Buy(p1, 0, pool, rng, RoundPhase.Deployment, 1);
    if (buyFail) throw new Exception("UnitService.Buy should fail when no elixir");
    if (RosterService.GetBenchUsed(p1) != 0) throw new Exception("No unit should be created on failed buy");

    scene.RemoveComponent<MatchComponent>();
    Log.Info("[AutoChess] UnitService test passed: buy/sell integration correct");
}
```

2. 在 RunAllTests 中添加 `TestUnitService(scene);`

3. 创建 UnitService（Hotfix/Server）：

```csharp
namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(ShopComponent))]
    public static class UnitService
    {
        /// <summary>
        /// 购买：ShopService.TryBuy + RosterService.AddToBench + 可选合成。
        /// </summary>
        public static bool Buy(MatchPlayer player, int slotIndex,
            SharedPoolComponent pool, DeterministicRngComponent rng,
            RoundPhase currentPhase, int round)
        {
            int templateId = ShopService.TryBuy(player, slotIndex, pool, rng, currentPhase, round);
            if (templateId <= 0) return false;

            UnitInfo unit = RosterService.AddToBench(player, templateId, 1, false);
            if (unit == null)
            {
                // 板凳满，退回购买（回流圣水和卡池）
                ShopService.TrySell(player, templateId, 1, false, pool, round);
                return false;
            }

            // Deployment 阶段触发合成
            if (currentPhase == RoundPhase.Deployment)
            {
                MergeService.RunMergeChain(player, round);
            }

            return true;
        }

        /// <summary>
        /// 出售：RosterService.Remove + ShopService.TrySell。
        /// </summary>
        public static void Sell(MatchPlayer player, UnitInfo unit,
            SharedPoolComponent pool, int round)
        {
            int templateId = unit.TemplateId;
            int star = unit.Star;
            bool isGift = unit.IsGift;

            RosterService.Remove(player, unit);
            ShopService.TrySell(player, templateId, star, isGift, pool, round);
        }

        /// <summary>
        /// 首回合赠送：将已选好的礼包模板创建为 UnitInfo 入板凳。
        /// </summary>
        public static void CreateFirstRoundGift(MatchPlayer player)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();
            int giftTemplateId = shop.FirstRoundGiftTemplateId;
            if (giftTemplateId <= 0) return;

            RosterService.AddToBench(player, giftTemplateId, 1, true);
        }
    }
}
```

4. 编译检查
5. Commit: `feat(a1-unit): add UnitService facade with Buy/Sell/CreateFirstRoundGift`

- [x] 6.2 创建 PhaseChangedEventHandler_Unit

**Context:**
- Depends: 4.1, 5.1, 6.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Shop.cs`
- Why: 在 Deployment 阶段触发合成链（处理上回合 Battle 买入），在 PreBattle 阶段触发校验修正

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Unit.cs`

**Steps:**
1. 创建事件处理器：

```csharp
namespace ET.Server
{
    /// <summary>
    /// Deployment: 触发合成链（处理上回合 Battle 阶段买入的重复单位）。
    /// PreBattle: 触发合法性校验与自动修正。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Unit : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            if (args.NewPhase != RoundPhase.Deployment && args.NewPhase != RoundPhase.PreBattle)
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

            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();
            int popCap = room.GetPopCap();

            if (args.NewPhase == RoundPhase.Deployment)
            {
                // Deployment 阶段：触发合成链
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    MergeService.RunMergeChain(player, args.Round);
                }
            }
            else if (args.NewPhase == RoundPhase.PreBattle)
            {
                // PreBattle 阶段：校验 + 自动修正
                foreach (MatchPlayer player in room.GetAlivePlayers())
                {
                    PreBattleService.ValidateAndFix(player, pool, popCap, args.Round);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
```

2. 编译检查
3. Commit: `feat(a1-unit): add PhaseChangedEventHandler_Unit for merge and prebattle`

- [x] 6.3 修改 PhaseChangedEventHandler_Shop 添加首回合赠送入板凳

**Context:**
- Depends: 6.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Shop.cs`
- Why: 首回合赠送需要在 Handler_Shop 中直接调用 UnitService.CreateFirstRoundGift，避免事件处理器顺序依赖

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Shop.cs`

**Steps:**
1. 在 PhaseChangedEventHandler_Shop 的首回合分支中，`FirstRoundGiftService.SelectGiftTemplate(player, rng)` 之后添加：

```csharp
// 创建赠送单位入板凳
UnitService.CreateFirstRoundGift(player);
```

2. 编译检查
3. Commit: `feat(a1-unit): integrate first round gift UnitInfo creation`

---

## 7. 淘汰回收集成

- [x] 7.1 扩展 EliminatePlayer 回收棋盘/板凳单位

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`
- Why: 淘汰时棋盘+板凳单位需要回流卡池（isGift 不回流），否则卡池库存泄漏

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 AutoChessTestHelper 中编写 TestEliminationRecovery 方法：

```csharp
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
    UnitInfo gift = RosterService.AddToBench(p2, 3, 1, true);  // gift → 不回流

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
```

2. 在 RunAllTests 中添加 `TestEliminationRecovery(scene);`

3. 在 MatchRoomSystem.EliminatePlayer 中，在现有 Shop Offer 回收代码之后，break 之前添加：

```csharp
// 回收棋盘+板凳所有单位到卡池
RosterService.RecoverAllToPool(player, pool);
```

注意：MatchRoomSystem 已有 `[FriendOf(typeof(SharedPoolComponent))]`，调用 RosterService 的公开方法不需要额外 FriendOf。

4. 编译检查
5. Commit: `feat(a1-unit): recover roster units to pool on elimination`

---

## 8. 全局编译验证与最终审查

- [x] 8.1 全项目编译检查

**Context:**
- Depends: 1.3, 2.1, 3.1, 4.1, 5.1, 6.1, 6.2, 6.3, 7.1
- Why: 确保所有新增和修改文件无编译错误、无分析器警告

**Steps:**
1. 运行 `dotnet build` 全项目编译
2. 确认 0 errors, 0 warnings
3. 如有错误，逐一修复

- [x] 8.2 运行集成测试

**Context:**
- Depends: 8.1
- Why: 端到端验证所有系统协同工作

**Steps:**
1. 确保 AutoChessTestHelper.RunAllTests 包含所有新测试：
   - TestConfigLoader
   - TestPrngDeterminism
   - TestEntityTree
   - TestPhaseGate
   - TestEconomy
   - TestShop
   - TestRoster (NEW)
   - TestPlacement (NEW)
   - TestMerge (NEW)
   - TestPreBattle (NEW)
   - TestUnitService (NEW)
   - TestEliminationRecovery (NEW)
2. 启动服务器运行测试验证全部通过
3. 如有失败，定位并修复

- [x] 8.3 更新 system-map.md

**Context:**
- Depends: 8.2
- Reads: `docs/system-map.md`
- Why: 保持系统关系图反映最新架构

**Steps:**
1. 在 system-map.md 的 `## 自走棋系统（cn.etetet.autochess）` 章节中：
   - Entity 树结构：MatchPlayer 下添加 `RosterComponent (ComponentOf: MatchPlayer) — 单位列表 + InstId 自增`
   - 新增 `### 单位系统（a1-unit）` 小节，记录：
     - UnitInfo（纯 C# 类）
     - RosterService / PlacementService / MergeService / PreBattleService / UnitService
     - PhaseChangedEventHandler_Unit
   - 事件章节新增 Handler_Unit 的说明
2. Commit: `docs: update system-map.md with unit system`

- [x] 8.4 Final Review

**Context:**
- Depends: 8.3
- Why: 跨文件架构审查，确保无遗漏的 FriendOf、命名空间错误、ET 分析器违规

**Steps:**
1. 使用 `/qx-review` 或 code-reviewer Agent 执行最终审查
2. 检查清单：
   - [ ] 所有新 Entity 类没有方法定义
   - [ ] 所有 System 类有 `partial` 关键字
   - [ ] 所有 [FriendOf] 声明完整
   - [ ] UnitInfo 有 [EnableClass]
   - [ ] RosterComponent 有 [ComponentOf(typeof(MatchPlayer))]
   - [ ] 所有事件处理器有 [Event(SceneType.Map)]
   - [ ] 命名空间全部为 ET.Server
   - [ ] 无循环依赖
3. 修复审查发现的问题
4. Commit: `fix(a1-unit): address review findings`
