# a1-synergy Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** 实现羁绊统计引擎、TraitSnapshot 快照、静态羁绊效果、Goblin 经济羁绊、部署阶段实时追踪。

**Architecture:** SynergyComponent 挂 MatchPlayer，SynergyService 纯静态服务类统计/生成快照，GoblinGiftService 处理经济羁绊。PhaseChangedEventHandler_Synergy 驱动阶段逻辑。静态效果写入 UnitBattleModifiers（不修改 UnitInfo），动态效果仅标记到快照供 E7 读取。

**Tech Stack:** ET9 ECS, C#, cn.etetet.autochess 包

**Impact:**
- Modules/Assemblies: cn.etetet.autochess (Model/Server, Model/Share, Hotfix/Server)
- Code generation changes: 否
- New data models: SynergyComponent (ComponentOf: MatchPlayer), SynergyEntry [EnableClass], TraitSnapshot [EnableClass], UnitBattleModifiers [EnableClass], SynergyType (enum)
- New messages/protocols: 无

**Rules:** ecs-patterns, code-templates, architecture

**Design Ref:** docs/changes/a1-synergy/design.md

---

## 1. 数据模型与配置

- [x] 1.1 创建 SynergyType 枚举和 SynergyChangedEvent

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Events/ElixirChangedEvent.cs`, `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Defs/SynergyDef.cs`
- Why: 定义羁绊分类枚举和事件，供后续所有任务使用

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Defs/SynergyType.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Events/SynergyChangedEvent.cs`
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/Defs/SynergyDef.cs`

**Steps:**
1. 创建 `SynergyType` 枚举（namespace ET）：Static=1, Dynamic=2, Economic=3
2. 创建 `SynergyChangedEvent` 结构体（namespace ET）：字段 `MatchPlayerId` (long)
3. 修改 `SynergyDef` 添加 `public SynergyType Type;` 字段
4. 编译检查：`dotnet build Packages/cn.etetet.autochess/`
5. 提交

- [x] 1.2 创建 SynergyEntry、UnitBattleModifiers、TraitSnapshot 数据类

**Context:**
- Depends: 1.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/UnitInfo.cs`
- Why: 定义羁绊统计条目、战斗属性修改值和冻结快照的数据容器

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/SynergyEntry.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/UnitBattleModifiers.cs`
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/TraitSnapshot.cs`

**Steps:**
1. 创建 `SynergyEntry`（namespace ET.Server, `[EnableClass]`）：字段 Tag (string), Count (int), Level (int), Type (SynergyType)
2. 创建 `UnitBattleModifiers`（namespace ET.Server, `[EnableClass]`）：字段 HpMultiplier=1.0f, DamageReduction=0f, DamageMultiplier=1.0f, AtkSpeedMultiplier=1.0f, RangeBonus=0, DistanceDamagePerHex=0f
3. 创建 `TraitSnapshot`（namespace ET.Server, `[EnableClass]`）：字段 ActiveSynergies (List\<SynergyEntry\>), UnitSynergyMap (Dictionary\<int, List\<SynergyEntry\>\>), UnitModifiers (Dictionary\<int, UnitBattleModifiers\>)
4. 编译检查
5. 提交

- [x] 1.3 创建 SynergyComponent

**Context:**
- Depends: 1.2
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/MatchPlayer.cs`
- Why: 挂 MatchPlayer 的 Entity Component，持有实时统计、快照和跨回合 Goblin 状态

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Model/Server/AutoChess/SynergyComponent.cs`

**Steps:**
1. 创建 `SynergyComponent`（namespace ET.Server, `[ComponentOf(typeof(MatchPlayer))]`）：
   - `public List<SynergyEntry> LiveSynergies;`
   - `public TraitSnapshot Snapshot;`
   - `public int LastGoblinLevel;`
   - 实现 `IAwake, IDestroy`
2. 编译检查
3. 提交

- [x] 1.4 校准 SynergyDef 参数并设置 SynergyType

**Context:**
- Depends: 1.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Share/AutoChess/AutoChessConfigLoader.cs`, `docs/changes/a1-synergy/design.md`（决策 6 参数表）
- Why: 当前 ConfigLoader 中的 SynergyDef 参数是占位数据，与 GDD 不一致，需要按 design.md 决策 6 的表格校准

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Share/AutoChess/AutoChessConfigLoader.cs`（InitSynergies 方法）

