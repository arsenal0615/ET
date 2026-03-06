# 实施计划: battle-system

> **Change:** docs/changes/battle-system/
> **Design:** design.md (8 个技术决策)
> **Specs:** 10 capabilities, ~30 requirements
> **Package:** Packages/cn.etetet.autochess/Scripts/

## 阶段 1: 数据模型

### ✅ Task 1.1: CombatEvent 和 CombatEventType ✅
- **What:** 创建 CombatEvent 纯 C# 类和 CombatEventType 枚举
- **Where:** `Model/Share/AutoChess/Combat/CombatEventType.cs`, `Model/Share/AutoChess/Combat/CombatEvent.cs`
- **Context:**
  - Depends: 无
  - Reads: design.md TD-2, specs/combat-events/spec.md
  - Why: 所有后续任务生成的事件都用这个类型
- **Steps:**
  1. 创建 `CombatEventType` 枚举: RoundStart=0, Spawn=1, Move=2, Attack=3, Cast=4, Damage=5, Heal=6, BuffAdd=7, BuffRemove=8, Death=9, FrenzyStart=10, RoundEnd=11
  2. 创建 `CombatEvent` 类 `[EnableClass]`: I(序号), Tick, EventType, SourceInstId, TargetInstId, Amount, HpAfter, IsCrit, Col, Row, TemplateId, Star, Side, MaxHp, BuffType, Winner(0=L,1=R,2=Draw), TimeUp, LeftAliveEffective, RightAliveEffective, SkillDefId, DamageType(0=Normal,1=Skill,2=Dot)
  3. `dotnet build Unity/Assets/Scripts/Game/ET.AutoChess.Model.Share.csproj` 验证编译
- **Done:** 两个文件编译通过

### ✅ Task 1.2: CombatResult 和 PairingResult
- **What:** 创建战斗结果和配对结果数据类
- **Where:** `Model/Share/AutoChess/Combat/CombatResult.cs`, `Model/Server/AutoChess/PairingResult.cs`
- **Context:**
  - Depends: Task 1.1 (CombatEvent)
  - Reads: design.md TD-3/TD-4, specs/round-pairing/spec.md
- **Steps:**
  1. 创建 `CombatResult` `[EnableClass]` in Model/Share, namespace ET: Winner(int: 0=L,1=R,2=Draw), TimeUp(bool), Events(List<CombatEvent>), LeftAliveEffective(int), RightAliveEffective(int)
  2. 创建 `PairingResult` `[EnableClass]` in Model/Server, namespace ET.Server: LeftPlayerId(long), RightPlayerId(long), IsGhostMatch(bool), GhostSide(int: 0=L,1=R, 默认-1)
  3. 编译验证

### ✅ Task 1.3: GhostSnapshot 和 MatchRoom 扩展
- **What:** 创建 Ghost 快照类，扩展 MatchRoom 字段
- **Where:** `Model/Server/AutoChess/GhostSnapshot.cs`, 编辑 `Model/Server/AutoChess/MatchRoom.cs`
- **Context:**
  - Depends: 无
  - Reads: design.md TD-7, specs/round-pairing/spec.md (Ghost 定义)
- **Steps:**
  1. 创建 `GhostSnapshot` `[EnableClass]` in Model/Server, namespace ET.Server: Units(List<UnitInfo>), Snapshot(TraitSnapshot), PlayerId(long)
  2. 编辑 MatchRoom 新增: `public GhostSnapshot LastGhost;`
  3. 编辑 MatchRoomSystem.Awake 初始化 `LastGhost = null`，Destroy 清理
  4. 编译验证

### ✅ Task 1.4: AutoChessDefine 战斗常量
- **What:** 在 AutoChessDefine 中添加缺失的战斗常量
- **Where:** 编辑 `Model/Share/AutoChess/AutoChessDefine.cs`
- **Context:**
  - Depends: 无
  - Reads: specs/combat-tick-loop, specs/combat-frenzy, specs/combat-attack
- **Steps:**
  1. 新增常量: MaxCombatTicks=600, FrenzyStartTick=400, FrenzyAtkSpeedMultiplier=2.0f, FrenzyInvisDurationMultiplier=0.5f, DrawHpLoss=1, DefaultCritMultiplier=1.5f, StarMultipliers=new[]{0,1,2,4,8} (index=star)
  2. 编译验证

