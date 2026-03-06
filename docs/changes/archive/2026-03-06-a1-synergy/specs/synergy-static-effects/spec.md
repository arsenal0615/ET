## ADDED Requirements

### Requirement: Static Synergy Application Timing
静态羁绊效果 SHALL 在 PreBattle 阶段、TraitSnapshot 生成后立即应用。应用顺序：基础属性 → 星级倍率 → 羁绊静态效果（GDD 叠加顺序）。

#### Scenario: PreBattle 时应用
- **WHEN** PreBattle 阶段开始，TraitSnapshot 已生成
- **THEN** 系统遍历每个棋盘单位，根据 UnitSynergyMap 中的已激活静态羁绊修改战斗属性

#### Scenario: 仅静态羁绊在此阶段应用
- **WHEN** 玩家同时激活了 Brawler(静态) 和 Assassin(动态) 羁绊
- **THEN** PreBattle 只应用 Brawler 的 HP 加成；Assassin 的跳后排标记写入快照但不在此阶段执行

---

### Requirement: Brawler — Max HP Bonus
Brawler 羁绊 SHALL 为拥有 Brawler 标签的单位增加最大生命值：
- Level 1: +50% maxHp
- Level 2: +100% maxHp

加成基于星级倍率后的 HP 值。当前 HP 同步增加（满血进入战斗）。

#### Scenario: Brawler Level 1
- **WHEN** 野蛮人(Clan/Brawler) 基础 HP=520，1★，Brawler level=1
- **THEN** 战斗 HP = 520 × 1.5 = 780

#### Scenario: Brawler Level 2
- **WHEN** 野蛮人 基础 HP=520，1★，Brawler level=2
- **THEN** 战斗 HP = 520 × 2.0 = 1040

#### Scenario: 多星级叠加
- **WHEN** 野蛮人 基础 HP=520，2★（星级倍率×2），Brawler level=1
- **THEN** 战斗 HP = (520 × 2) × 1.5 = 1560

---

### Requirement: Giant — Damage Reduction
Giant 羁绊 SHALL 为拥有 Giant 标签的单位提供受伤减免：
- 仅 1 档：受伤 -40%（Thresholds[0]=2 即可触发）
- Level 2 与 Level 1 效果相同（GDD 标注"仅1档"）

减伤标记写入 TraitSnapshot，由战斗框架（E7）在伤害结算时读取。

#### Scenario: Giant 激活
- **WHEN** 皇家巨人(Giant/Ranger) 和 电巨人(Giant/Superstar) 上场，Giant level=1
- **THEN** 两个单位的 TraitSnapshot 标记 damageReduction = 0.40

#### Scenario: Giant Level 2 不额外增强
- **WHEN** Giant count=4, level=2
- **THEN** damageReduction 仍为 0.40（与 level 1 相同）

---

### Requirement: Noble — Position-Based Bonus
Noble 羁绊 SHALL 根据单位位置给予不同加成：
- 前排（Row 0-1）：减伤
- 后排（Row 2-4）：增伤
- Level 1: 25% 减伤/增伤
- Level 2: 40% 减伤/增伤

#### Scenario: Noble 前排减伤
- **WHEN** 王子(Noble/Brawler) 在 Row=0，Noble level=1
- **THEN** 标记 damageReduction = 0.25

#### Scenario: Noble 后排增伤
- **WHEN** 火枪手(Noble/Superstar) 在 Row=3，Noble level=2
- **THEN** 标记 damageMultiplier = 1.40（基础 1.0 + 0.40）

#### Scenario: Noble 分界线
- **WHEN** 单位在 Row=2
- **THEN** 视为后排，获得增伤加成

---

### Requirement: Blaster — Range and Distance Damage
Blaster 羁绊 SHALL 为拥有 Blaster 标签的单位提供：
- 射程 +1（所有等级）
- 距离增伤：每与目标多一格距离，额外伤害百分比
  - Level 1: +10% 每格
  - Level 2: +15% 每格

射程加成在 PreBattle 静态写入。距离增伤标记写入 TraitSnapshot，由 E7 运行时计算。

#### Scenario: 射程加成
- **WHEN** 法师(Clan/Blaster) 基础 Range=3，Blaster level=1
- **THEN** 战斗 Range = 4

#### Scenario: 距离增伤标记
- **WHEN** Blaster level=2
- **THEN** TraitSnapshot 标记 distanceDamageBonus = 0.15（每格）

---

### Requirement: Brutalist — Attack Speed Bonus (Static Part)
Brutalist 羁绊的攻速加成部分 SHALL 在 PreBattle 静态应用：
- Level 1: 攻速 +30%
- Level 2: 攻速 +60%

印记削最大生命效果为动态部分，标记写入 TraitSnapshot，由 E7 执行。

#### Scenario: Brutalist 攻速加成
- **WHEN** 迷你皮卡(P.E.K.K.A/Brutalist) 基础 AtkSpeed=1.5，Brutalist level=1
- **THEN** 战斗 AtkSpeed = 1.5 × 0.7 = 1.05（攻击间隔缩短 30%，即攻速提升 30%）

#### Scenario: Brutalist Level 2
- **WHEN** 迷你皮卡 基础 AtkSpeed=1.5，Brutalist level=2
- **THEN** 战斗 AtkSpeed = 1.5 × 0.4 = 0.60（攻速提升 60%）

---

### Requirement: Dynamic Synergy Snapshot Marking
对于动态羁绊（Ace/Assassin/Clan/Ranger/P.E.K.K.A/Superstar/Undead），系统 SHALL 在 TraitSnapshot 中写入激活标记和参数，但不在 PreBattle 执行任何运行时逻辑。E7 战斗模拟器将读取这些标记来执行。

#### Scenario: Assassin 快照标记
- **WHEN** Assassin level=1
- **THEN** TraitSnapshot 标记：type=Dynamic, tag="Assassin", level=1, params={jumpBackRow=true, critBonus=0.30}

#### Scenario: Superstar 快照标记
- **WHEN** Superstar level=2
- **THEN** TraitSnapshot 标记：type=Dynamic, tag="Superstar", level=2, params={manaPrecharge=0.667, doubleCastChance=0.50, maxDoubleCasts=4}

#### Scenario: 动态羁绊不修改属性
- **WHEN** PreBattle 应用羁绊效果
- **THEN** 动态羁绊不修改任何单位的 Hp/Atk/AtkSpeed/Range 等基础属性

---

### Requirement: Effect Stacking
当一个单位受到多个静态羁绊影响时，效果 SHALL 叠加计算（乘法叠加）。

#### Scenario: 多羁绊叠加
- **WHEN** 皮卡超人(P.E.K.K.A/Brawler)，P.E.K.K.A level=1（动态，仅标记），Brawler level=1（+50% HP）
- **THEN** HP 加成仅来自 Brawler：HP × 1.5（P.E.K.K.A 是动态羁绊，不影响属性）

#### Scenario: Noble + Giant 叠加
- **WHEN** 皇家巨人(Giant/Ranger) 在 Row=0，Giant level=1(减伤40%)，Noble level=1 且 Noble 标签持有者...
- **THEN** 皇家巨人没有 Noble 标签，所以只有 Giant 减伤 40%（Noble 效果仅限 Noble 标签持有者）
