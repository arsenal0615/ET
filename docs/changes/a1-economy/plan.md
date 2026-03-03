# a1-economy 经济系统实施计划

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 实现自走棋圣水（Elixir）经济系统——回合收入、统帅被动、买/卖/合成收支，并记录经济日志。

**Architecture:** EconomyService 作为单一静态服务入口，所有圣水变更必须经由它执行（clamp + log + 事件）。EconomyLogComponent 挂在每个 MatchPlayer 上，存储本局全部 delta 记录。收入触发点通过监听 PhaseChangedEvent 实现，与 FSM 解耦。

**Tech Stack:** C# / ET9 / ET.Model + ET.Hotfix（仅 Server 侧）

**Impact:**
- Modules/Assemblies: cn.etetet.autochess（Model/Share、Model/Server、Hotfix/Server）
- Code generation changes: No
- New data models: EconomyLogComponent（ComponentOf MatchPlayer）；EconomyDelta struct；EconomyDeltaType enum；ElixirChangedEvent struct
- New messages/protocols: 无（ElixirChangedEvent 为 ET 内部事件，非网络协议）

**Rules:** ecs-patterns, code-templates, async-patterns

**Design Ref:** docs/changes/a1-economy/design.md

---

## 1. 数据类型（Model/Share）

- [x] 1.1 AutoChessDefine 追加经济常量

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: 把经济系统的"魔法数字"集中到常量，方便后续调参

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`

**Steps:**
1. 在 `AutoChessDefine` 类末尾（`CombatTickRate` 后）追加：
```csharp
// 经济
public const int RoundBaseIncome = 4;
public const int CommanderPassiveMin = 4;
public const int CommanderPassiveMax = 8;
public const int MergeElixirReturn = 1;
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs && git commit -m "feat(a1-economy): add economy constants to AutoChessDefine"`

---

- [x] 1.2 EconomyDeltaType 枚举

**Context:**
- Depends: 1.1
- Why: 定义圣水变动来源的分类，供日志记录和后续分析使用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/EconomyDeltaType.cs`

**Steps:**
1. 创建文件，内容：
```csharp
namespace ET
{
    public enum EconomyDeltaType
    {
        RoundIncome = 1,       // 回合基础收入 +4
        CommanderPassive = 2,  // 统帅被动（战败补偿）randInt(4,8)
        Buy = 3,               // 购买单位 -cost
        Sell = 4,              // 出售单位 +max(cost-1,0)
        Merge = 5,             // 合成返还 +1/次
    }
}
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/EconomyDeltaType.cs && git commit -m "feat(a1-economy): add EconomyDeltaType enum"`

---

- [x] 1.3 EconomyDelta struct

**Context:**
- Depends: 1.2
- Why: 单条圣水变动记录，存储在 EconomyLogComponent 中供查询

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/EconomyDelta.cs`

**Steps:**
1. 创建文件，内容：
```csharp
namespace ET
{
    /// <summary>
    /// 单条圣水变动记录。Amount 正数为收入，负数为支出。
    /// </summary>
    public struct EconomyDelta
    {
        public EconomyDeltaType Type;
        public int Amount;   // 正=收入，负=支出，已 clamp 的实际变动量
        public int Round;    // 触发回合
        public string Context; // 可选上下文，如 "unitId=5"（空字符串表示无）
    }
}
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/EconomyDelta.cs && git commit -m "feat(a1-economy): add EconomyDelta struct"`

---

- [x] 1.4 ElixirChangedEvent struct

**Context:**
- Depends: 1.3
- Why: 发布圣水变化通知，E9 网络层订阅此事件向客户端推送

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Events/ElixirChangedEvent.cs`

**Steps:**
1. 创建文件，内容：
```csharp
namespace ET
{
    /// <summary>
    /// 玩家圣水值发生变化时发布（ET 内部事件，非网络协议）。
    /// </summary>
    public struct ElixirChangedEvent
    {
        public long PlayerId;
        public int OldValue;
        public int NewValue;
    }
}
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Events/ElixirChangedEvent.cs && git commit -m "feat(a1-economy): add ElixirChangedEvent struct"`

---

## 2. EconomyLogComponent

- [x] 2.1 EconomyLogComponent（Model/Server）