---

## 阶段 2: 战斗初始化

### ✅ Task 2.1: CombatUnitInit — UnitInfo 转 CombatUnitState
- **What:** 创建 CombatUnitInit 静态类，将玩家的棋盘单位转为战斗状态
- **Where:** `Hotfix/Server/AutoChess/Battle/CombatUnitInit.cs`
- **Context:**
  - Depends: Task 1.4 (常量)
  - Reads: CombatUnitState.cs, UnitInfo.cs, TraitSnapshot.cs, UnitBattleModifiers.cs, UnitTemplateDef.cs, specs/combat-unit-init/spec.md
  - Why: 战斗模拟的第一步，所有后续任务依赖正确的初始化
- **Steps:**
  1. 创建 `CombatUnitInit` 静态类，namespace ET.Server
  2. 方法 `InitSide(RosterComponent roster, TraitSnapshot snapshot, int side, List<CombatUnitState> outUnits)`:
     - 遍历 roster.Units，过滤 Row>=0（仅棋盘单位）
     - 获取 UnitTemplateDef 模板
     - 计算星级倍率: starMul = AutoChessDefine.StarMultipliers[star]
     - 创建 CombatUnitState，设置基础属性 × starMul
     - 如果 side==1（R方），y 镜像: Row = 4 - Row
     - 应用 UnitBattleModifiers（从 snapshot.UnitModifiers 查找）
     - HpMultiplier 应用到 MaxHp 和 Hp
     - Mana = ManaPrecharge
     - 初始化空 Buffs 列表和 TriggerState
  3. `[FriendOf(typeof(RosterComponent))]`
  4. 编译验证 + 基本初始化测试

---

## 阶段 3: 核心 Tick 循环

### ✅ Task 3.1: CombatSimulator 骨架 — Tick 循环 + 移动 + 普攻
- **What:** 实现 CombatSimulator 的主 Tick 循环，包含移动和普攻
- **Where:** `Hotfix/Server/AutoChess/Battle/CombatSimulator.cs`
- **Context:**
  - Depends: Task 2.1 (CombatUnitInit)
  - Reads: specs/combat-tick-loop, specs/combat-movement, specs/combat-attack, HexUtil.cs, ManaService.cs
  - Why: 这是战斗系统的核心
- **Steps:**
  1. 创建 `CombatSimulator` 静态类, namespace ET.Server
  2. 主入口 `RunCombat(List<CombatUnitState> leftUnits, List<CombatUnitState> rightUnits, DeterministicRngComponent rng, int round) → CombatResult`
  3. 合并 leftUnits + rightUnits 为 allUnits
  4. Tick 循环 (tick=0 to MaxCombatTicks-1):
     a. TickBuffs（遍历 allUnits 调用 SkillExecutor.TickBuffs）
     b. 单位行动（按 instId 升序）:
        - 跳过 !IsAlive 或 IsStunned
        - 查找射程内最近敌人（Target selection: 距离→仇恨→instId）
        - 有目标: 检查攻击冷却 → 执行普攻（damage formula + crit + DamageReduction + Blaster distance bonus）→ ManaService.GainOnAttack/GainOnHit → 检查 ManaFull 触发技能
        - 无目标: 检查移动冷却 → 贪心移动一格
        - 记录 CombatEvent
     c. 死亡清理（HP<=0 标记 !IsAlive，收集 DEATH 事件按 instId 排序）
     d. 检查胜负
     e. Frenzy 检测（tick>=400，修改攻速）
  5. 构建 CombatResult 返回
  6. 私有辅助方法: FindTarget, TryAttack, TryMove, CollectDeaths, CheckWinCondition
  7. 编译验证

### ✅ Task 3.2: 移动逻辑细化 — 攻击间隔和移动间隔跟踪
- **What:** 在 CombatUnitState 上添加 tick 跟踪字段，完善移动和攻击冷却
- **Where:** 编辑 `Model/Share/AutoChess/Combat/CombatUnitState.cs`, 编辑 `CombatSimulator.cs`
- **Context:**
  - Depends: Task 3.1
  - Reads: specs/combat-movement (moveSpeed interval), specs/combat-attack (atkSpeed interval)