**Steps:**
1. 修改 InitSynergies，为每个 SynergyDef 设置正确的 `Type` 字段和 Effects 参数：
   - Brawler: Type=Static, Effects[0].Param1=0.5f, Effects[1].Param1=1.0f
   - Giant: Type=Static, Effects[0].Param1=0.4f, Effects[1].Param1=0.4f
   - Noble: Type=Static, Effects[0]={Param1=0.25f, Param2=1f}, Effects[1]={Param1=0.40f, Param2=1f}
   - Blaster: Type=Static, Effects[0]={Param1=0.10f, Param2=1f}, Effects[1]={Param1=0.15f, Param2=1f}
   - Brutalist: Type=Static（仅攻速部分）, Effects[0].Param1=0.30f, Effects[1].Param1=0.60f
   - Ace: Type=Dynamic, Effects[0]={Param1=0.40f, Param2=0.40f}, Effects[1]={Param1=0.70f, Param2=0.70f}
   - Assassin: Type=Dynamic, Effects[0]={Param1=0.30f, Param2=1f}, Effects[1]={Param1=0.60f, Param2=1f}
   - Clan: Type=Dynamic, Effects[0]={Param1=0.35f, Param2=0.35f, Param3=5.0f}, Effects[1]={Param1=0.70f, Param2=0.70f, Param3=5.0f}
   - Ranger: Type=Dynamic, Effects[0]={Param1=0.10f, Param2=5f}, Effects[1]={Param1=0.15f, Param2=5f}
   - P.E.K.K.A: Type=Dynamic, Effects[0]={Param1=0.60f, Param2=0.40f}, Effects[1]={Param1=0.60f, Param2=0.40f}
   - Superstar: Type=Dynamic, Effects[0]={Param1=0.333f, Param2=0.50f, Param3=4f}, Effects[1]={Param1=0.667f, Param2=0.50f, Param3=4f}
   - Undead: Type=Dynamic, Effects[0].Param1=0.25f, Effects[1].Param1=0.50f
   - Goblin: Type=Economic, Effects[0].Param1=1f, Effects[1].Param1=2f
2. 更新 Description 字段与 GDD 一致
3. 编译检查
4. 提交

- [x] 1.5 添加 FrontRowMax 常量

**Context:**
- Reads: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`
- Why: Noble 前排/后排分界线需要常量化（Row 0-1=前排，Row 2-4=后排）

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/AutoChessDefine.cs`

**Steps:**
1. 添加 `public const int FrontRowMax = 1;`
2. 编译检查
3. 提交

---

## 2. 核心逻辑：SynergyService

- [x] 2.1 实现 SynergyService.Recalculate

**Context:**
- Depends: 1.3, 1.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/RosterService.cs`
- Why: 遍历棋盘单位统计标签 count，按阈值计算 level，更新 LiveSynergies，有变化则发布事件

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/SynergyService.cs`

**Steps:**
1. 创建 `SynergyService`（namespace ET.Server, static class）
   - `[FriendOf(typeof(SynergyComponent))]`
   - `[FriendOf(typeof(RosterComponent))]`
2. 实现 `public static void Recalculate(MatchPlayer player)`：
   - 获取 RosterComponent 和 SynergyComponent
   - 用 Dictionary\<string, int\> tagCounts 遍历 roster.Units where Row >= 0，累加每个 UnitTemplateDef.Tags
   - 遍历所有 13 个 SynergyDef（用 AutoChessConfigLoader），计算 level：count < thresholds[0] → 0, < thresholds[1] → 1, else → 2
   - 构建新的 List\<SynergyEntry\>（只包含 count > 0 的条目）
   - 对比 synergy.LiveSynergies 是否有变化（比较 count/level）
   - 有变化则更新 LiveSynergies 并发布 SynergyChangedEvent
3. 编译检查
4. 提交

- [x] 2.2 实现 SynergyService.GenerateSnapshot