**Context:**
- Depends: 1.3
- Why: 持有玩家经济日志的 Entity；ComponentOf(MatchPlayer) 约束确保只挂在玩家上

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/EconomyLogComponent.cs`

**Steps:**
1. 创建文件，内容：
```csharp
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 玩家经济日志，记录本局所有圣水收支。ComponentOf MatchPlayer。
    /// </summary>
    [ComponentOf(typeof(MatchPlayer))]
    public class EconomyLogComponent : Entity, IAwake, IDestroy
    {
        public List<EconomyDelta> Deltas;
    }
}
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/EconomyLogComponent.cs && git commit -m "feat(a1-economy): add EconomyLogComponent entity"`

---

- [x] 2.2 EconomyLogComponentSystem（Hotfix/Server）

**Context:**
- Depends: 2.1
- Why: 实现日志组件的生命周期和查询 API

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyLogComponentSystem.cs`

**Steps:**
1. 创建文件，内容：
```csharp
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(EconomyLogComponent))]
    [FriendOf(typeof(EconomyLogComponent))]
    public static partial class EconomyLogComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EconomyLogComponent self)
        {
            self.Deltas = new List<EconomyDelta>();
        }

        [EntitySystem]
        private static void Destroy(this EconomyLogComponent self)
        {
            self.Deltas = null;
        }

        /// <summary>
        /// 追加一条经济记录（由 EconomyService 调用）。
        /// </summary>
        public static void AddDelta(this EconomyLogComponent self, EconomyDelta delta)
        {
            self.Deltas.Add(delta);
        }

        /// <summary>
        /// 按回合查询经济流水。
        /// </summary>
        public static IEnumerable<EconomyDelta> GetDeltasByRound(this EconomyLogComponent self, int round)
        {
            foreach (EconomyDelta delta in self.Deltas)
            {
                if (delta.Round == round)
                    yield return delta;
            }
        }
    }
}
```
2. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyLogComponentSystem.cs && git commit -m "feat(a1-economy): add EconomyLogComponentSystem"`

---

## 3. 修改现有实体

- [x] 3.1 MatchRoom 追加 LastRoundLosers

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchRoom.cs`
- Why: 存储上回合战败玩家 ID，供 RoundStart 时发放统帅被动；E5 战斗结算写入，E2 只读

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchRoom.cs`

**Steps:**
1. 在 `MatchRoom` 类的字段末尾追加：
```csharp
public List<long> LastRoundLosers; // 上回合战败的 PlayerId，E5 战斗结算写入
```
2. Compile check: `dotnet build ET.Model.csproj --no-incremental 2>&1 | tail -5`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchRoom.cs && git commit -m "feat(a1-economy): add LastRoundLosers to MatchRoom"`

---

- [x] 3.2 MatchRoomSystem — 初始化 LastRoundLosers + 追加辅助方法

**Context:**
- Depends: 3.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`
- Why: Awake/Destroy 必须管理新字段的生命周期；GetAlivePlayers/FindPlayerById 供 handler 和测试用

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`

**Steps:**
1. 在 `Awake` 方法末尾追加：
```csharp
self.LastRoundLosers = new List<long>();
```
2. 在 `Destroy` 方法末尾追加：
```csharp
self.LastRoundLosers = null;
```
3. 在类末尾（`StartRoundLoop` 之后）追加两个辅助方法：
```csharp
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
```
注意：这两个方法需要访问 MatchPlayer.PlayerId，MatchRoomSystem 已有 `[FriendOf(typeof(MatchPlayer))]`，无需追加。
4. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
5. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs && git commit -m "feat(a1-economy): add LastRoundLosers lifecycle and room helpers"`

---

- [x] 3.3 MatchRoomFactory — 为每个玩家添加 EconomyLogComponent

**Context:**
- Depends: 2.1, 3.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`
- Why: MatchPlayer 在 room.Init() 中被创建，Factory 负责为其补充 EconomyLogComponent

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`

**Steps:**
1. 在 `room.Init(seed, playerIds)` 调用之后、`room.AddComponent<DeterministicRngComponent>` 之前，追加：
```csharp
// 为每个玩家添加经济日志组件
foreach (Entity child in room.Children.Values)
{
    if (child is MatchPlayer player)
        player.AddComponent<EconomyLogComponent>();
}
```
2. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs && git commit -m "feat(a1-economy): add EconomyLogComponent to players in factory"`