- **Steps:**
  1. CombatUnitState 新增: LastAttackTick(int, init=-999), LastMoveTick(int, init=-999), AttackTargetInstId(int, -1=无，用于仇恨)
  2. CombatSimulator 中:
     - attackIntervalTicks = (int)Math.Ceiling(1f / (unit.AtkSpeed * unit.AtkSpeedMultiplier) * CombatTickRate)
     - moveIntervalTicks = (int)Math.Ceiling(1f / unit.MoveSpeed * CombatTickRate)
     - 冷却检查: tick - LastAttackTick >= attackIntervalTicks
  3. 编译验证

---

## 阶段 4: 开战特殊处理

### ✅ Task 4.1: CombatStartProcessor — 刺客跳后排 + 亡灵诅咒
- **What:** 实现 tick=0 的刺客跳后排和亡灵诅咒
- **Where:** `Hotfix/Server/AutoChess/Battle/CombatStartProcessor.cs`
- **Context:**
  - Depends: Task 3.1 (CombatSimulator 骨架)
  - Reads: specs/combat-start-effects, SynergyDef.cs (Assassin/Undead 配置), HexUtil.cs
  - Why: 刺客和亡灵是开战最复杂的特殊处理
- **Steps:**
  1. 创建 `CombatStartProcessor` 静态类, namespace ET.Server
  2. `ProcessCombatStart(List<CombatUnitState> allUnits, TraitSnapshot leftSnapshot, TraitSnapshot rightSnapshot, List<CombatEvent> events, ref int eventIndex)`
  3. 刺客跳后排（按 Side 分别处理）:
     - 找该方所有 Assassin 单位（有 Assassin synergy 的），按 instId 升序
     - 对每个刺客:
       a. 敌方后排: side==0 则敌方后排 row∈{0,1}，side==1 则敌方后排 row∈{3,4}
       b. 选最近的后排敌人（distance→instId）
       c. 计算落点（target 方向+1/-1 的位置）
       d. 落点被占→同行找空→6邻居找空→放弃
       e. 修改 Col/Row，生成 MOVE 事件
  4. 亡灵诅咒（刺客跳后）:
     - 检查每方 Undead level
     - 按 maxHp DESC → instId ASC 选目标
     - 应用 maxHp 乘数（0.75 or 0.50），Hp = min(Hp, MaxHp)
     - 生成 BUFF_ADD 事件
     - 在 CombatUnitState 上新增 `bool IsCursedByUndead` 标记
  5. 在 CombatSimulator.RunCombat 的 tick=0 前调用
  6. 编译验证

---

## 阶段 5: 动态羁绊

### ✅ Task 5.1: 动态羁绊效果集成
- **What:** 在 CombatSimulator 中实现 Ace/Clan/P.E.K.K.A/Ranger/Undead 动态效果
- **Where:** 编辑 `Hotfix/Server/AutoChess/Battle/CombatSimulator.cs`
- **Context:**
  - Depends: Task 3.1, Task 4.1
  - Reads: specs/combat-dynamic-traits, SynergyDef.cs
- **Steps:**
  1. CombatUnitState 新增: `bool ClanTriggered`(Clan 已触发), `int RangerStacks`(攻速叠层), `bool PekkaKillBonusApplied`(击杀增伤已生效), `int AceLifestealPct`(吸血百分比)
  2. **Ace 队长**: 初始化时选 captain（star DESC → cost DESC → instId ASC），设置 DamageMultiplier += bonus, AceLifestealPct
  3. **Ranger 叠加**: 每次普攻后 RangerStacks++（上限检查），AtkSpeedMultiplier 动态更新
  4. **Clan 低血爆发**: 受伤后检查 Hp <= MaxHp×50% && !ClanTriggered → 治疗 + SpeedBuff + 标记
  5. **P.E.K.K.A 击杀**: 击杀时检查 → 回血 + DamageMultiplier += 0.40
  6. **Undead 击杀增伤**: 击杀被诅咒目标 → 该方所有 Undead 单位 DamageMultiplier += 0.30
  7. **Ace 吸血**: 造成伤害后 Hp += damage × lifestealPct（cap MaxHp）
  8. 每个效果生成对应 CombatEvent（HEAL/BUFF_ADD）
  9. 编译验证

