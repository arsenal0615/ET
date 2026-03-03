# a1-shop 实施计划

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 实现自走棋共享卡池（SharedPool）、商店（Shop）和首回合赠送（FirstRoundGift），为 E4 单位系统提供经济+卡池基础。

**Architecture:** SharedPoolComponent（MatchRoom）跟踪全局单位库存；ShopComponent（MatchPlayer）管理 3 个 Offer 槽；ShopService 作为静态服务处理生成/购买/出售；PhaseChangedEventHandler_Shop 在 RoundStart 刷新所有玩家商店。

**Tech Stack:** cn.etetet.autochess，Model/Server + Hotfix/Server，不引入任何新外部依赖

**Impact:**
- Modules/Assemblies: `cn.etetet.autochess`（Model.Server + Hotfix.Server）
- Code generation changes: No
- New data models: SharedPoolComponent (ComponentOf MatchRoom), ShopComponent (ComponentOf MatchPlayer), ShopOffer (plain C# class)
- New messages/protocols: None

**Rules:**
- ecs-patterns
- code-templates

**Design Ref:** docs/changes/a1-shop/design.md

---

## 1. 数据模型

- [ ] 1.1 ShopOffer 类 + SharedPoolComponent

**Context:**
- Why: 商店槽位的值类型 + 卡池容器；其他所有任务都依赖这两个数据结构

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/ShopOffer.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/SharedPoolComponent.cs`

**Steps:**
1. 新建 `ShopOffer.cs`：

```csharp
namespace ET.Server
{
    /// <summary>
    /// 商店单个槽位数据。TemplateId = -1 表示空位。
    /// 非 Entity，值语义数据类。
    /// </summary>
    public class ShopOffer
    {
        public int TemplateId; // -1 = 空位
    }
}
```

2. 新建 `SharedPoolComponent.cs`：

```csharp
namespace ET.Server
{
    /// <summary>
    /// 全局共享卡池，挂在 MatchRoom 上。
    /// Remaining[templateId - 1] = 该单位剩余份数。
    /// </summary>
    [ComponentOf(typeof(MatchRoom))]
    public class SharedPoolComponent : Entity, IAwake, IDestroy
    {
        public int[] Remaining; // length = AutoChessDefine.TotalUnitTemplates (24)
    }
}
```

3. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

4. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/ShopOffer.cs
git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/SharedPoolComponent.cs
git commit -m "feat(a1-shop): add ShopOffer and SharedPoolComponent data models"
```

---

- [ ] 1.2 ShopComponent

**Context:**
- Depends: 1.1
- Why: MatchPlayer 的商店状态容器，3 槽 + RollIndex + 首回合礼包结果

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/ShopComponent.cs`

**Steps:**
1. 新建 `ShopComponent.cs`：

```csharp
namespace ET.Server
{
    /// <summary>
    /// 玩家商店状态，挂在 MatchPlayer 上。
    /// Slots[0..2] = 三个商店槽位。
    /// ShopRollIndex 全局递增，确保每次刷新使用唯一子种子。
    /// FirstRoundGiftTemplateId = Round 1 赠送的单位模板ID（-1=未赠送）。
    /// </summary>
    [ComponentOf(typeof(MatchPlayer))]
    public class ShopComponent : Entity, IAwake, IDestroy
    {
        public ShopOffer[] Slots;       // length = AutoChessDefine.ShopSlotCount (3)
        public int ShopRollIndex;       // 全局递增，从不重置
        public int FirstRoundGiftTemplateId; // -1 = 无礼包
    }
}
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/ShopComponent.cs
git commit -m "feat(a1-shop): add ShopComponent data model"
```

---

- [ ] 1.3 MatchPlayer.PlayerIndex 字段

**Context:**
- Why: 首回合礼包使用 PlayerIndex（0-3）作为 PRNG 子种子，确保同局内各玩家礼包不同

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs`

**Steps:**
1. 在 MatchPlayer 末尾新增字段：

修改前：
```csharp
    public class MatchPlayer : Entity, IAwake<long>, IDestroy
    {
        public long PlayerId;
        public int Hp;
        public int Elixir;
        public int PopCap;
        public bool IsAlive;
        public int Rank; // 0=未结算, 1=第一名, 2=第二名...
    }
```

修改后：
```csharp
    public class MatchPlayer : Entity, IAwake<long>, IDestroy
    {
        public long PlayerId;
        public int Hp;
        public int Elixir;
        public int PopCap;
        public bool IsAlive;
        public int Rank;        // 0=未结算, 1=第一名, 2=第二名...
        public int PlayerIndex; // 0-based，工厂创建时按序赋值，用于 PRNG 子种子
    }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs
git commit -m "feat(a1-shop): add PlayerIndex to MatchPlayer for gift PRNG"
```

---

## 2. Component Systems

- [ ] 2.1 SharedPoolComponentSystem

**Context:**
- Depends: 1.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: Awake 初始化 24 种单位各 8 份库存，Destroy 释放数组

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/SharedPoolComponentSystem.cs`

**Steps:**
1. 新建文件：

```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(SharedPoolComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    public static partial class SharedPoolComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SharedPoolComponent self)
        {
            self.Remaining = new int[AutoChessDefine.TotalUnitTemplates];
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
                self.Remaining[i] = AutoChessDefine.CopiesPerUnit; // 8
        }

        [EntitySystem]
        private static void Destroy(this SharedPoolComponent self)
        {
            self.Remaining = null;
        }
    }
}
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/SharedPoolComponentSystem.cs
git commit -m "feat(a1-shop): add SharedPoolComponentSystem with pool initialization"
```

---

- [ ] 2.2 ShopComponentSystem

**Context:**
- Depends: 1.2
- Why: Awake 初始化 3 个空 ShopOffer 槽，Destroy 释放

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopComponentSystem.cs`