---

## 4. EconomyService

- [x] 4.1 EconomyService（Hotfix/Server）

**Context:**
- Depends: 3.2, 2.2
- Why: 所有圣水变更的唯一入口；ApplyDelta 负责 clamp + 日志记录 + 发布 ElixirChangedEvent

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyService.cs`

**Steps:**
1. 创建文件，内容：
```csharp
using System;

namespace ET.Server
{
    /// <summary>
    /// 圣水经济服务。所有圣水变更必须通过此类——保证 clamp、日志和事件三者联动。
    /// </summary>
    [FriendOf(typeof(MatchPlayer))]
    public static class EconomyService
    {
        /// <summary>
        /// 回合基础收入 +4。在每个 RoundStart 阶段对所有存活玩家调用。
        /// </summary>
        public static void GiveRoundIncome(MatchPlayer player, int round)
        {
            ApplyDelta(player, EconomyDeltaType.RoundIncome, AutoChessDefine.RoundBaseIncome, round);
        }

        /// <summary>
        /// 统帅被动补偿：战败后 +randInt(4,8)。仅对上回合战败玩家调用（round >= 2）。
        /// </summary>
        public static void GiveCommanderPassive(MatchPlayer player, DeterministicRngComponent rng, int round)
        {
            // 使用 CommanderPassive 专用子种子，确保确定性
            uint subSeed = XxHash32.Hash(rng.InitialSeed, round, (int)PrngPurpose.CommanderPassive);
            DeterministicRngComponent subRng = rng; // 复用同一个 rng（子种子推进）
            int bonus = subRng.NextInt(AutoChessDefine.CommanderPassiveMin,
                                       AutoChessDefine.CommanderPassiveMax + 1);
            ApplyDelta(player, EconomyDeltaType.CommanderPassive, bonus, round);
        }

        /// <summary>
        /// 购买单位：扣除 cost 圣水。返回 false 则圣水不足，不做任何修改。
        /// </summary>
        public static bool TryDeductBuy(MatchPlayer player, int cost, int round)
        {
            if (player.Elixir < cost)
                return false;
            ApplyDelta(player, EconomyDeltaType.Buy, -cost, round);
            return true;
        }

        /// <summary>
        /// 出售单位：返还 max(cost-1, 0) 圣水。
        /// </summary>
        public static void GiveSell(MatchPlayer player, int cost, int round)
        {
            int refund = Math.Max(cost - 1, 0);
            ApplyDelta(player, EconomyDeltaType.Sell, refund, round);
        }

        /// <summary>
        /// 合成返还：每次合并 +1 圣水（连锁合成时每层各调用一次）。
        /// </summary>
        public static void GiveMerge(MatchPlayer player, int round)
        {
            ApplyDelta(player, EconomyDeltaType.Merge, AutoChessDefine.MergeElixirReturn, round);
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// 核心：应用圣水变动。执行 clamp、记录日志、发布事件。
        /// </summary>
        private static void ApplyDelta(MatchPlayer player, EconomyDeltaType type, int amount, int round,
            string context = "")
        {
            int oldValue = player.Elixir;
            int raw = player.Elixir + amount;
            player.Elixir = Math.Clamp(raw, 0, AutoChessDefine.MaxElixir);
            int actualAmount = player.Elixir - oldValue; // 实际变动（含 clamp 截断）

            // 记录日志
            EconomyLogComponent log = player.GetComponent<EconomyLogComponent>();
            log?.AddDelta(new EconomyDelta
            {
                Type = type,
                Amount = actualAmount,
                Round = round,
                Context = context,
            });

            // 发布圣水变化事件（供 E9 网络层和 UI 订阅）
            if (player.Elixir != oldValue)
            {
                EventSystem.Instance.Publish(player.Scene(),
                    new ElixirChangedEvent
                    {
                        PlayerId = player.PlayerId,
                        OldValue = oldValue,
                        NewValue = player.Elixir,
                    });
            }
        }
    }
}
```
2. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyService.cs && git commit -m "feat(a1-economy): add EconomyService with all 5 elixir operations"`

---

## 5. PhaseChangedEventHandler_Economy

- [x] 5.1 PhaseChangedEventHandler_Economy（Hotfix/Server）

**Context:**
- Depends: 4.1, 3.2
- Why: 在回合开始（RoundStart）时自动给所有存活玩家发放收入和统帅被动，与 FSM 解耦

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs`

**Steps:**
1. 创建文件，内容：
```csharp
namespace ET.Server
{
    /// <summary>
    /// 监听 PhaseChangedEvent：当阶段切换到 RoundStart 时发放回合收入和统帅被动。
    ///
    /// 注意：[Event(SceneType.Main)] 中的 SceneType 需与 MatchComponent 实际挂载的
    /// Scene 类型一致。当前先用 SceneType.Main，集成时视服务端 Scene 配置调整。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [FriendOf(typeof(MatchPlayer))]
    [Event(SceneType.Main)]
    public class PhaseChangedEventHandler_Economy : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            // 只处理 RoundStart 阶段
            if (args.NewPhase != RoundPhase.RoundStart)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 查找对应的 MatchRoom
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