**Context:**
- Depends: 2.1
- Why: PreBattle 时基于 LiveSynergies 生成冻结快照，计算静态效果写入 UnitBattleModifiers，标记动态效果，更新 LastGoblinLevel

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/SynergyService.cs`

**Steps:**
1. 实现 `public static void GenerateSnapshot(MatchPlayer player)`：
   - 先调 Recalculate(player) 确保 LiveSynergies 最新
   - 创建 TraitSnapshot，复制 ActiveSynergies（仅 level > 0）
   - 遍历棋盘单位，为每个 instId 构建 UnitSynergyMap：查该单位的 tags 有哪些在 ActiveSynergies 中
   - 遍历 UnitSynergyMap，对每个单位调 ApplyStaticEffects 生成 UnitBattleModifiers
   - 更新 LastGoblinLevel = ActiveSynergies 中 Goblin 条目的 level（无则 0）
   - 赋值 SynergyComponent.Snapshot = snapshot
2. 实现 `private static void ApplyStaticEffects(UnitInfo unit, List<SynergyEntry> unitSynergies, UnitBattleModifiers mods)`：
   - 遍历 unitSynergies，仅处理 Type==Static 的条目
   - 按 Tag 分发：
     - "Brawler": mods.HpMultiplier *= (1f + effect.Param1)
     - "Giant": mods.DamageReduction = Math.Max(mods.DamageReduction, effect.Param1)
     - "Noble": if (unit.Row <= AutoChessDefine.FrontRowMax) mods.DamageReduction = Math.Max(mods.DamageReduction, effect.Param1); else mods.DamageMultiplier *= (1f + effect.Param1)
     - "Blaster": mods.RangeBonus += (int)effect.Param2; mods.DistanceDamagePerHex = effect.Param1
     - "Brutalist": mods.AtkSpeedMultiplier *= (1f - effect.Param1)
   - effect = SynergyDef.Effects[entry.Level - 1]
3. 编译检查
4. 提交

---

## 3. Goblin 经济羁绊

- [x] 3.1 实现 GoblinGiftService

**Context:**
- Depends: 1.3, 1.4
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/FirstRoundGiftService.cs`, `Packages/cn.etetet.autochess/Scripts/Model/Share/AutoChess/PrngPurpose.cs`
- Why: RoundStart 时根据上一回合 Goblin 激活等级赠送哥布林单位到板凳（isGift=true，不扣卡池）

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/GoblinGiftService.cs`

**Steps:**
1. 创建 `GoblinGiftService`（namespace ET.Server, static class）
   - `[FriendOf(typeof(SynergyComponent))]`
   - `[FriendOf(typeof(RosterComponent))]`
2. 实现 `public static int TryGiveGifts(MatchPlayer player, DeterministicRngComponent rng, int round)`：
   - 读 SynergyComponent.LastGoblinLevel；== 0 → return 0
   - 从 AutoChessConfigLoader 获取 Goblin SynergyDef
   - giftCount = (int)def.Effects[level - 1].Param1
   - 收集所有 Goblin 标签的 UnitTemplateDef Id 列表（遍历 1-24，检查 Tags 包含 "Goblin"）
   - 循环 giftCount 次：
     - 检查 RosterService.GetBenchUsed(player) >= BenchSize → break
     - subSeed = rng.DeriveSubSeed(PrngPurpose.GoblinGift, round * 10 + i)
     - 用 subSeed 从 goblin 模板列表中选一个
     - RosterService.AddToBench(player, templateId, 1, isGift: true)
   - return 实际赠送数
3. 编译检查
4. 提交

---

## 4. 阶段事件处理器

- [ ] 4.1 创建 PhaseChangedEventHandler_Synergy

**Context:**
- Depends: 2.2, 3.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs`
- Why: 在 Deployment/PreBattle/RoundStart 阶段切换时驱动羁绊统计、快照生成和 Goblin 赠送