**Steps:**
1. 新建文件：

```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(ShopComponent))]
    [FriendOf(typeof(ShopComponent))]
    public static partial class ShopComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ShopComponent self)
        {
            self.Slots = new ShopOffer[AutoChessDefine.ShopSlotCount];
            for (int i = 0; i < AutoChessDefine.ShopSlotCount; i++)
                self.Slots[i] = new ShopOffer { TemplateId = -1 };
            self.ShopRollIndex = 0;
            self.FirstRoundGiftTemplateId = -1;
        }

        [EntitySystem]
        private static void Destroy(this ShopComponent self)
        {
            self.Slots = null;
        }
    }
}
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopComponentSystem.cs
git commit -m "feat(a1-shop): add ShopComponentSystem"
```

---

- [ ] 2.3 MatchPlayerSystem: PlayerIndex 初始化

**Context:**
- Depends: 1.3
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchPlayerSystem.cs`
- Why: Awake 须初始化 PlayerIndex = 0（工厂稍后会覆写为正确的值）

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchPlayerSystem.cs`

**Steps:**
1. 在 Awake 末尾添加初始化：

```csharp
        [EntitySystem]
        private static void Awake(this MatchPlayer self, long playerId)
        {
            self.PlayerId = playerId;
            self.Hp = AutoChessDefine.InitialHp;
            self.Elixir = 0;
            self.PopCap = AutoChessDefine.InitialPopCap;
            self.IsAlive = true;
            self.Rank = 0;
            self.PlayerIndex = 0; // 工厂创建时会覆写为 0-3
        }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchPlayerSystem.cs
git commit -m "feat(a1-shop): init PlayerIndex in MatchPlayerSystem.Awake"
```

---

## 3. ShopService

- [ ] 3.1 ShopService 核心：GenerateOffersForPlayer + ReturnOffersToPool