---

## 阶段 6: 事件流 + Frenzy + 技能集成

### ✅ Task 6.1: 完善事件流生成 + Frenzy + 技能触发集成
- **What:** 确保 CombatSimulator 生成完整的 CombatEvent 流，集成 Frenzy 和技能触发
- **Where:** 编辑 `CombatSimulator.cs`
- **Context:**
  - Depends: Task 5.1
  - Reads: specs/combat-events, specs/combat-frenzy, SkillExecutor.cs
- **Steps:**
  1. 事件流收集: 用 List<CombatEvent>，每个事件分配递增 I
  2. ROUND_START 事件（tick=0 最前）
  3. SPAWN 事件（tick=0，每个单位）
  4. MOVE 事件（移动时）
  5. ATTACK 事件（普攻时）
  6. 技能触发点:
     - 普攻命中后: AttackTrait trigger
     - 普攻命中后: 检查 ManaFull → ManaFull trigger → SkillExecutor.TryExecute
     - 每 tick: Interval trigger
     - 击杀后: OnKill trigger
     - 受伤后: OnHpBelow trigger
     - tick=0: CombatStart trigger
     - 死亡时: OnDeath trigger
  7. SkillExecutionResult → CAST + DAMAGE/HEAL/BUFF 事件转换
  8. Frenzy: tick>=FrenzyStartTick 时，一次性标记 isFrenzy=true，所有单位 AtkSpeedMultiplier *= 2，后续隐身 duration *= 0.5，生成 FRENZY_START 事件
  9. ROUND_END 事件（最后）
  10. 同 tick 内事件排序: MOVE < ATTACK < CAST < DAMAGE/HEAL/BUFF < DEATH
  11. 编译验证

---

## 阶段 7: 配对与结算

### ✅ Task 7.1: PairingService — 确定性配对 + Ghost
- **What:** 实现配对算法和 Ghost 选取
- **Where:** `Hotfix/Server/AutoChess/Battle/PairingService.cs`
- **Context:**
  - Depends: Task 1.2 (PairingResult), Task 1.3 (GhostSnapshot)
  - Reads: specs/round-pairing, DeterministicRngComponent.cs
- **Steps:**
  1. 创建 `PairingService` 静态类, namespace ET.Server
  2. `MakePairings(MatchRoom room, DeterministicRngComponent rng) → List<PairingResult>`:
     - 收集存活玩家 PlayerId 列表
     - pairSeed = rng.DeriveSubSeed(PrngPurpose.Pairing, room.CurrentRound)
     - Fisher-Yates 洗牌
     - 4人: 2对; 3人: 1对+1个 vs Ghost; 2人: 固定对打
     - 每对中 playerId 小者为 L
  3. Ghost 选取: room.LastGhost（MatchRoom 在淘汰时记录）
  4. `[FriendOf(typeof(MatchRoom))]`
  5. 编译验证

### ✅ Task 7.2: SettlementService — 扣血 + 淘汰 + 排名
- **What:** 实现回合结算
- **Where:** `Hotfix/Server/AutoChess/Battle/SettlementService.cs`
- **Context:**
  - Depends: Task 7.1, Task 1.2 (CombatResult)
  - Reads: specs/round-settlement, MatchRoomSystem.cs (EliminatePlayer)
- **Steps:**
  1. 创建 `SettlementService` 静态类, namespace ET.Server
  2. `ProcessResults(MatchRoom room, List<(PairingResult Pairing, CombatResult Result)> battleResults)`:
     - 清空 LastRoundLosers
     - 对每场战斗:
       a. 非平局: 败方扣血 = aliveEffective + 1
       b. 平局: 双方各扣 DrawHpLoss=1
       c. Ghost 对手: 赢不扣，输正常扣
     - 收集 HP<=0 的玩家
     - 同回合多人淘汰排名 Tiebreaker: hp DESC → hpBefore DESC → aliveEffective DESC → playerId ASC
     - 调用 EliminatePlayer
     - 记录最近淘汰者的 GhostSnapshot
     - 写入 LastRoundLosers
  3. `[FriendOf(typeof(MatchRoom))]`, `[FriendOf(typeof(MatchPlayer))]`
  4. 编译验证