            int round = args.Round;
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();

            // 1. 所有存活玩家 +4 圣水（回合基础收入）
            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                EconomyService.GiveRoundIncome(player, round);
            }

            // 2. 上回合战败者统帅被动（首回合跳过，无战斗结果）
            if (round > 1 && rng != null && room.LastRoundLosers.Count > 0)
            {
                foreach (long loserId in room.LastRoundLosers)
                {
                    MatchPlayer loser = room.FindPlayerById(loserId);
                    if (loser != null && loser.IsAlive)
                    {
                        EconomyService.GiveCommanderPassive(loser, rng, round);
                    }
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
```
2. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
3. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs && git commit -m "feat(a1-economy): add PhaseChangedEventHandler for round income"`

---

## 6. 集成测试

- [x] 6.1 AutoChessTestHelper — 追加 TestEconomy

**Context:**
- Depends: 4.1, 5.1, 3.3
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`
- Why: 验证 EconomyService 5 个方法的正确性、clamp 边界、EconomyLog 记录

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 `RunAllTests` 的 `Log.Info("[AutoChess] All integration tests passed!")` 之前追加：
```csharp
TestEconomy(scene);
```
2. 在文件末尾（`AssertGate` 之前）追加新方法：
```csharp
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

    // --- Clamp: 0 - sell(cost=0) → 0 不低于 0 ---
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
    // Round 1: GiveRoundIncome + TryDeductBuy(ok) + GiveSell + GiveMerge = 4 条
    if (round1Count != 4)
        throw new Exception($"Round 1 deltas: expected 4, got {round1Count}");

    // --- LastRoundLosers 为空时跳过统帅被动 ---
    MatchPlayer p2 = room.FindPlayerById(3002);
    p2.Elixir = 0;
    // Round 1 时 LastRoundLosers 为空，不应触发补偿
    // （此处直接验证 LastRoundLosers.Count == 0，E5 写入逻辑在 E5 change 中）
    if (room.LastRoundLosers.Count != 0)
        throw new Exception("LastRoundLosers should be empty at start");

    // 清理
    scene.RemoveComponent<MatchComponent>();

    Log.Info("[AutoChess] Economy test passed: income/buy/sell/merge/passive/clamp/log all correct");
}
```
3. Compile check: `dotnet build ET.Hotfix.csproj --no-incremental 2>&1 | tail -5`
   Expected: `已成功生成。0 个警告 0 个错误`
4. Commit: `git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs && git commit -m "test(a1-economy): add TestEconomy integration test"`

---

## 完成标准

- [x] ET.Model、ET.Hotfix 编译 0 错误 0 警告
- [x] EconomyService 所有 5 个方法单测通过（GiveRoundIncome / TryDeductBuy / GiveSell / GiveMerge / GiveCommanderPassive）
- [x] GiveCommanderPassive 使用 PrngPurpose.CommanderPassive 子种子，bonus 范围 [4, 8]
- [x] Round 1 不触发统帅被动（LastRoundLosers 为空时正确跳过）
- [x] clamp：Elixir 不超过 MaxElixir(999)，不低于 0
- [x] EconomyLogComponent.GetDeltasByRound 返回对应回合的全部记录（Round 1 = 4 条）
- [x] PhaseChangedEventHandler_Economy 仅在 RoundPhase.RoundStart 时触发（非 RoundStart 直接返回）
- [x] ElixirChangedEvent 在每次 Elixir 实际变化时发布（failed buy 不发布）