**Context:**
- Depends: 2.1, 2.2
- Why: 商店刷新的核心逻辑——预扣机制保证全局一致性

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`

**Steps:**
1. 新建 ShopService.cs，先写 GenerateOffersForPlayer 和 ReturnOffersToPool：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 商店服务：Offer 生成、购买、出售、淘汰回收。
    /// 所有圣水变更通过 EconomyService 执行。
    /// </summary>
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    [FriendOf(typeof(MatchPlayer))]
    public static class ShopService
    {
        /// <summary>
        /// 将当前所有未购买 Offer（非空槽）归还卡池，然后生成 3 个新 Offer。
        /// </summary>
        public static void GenerateOffersForPlayer(MatchPlayer player,
            SharedPoolComponent pool, DeterministicRngComponent rng)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();

            // 1. 归还当前所有预扣
            ReturnCurrentOffers(shop, pool);

            // 2. 收集可用模板（remaining > 0），按 templateId 升序
            List<int> candidates = new List<int>(AutoChessDefine.TotalUnitTemplates);
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
            {
                if (pool.Remaining[i] > 0)
                    candidates.Add(i + 1); // templateId = i + 1
            }

            // 3. 选 3 个不重复模板，用 DeriveSubSeed 确定性抽取
            for (int slot = 0; slot < AutoChessDefine.ShopSlotCount; slot++)
            {
                if (candidates.Count == 0)
                {
                    shop.Slots[slot].TemplateId = -1;
                    continue;
                }

                uint sub = rng.DeriveSubSeed(PrngPurpose.ShopReroll, shop.ShopRollIndex + slot);
                int idx = (int)(sub % (uint)candidates.Count);
                int templateId = candidates[idx];

                shop.Slots[slot].TemplateId = templateId;
                pool.Remaining[templateId - 1]--;  // 预扣
                candidates.RemoveAt(idx);
            }

            shop.ShopRollIndex += AutoChessDefine.ShopSlotCount;
        }

        /// <summary>
        /// 归还玩家当前 Offer 到卡池（淘汰/结算时调用）。
        /// </summary>
        public static void ReturnOffersToPool(MatchPlayer player, SharedPoolComponent pool)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();
            ReturnCurrentOffers(shop, pool);
        }

        // --- Private helpers ---

        private static void ReturnCurrentOffers(ShopComponent shop, SharedPoolComponent pool)
        {
            for (int i = 0; i < shop.Slots.Length; i++)
            {
                int tid = shop.Slots[i].TemplateId;
                if (tid > 0)
                {
                    pool.Remaining[tid - 1]++;
                    shop.Slots[i].TemplateId = -1;
                }
            }
        }
    }
}
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit:

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs
git commit -m "feat(a1-shop): add ShopService GenerateOffersForPlayer + ReturnOffersToPool"
```

---

- [ ] 3.2 ShopService.TryBuy

**Context:**
- Depends: 3.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/EconomyService.cs`
- Why: 购买校验圣水 + 预扣圆满 + 触发全行重刷

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`

**Steps:**
1. 在 ShopService 中追加 TryBuy 方法（放在 ReturnOffersToPool 之后，`// --- Private helpers ---` 之前）：

```csharp
        /// <summary>
        /// 购买商店指定槽位的单位。
        /// 成功：扣除圣水 + 全行重刷 + 返回 templateId。
        /// 失败：返回 -1，状态不变。
        /// 购买后 Offer 的库存归还时机：全行重刷时先 ReturnCurrentOffers 再生成新 Offer。
        /// 注：实际 UnitInstance 创建由 E4 负责，此处只返回 templateId。
        /// </summary>
        public static int TryBuy(MatchPlayer player, int slotIndex,
            SharedPoolComponent pool, DeterministicRngComponent rng,
            RoundPhase currentPhase, int round)
        {
            // 阶段门控
            if (!RoundFSMComponentSystem.CanOperate(currentPhase, OperationType.Buy))
                return -1;

            ShopComponent shop = player.GetComponent<ShopComponent>();

            // 槽位合法性
            if (slotIndex < 0 || slotIndex >= shop.Slots.Length)
                return -1;

            int templateId = shop.Slots[slotIndex].TemplateId;
            if (templateId <= 0)
                return -1; // 空槽

            // 获取单价
            int cost = AutoChessConfigLoader.GetUnit(templateId).Cost;

            // 扣除圣水（已购买的 Offer 份数之前已预扣，此处不需要再动 pool）
            bool ok = EconomyService.TryDeductBuy(player, cost, round);
            if (!ok)
                return -1;

            // 清空该槽（预扣份数已在生成时扣，不需要归还——该单位被买走了）
            shop.Slots[slotIndex].TemplateId = -1;

            // 全行重刷（归还剩余 2 槽的预扣 → 生成全新 3 槽）
            GenerateOffersForPlayer(player, pool, rng);

            return templateId;
        }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs
git commit -m "feat(a1-shop): add ShopService.TryBuy with phase gate + elixir check + re-roll"
```

---

- [ ] 3.3 ShopService.TrySell

**Context:**
- Depends: 3.2
- Why: 出售操作归还圣水 + 回流卡池（isGift 单位不回流）

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs`

**Steps:**
1. 在 ShopService 中追加 TrySell 方法（放在 TryBuy 之后，`// --- Private helpers ---` 之前）：