**Files:**
- Create: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PhaseChangedEventHandler_Synergy.cs`

**Steps:**
1. 创建 Handler（namespace ET.Server）：
   - `[Event(SceneType.Map)]`（重要：autochess 在 Map Scene 运行，不是 Server/Main）
   - 继承 `AEvent<Scene, PhaseChangedEvent>`
2. 实现 `RunAsync`：
   - 获取 MatchComponent → FindRoom(evt.MatchRoomId) → GetAlivePlayers()
   - `if (evt.NewPhase == RoundPhase.Deployment)`: 对所有存活玩家调 SynergyService.Recalculate
   - `if (evt.NewPhase == RoundPhase.PreBattle)`: 对所有存活玩家调 SynergyService.GenerateSnapshot
   - `if (evt.NewPhase == RoundPhase.RoundStart && evt.Round >= 2)`: 获取 room 的 DeterministicRngComponent，对所有存活玩家调 GoblinGiftService.TryGiveGifts
3. 编译检查
4. 提交

---

## 5. 集成：工厂 + 实时追踪

- [x] 5.1 MatchRoomFactory 添加 SynergyComponent

**Context:**
- Depends: 1.3
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`
- Why: 创建 MatchPlayer 时挂载 SynergyComponent，确保生命周期一致

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/MatchRoomFactory.cs`

**Steps:**
1. 在 `CreateMatch` 方法中，`player.AddComponent<RosterComponent>();` 之后添加 `player.AddComponent<SynergyComponent>();`
2. 编译检查
3. 提交

- [x] 5.2 PlacementService 集成 Recalculate

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PlacementService.cs`
- Why: 棋盘变化时实时重新统计羁绊（设计决策 7）

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/PlacementService.cs`

**Steps:**
1. `TryPlaceToBoard`：在 `return true;` 前添加 `SynergyService.Recalculate(player);`
2. `TryMoveOnBoard`：在 `return true;` 前添加 `SynergyService.Recalculate(player);`（Noble 位置依赖）
3. `TrySwap`：在 `return true;` 前添加：
   - 检查 u1 或 u2 的 Row 是否有一个 >= 0（涉及棋盘），有则调 `SynergyService.Recalculate(player);`
4. 编译检查
5. 提交

- [x] 5.3 UnitService 集成 Recalculate

**Context:**
- Depends: 2.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/UnitService.cs`
- Why: 出售棋盘单位和购买后合成导致棋盘变化时重新统计羁绊

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/UnitService.cs`

**Steps:**
1. `Sell` 方法：在调用后（return 前），检查被出售单位是否在棋盘上（unit.Row >= 0，注意要在 Remove 之前保存 row 值），若是则调 `SynergyService.Recalculate(player);`
2. `Buy` 方法：在 `MergeService.RunMergeChain` 之后，检查是否有棋盘单位变化（合成可能导致板凳单位升级到棋盘），调 `SynergyService.Recalculate(player);`（简单起见：Deployment 阶段购买成功后统一调一次 Recalculate）
3. 编译检查
4. 提交

---

## 6. 集成测试

- [ ] 6.1 添加羁绊系统集成测试

**Context:**
- Depends: 5.1, 5.2, 5.3, 4.1
- Reads: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`
- Why: 验证统计引擎、快照生成、静态效果、Goblin 赠送、实时追踪全链路

**Files:**
- Modify: `Packages/cn.etetet.autochess/Scripts/Hotfix/Server/AutoChess/AutoChessTestHelper.cs`

**Steps:**
1. 在 `RunAllTests` 方法中添加调用：`TestSynergyCounting(scene);` `TestSynergySnapshot(scene);` `TestGoblinGift(scene);`
2. 添加 `[FriendOf(typeof(SynergyComponent))]` 到类声明
3. 实现 `TestSynergyCounting(Scene scene)`：
   - 创建 MatchRoom（4 个玩家，用 MatchRoomFactory）
   - 取一个玩家，手动添加棋盘单位（通过 RosterService.AddToBench + 手动设 Row>=0）：
     - 迷你皮卡(Id=1, P.E.K.K.A/Brutalist) 在棋盘
     - 皮卡超人(Id=10, P.E.K.K.A/Brawler) 在棋盘
     - 野蛮人(Id=4, Clan/Brawler) 在板凳
   - 调 SynergyService.Recalculate(player)
   - 验证：P.E.K.K.A count=2 level=1, Brutalist count=1 level=0, Brawler count=1 level=0
   - 将野蛮人移到棋盘 → Recalculate → Brawler count=2 level=1
4. 实现 `TestSynergySnapshot(Scene scene)`：
   - 创建 MatchRoom，为玩家添加 2 个 Brawler 到棋盘
   - 调 SynergyService.GenerateSnapshot(player)
   - 验证 Snapshot 非 null
   - 验证 Snapshot.ActiveSynergies 包含 Brawler level=1
   - 验证 UnitModifiers 中 Brawler 单位的 HpMultiplier == 1.5f
5. 实现 `TestGoblinGift(Scene scene)`：
   - 创建 MatchRoom
   - 手动设置 SynergyComponent.LastGoblinLevel = 1
   - 调 GoblinGiftService.TryGiveGifts(player, rng, round=2)
   - 验证板凳多了 1 个 isGift=true 的单位
   - 验证单位模板有 Goblin 标签
6. 编译检查：`dotnet build Packages/cn.etetet.autochess/`
7. 提交

---

## 7. 收尾

- [ ] 7.1 全量编译验证

**Context:**
- Depends: 6.1
- Why: 确保所有变更编译通过，无框架规则违反

**Steps:**
1. 运行 `dotnet build ET.sln` 确认 0 errors
2. 检查无 ET 分析器警告（ET0001-ET0032）
3. 提交所有未提交的变更