---

## 阶段 8: 集成与测试

### ✅ Task 8.1: PhaseChangedEventHandler_Battle — 编排入口
- **What:** 创建 Battle 阶段事件处理器，编排配对→模拟→结算
- **Where:** `Hotfix/Server/AutoChess/PhaseChangedEventHandler_Battle.cs`
- **Context:**
  - Depends: Task 7.2 (SettlementService), Task 3.1 (CombatSimulator), Task 2.1 (CombatUnitInit)
  - Reads: PhaseChangedEventHandler_Economy.cs (参考 Handler 模式), RoundFSMComponent.cs
- **Steps:**
  1. 创建 `PhaseChangedEventHandler_Battle`, `[Event(SceneType.Map)]`
  2. 订阅 PhaseChangedEvent, 仅处理 RoundPhase.Battle:
     a. 获取 MatchRoom、DeterministicRngComponent
     b. 调用 PairingService.MakePairings
     c. 对每场配对:
        - 获取双方 MatchPlayer 的 RosterComponent + SynergyComponent.Snapshot
        - 调用 CombatUnitInit.InitSide (L和R)
        - combatSeed = rng.DeriveSubSeed(PrngPurpose.Combat, round * 100 + pairIndex)
        - 调用 CombatSimulator.RunCombat
     d. 调用 SettlementService.ProcessResults
     e. 检查是否只剩 1 人 → EndMatch
  3. `[FriendOf(typeof(MatchRoom))]`, `[FriendOf(typeof(MatchPlayer))]`, `[FriendOf(typeof(SynergyComponent))]`, `[FriendOf(typeof(RosterComponent))]`
  4. 编译验证

### ✅ Task 8.2: AutoChessTestHelper — 战斗系统测试
- **What:** 在 AutoChessTestHelper 中添加战斗系统集成测试
- **Where:** 编辑 `Hotfix/Server/AutoChess/AutoChessTestHelper.cs`
- **Context:**
  - Depends: Task 8.1 (全部完成)
  - Reads: 现有 AutoChessTestHelper 测试模式
- **Steps:**
  1. **TestCombatUnitInit**: 验证 UnitInfo→CombatUnitState 转换（星级倍率、修改器应用、镜像）
  2. **TestCombatMovement**: 2 个近战单位对撞，验证每 tick 距离减少
  3. **TestCombatBasicAttack**: 1v1 普攻，验证伤害公式、法力获得
  4. **TestCombatDeterminism**: 相同 seed 运行 2 次，事件流逐条比对
  5. **TestPairingService**: 4人/3人/2人配对，验证 L/R + Ghost
  6. **TestSettlement**: 验证扣血公式、平局各扣1、Ghost 赢不扣
  7. **TestFrenzy**: 验证 tick>=400 攻速翻倍
  8. **TestAssassinJump**: 验证刺客开战跳后排位置
  9. **TestFullBattle**: 完整 E2E 测试 — 构建阵容→配对→战斗→结算→扣血
  10. 编译验证

### ✅ Task 8.3: 编译验证 + plan checkbox 更新
- **What:** 最终全量编译 + 更新 plan 复选框
- **Where:** 运行 `dotnet build ET.sln`
- **Steps:**
  1. 全量编译
  2. 确认无新增错误
  3. 更新所有 Task 为 [x]

---

## 任务总览

| 阶段 | 任务数 | 预估复杂度 |
|------|--------|-----------|
| 1. 数据模型 | 4 | 低 |
| 2. 战斗初始化 | 1 | 中 |
| 3. 核心 Tick 循环 | 2 | 高 |
| 4. 开战特殊处理 | 1 | 高 |
| 5. 动态羁绊 | 1 | 高 |
| 6. 事件流+Frenzy | 1 | 高 |
| 7. 配对与结算 | 2 | 中 |
| 8. 集成与测试 | 3 | 高 |
| **总计** | **15** | |