```csharp
        /// <summary>
        /// 出售单位：归还圣水 + 回流卡池（isGift 单位不回流）。
        /// copiesOfStar 公式：1★→1份 / 2★→2份 / 3★→4份（MergeCount 的幂）
        /// 注：实际 UnitInstance 移除由 E4 负责，此处只处理经济和卡池。
        /// </summary>
        public static void TrySell(MatchPlayer player, int templateId, int starLevel,
            bool isGift, SharedPoolComponent pool, int round)
        {
            int cost = AutoChessConfigLoader.GetUnit(templateId).Cost;

            // 圣水返还
            EconomyService.GiveSell(player, cost, round);

            // 卡池回流（礼品单位不回流）
            if (!isGift && templateId > 0 && templateId <= AutoChessDefine.TotalUnitTemplates)
            {
                int copies = 1 << (starLevel - 1); // 1★→1, 2★→2, 3★→4
                pool.Remaining[templateId - 1] += copies;
            }
        }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/ShopService.cs
git commit -m "feat(a1-shop): add ShopService.TrySell with pool refill and isGift guard"
```

---

## 4. FirstRoundGiftService

- [ ] 4.1 FirstRoundGiftService.SelectGiftTemplate

**Context:**
- Depends: 1.1, 2.2
- Why: Round 1 赠送 2 费单位，每名玩家用 PlayerIndex 区分，不扣卡池

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/FirstRoundGiftService.cs`

**Steps:**
1. 新建文件：

```csharp
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 首回合礼包服务：为每名玩家随机赠送 1 个 2 费单位。
    /// 不扣卡池（isGift=true）。实际 UnitInstance 创建由 E4 负责。
    /// 结果写入 ShopComponent.FirstRoundGiftTemplateId，供 E4 读取。
    /// </summary>
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(MatchPlayer))]
    public static class FirstRoundGiftService
    {
        /// <summary>
        /// 为玩家选取礼包模板（2 费单位，基于 PlayerIndex 确定性）。
        /// 结果写入 player.GetComponent&lt;ShopComponent&gt;().FirstRoundGiftTemplateId。
        /// </summary>
        public static void SelectGiftTemplate(MatchPlayer player, DeterministicRngComponent rng)
        {
            // 收集所有 2 费模板 ID
            List<int> cost2Templates = new List<int>();
            for (int id = 1; id <= AutoChessDefine.TotalUnitTemplates; id++)
            {
                if (AutoChessConfigLoader.GetUnit(id).Cost == 2)
                    cost2Templates.Add(id);
            }

            if (cost2Templates.Count == 0)
                return;

            uint sub = rng.DeriveSubSeed(PrngPurpose.FirstGift, player.PlayerIndex);
            int idx = (int)(sub % (uint)cost2Templates.Count);
            int templateId = cost2Templates[idx];

            ShopComponent shop = player.GetComponent<ShopComponent>();
            shop.FirstRoundGiftTemplateId = templateId;
            // 注意：不预扣卡池（isGift=true）
        }
    }
}
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/FirstRoundGiftService.cs
git commit -m "feat(a1-shop): add FirstRoundGiftService for deterministic 2-cost gift selection"
```

---

## 5. Factory + Room Integration

- [ ] 5.1 MatchRoomFactory：添加 SharedPoolComponent + ShopComponent

**Context:**
- Depends: 2.1, 2.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`
- Why: 对局创建时初始化卡池和每名玩家的商店

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`

**Steps:**
1. 修改 MatchRoomFactory.CreateMatch，在 EconomyLogComponent 初始化后添加 ShopComponent 和 SharedPoolComponent：

修改前的玩家初始化区域（`foreach (Entity child in room.Children.Values)` 块）替换为：

```csharp
        public static MatchRoom CreateMatch(MatchComponent matchComponent, List<long> playerIds, uint seed)
        {
            // 创建 MatchRoom
            MatchRoom room = matchComponent.CreateRoom();

            // 初始化（创建玩家等）
            room.Init(seed, playerIds);

            // 为每个玩家添加组件，并按创建顺序赋 PlayerIndex
            int playerIndex = 0;
            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer player)
                {
                    player.PlayerIndex = playerIndex++;
                    player.AddComponent<EconomyLogComponent>();
                    player.AddComponent<ShopComponent>();
                }
            }

            // 添加共享卡池（24种×8份）
            room.AddComponent<SharedPoolComponent>();

            // 添加 PRNG 组件
            room.AddComponent<DeterministicRngComponent, uint>(seed);

            // 添加回合状态机组件
            room.AddComponent<RoundFSMComponent>();

            return room;
        }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs
git commit -m "feat(a1-shop): add SharedPoolComponent and ShopComponent in MatchRoomFactory"
```

---

- [ ] 5.2 MatchRoomSystem.EliminatePlayer：淘汰时回收 Offer

**Context:**
- Depends: 3.1, 5.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`
- Why: 玩家淘汰时，其商店 Offer 预扣必须归还卡池，否则库存泄漏

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs`

**Steps:**
1. 在 EliminatePlayer 的 `break;` 之前添加 ReturnOffersToPool 调用：

修改 EliminatePlayer 方法（`player.IsAlive = false;` 行所在块）：

```csharp
        public static void EliminatePlayer(this MatchRoom self, long playerId)
        {
            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchPlayer player && player.PlayerId == playerId)
                {
                    player.IsAlive = false;
                    int aliveCount = self.GetAlivePlayerCount();
                    player.Rank = aliveCount + 1;
                    self.FinalResults.Add((playerId, player.Rank));

                    // 归还该玩家商店 Offer 的预扣份额
                    SharedPoolComponent pool = self.GetComponent<SharedPoolComponent>();
                    if (pool != null)
                        ShopService.ReturnOffersToPool(player, pool);

                    break;
                }
            }
        }
```

2. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

3. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomSystem.cs
git commit -m "feat(a1-shop): return player shop offers to pool on elimination"
```

---

## 6. Event Handler

- [ ] 6.1 PhaseChangedEventHandler_Shop

**Context:**
- Depends: 3.1, 4.1, 5.1
- Why: RoundStart 触发：刷新所有存活玩家的商店 Offer；Round 1 额外执行首回合礼包

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Shop.cs`

**Steps:**
1. 新建文件：

```csharp
namespace ET.Server
{
    /// <summary>
    /// RoundStart 时为所有存活玩家刷新商店 Offer。
    /// Round 1 额外执行首回合礼包（SelectGiftTemplate）。
    /// </summary>
    [Event(SceneType.Server)]
    public class PhaseChangedEventHandler_Shop : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            if (args.NewPhase != RoundPhase.RoundStart)
                return;

            MatchRoom room = args.Room;
            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();

            if (rng == null || pool == null)
                return;

            bool isFirstRound = (room.CurrentRound == 1);

            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                // 刷新商店 Offer（归还上回合预扣 → 生成本回合 Offer）
                ShopService.GenerateOffersForPlayer(player, pool, rng);

                // 首回合礼包
                if (isFirstRound)
                    FirstRoundGiftService.SelectGiftTemplate(player, rng);
            }

            await ETTask.CompletedTask;
        }
    }
}
```

2. 检查 PhaseChangedEvent 是否有 Room 字段，查看现有 PhaseChangedEventHandler_Economy.cs 中的事件用法：

```bash
grep -n "PhaseChangedEvent\|args\." Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs
```

如果 PhaseChangedEvent 没有 Room 字段，则通过 Scene 获取 MatchComponent：

```csharp
// 如 PhaseChangedEvent 没有 Room 字段，用此替代：
MatchComponent matchComp = scene.GetComponent<MatchComponent>();
// 然后通过 matchComp 找到目标 MatchRoom
```

3. 编译检查：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors（如有编译错误，根据实际 PhaseChangedEvent 结构调整）

4. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Shop.cs
git commit -m "feat(a1-shop): add PhaseChangedEventHandler_Shop for RoundStart offer refresh"
```

---

## 7. 集成测试

- [ ] 7.1 AutoChessTestHelper.TestShop

**Context:**
- Depends: 6.1（所有实现完成后）
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`
- Why: 集成验证卡池、商店、购买、出售、礼包的完整行为

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 RunAllTests 中添加 `TestShop(scene);` 调用：

```csharp
        public static void RunAllTests(Scene scene)
        {
            TestConfigLoader();
            TestPrngDeterminism(scene);
            TestEntityTree(scene);
            TestPhaseGate();
            TestEconomy(scene);
            TestShop(scene);
            Log.Info("[AutoChess] All integration tests passed!");
        }
```

2. 在文件末尾（`}` 前）添加 TestShop 方法：

```csharp
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
                    throw new Exception($"Pool init: template {i+1} expected {AutoChessDefine.CopiesPerUnit}, got {pool.Remaining[i]}");
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
```

3. 运行集成测试（编译后在服务端启动时手动调用）：

```
dotnet build ET.sln
```
Expected: Build succeeded, 0 errors

4. Commit：

```bash
git add Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs
git commit -m "feat(a1-shop): add TestShop integration test covering all shop capabilities"
```

---

## 完成标准

- [ ] 所有 13 个任务的 checkbox 已勾选
- [ ] `dotnet build ET.sln` → 0 errors, 0 warnings
- [ ] TestShop 所有断言通过（10 个验证点）
- [ ] TestEconomy 仍通过（回归验证）
- [ ] git log 显示每个任务有独立 commit
