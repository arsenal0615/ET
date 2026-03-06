## ADDED Requirements

### Requirement: TraitSnapshot Generation
系统 SHALL 在 PreBattle 阶段为每个存活玩家生成一份 TraitSnapshot，包含：
- 所有已激活羁绊的 tag、level、count
- 每个棋盘单位 InstId 到其适用羁绊效果的映射

TraitSnapshot 生成后 MUST 冻结，战斗全程不可修改。

#### Scenario: PreBattle 生成快照
- **WHEN** 阶段从 Deployment 转换到 PreBattle
- **THEN** 每个存活玩家的 SynergyComponent 持有一份新的 TraitSnapshot

#### Scenario: 快照内容完整
- **WHEN** 玩家棋盘有迷你皮卡(P.E.K.K.A/Brutalist)、皮卡超人(P.E.K.K.A/Brawler)、野蛮人(Clan/Brawler)
- **THEN** 快照包含：P.E.K.K.A level=1(count=2)、Brawler level=1(count=2)、Brutalist level=0(count=1)、Clan level=0(count=1)
- **AND** 单位效果映射：迷你皮卡→[P.E.K.K.A lv1]，皮卡超人→[P.E.K.K.A lv1, Brawler lv1]，野蛮人→[Brawler lv1]

#### Scenario: 战斗期间不变
- **WHEN** TraitSnapshot 在 PreBattle 生成后
- **THEN** 整个 Battle 阶段快照内容不可被任何操作修改

---

### Requirement: Snapshot Lifecycle
TraitSnapshot SHALL 在每个 PreBattle 阶段重新生成（覆盖上一回合的快照）。RoundEnd 阶段后，战斗框架（E7）不再引用该快照。

#### Scenario: 每回合重新生成
- **WHEN** Round 3 的 PreBattle 阶段开始
- **THEN** 新的 TraitSnapshot 覆盖 Round 2 的快照

#### Scenario: 淘汰玩家不生成
- **WHEN** 玩家已被淘汰（MatchPlayer.Eliminated == true）
- **THEN** 不为该玩家生成 TraitSnapshot

---

### Requirement: Snapshot Data Model
TraitSnapshot MUST 包含以下数据结构：

1. **ActiveSynergies**: `List<SynergyEntry>`，每个 SynergyEntry 包含：
   - Tag (string): 羁绊标签
   - Count (int): 当前上场单位拥有该标签的总数
   - Level (int): 激活等级（0/1/2）
   - SynergyType (enum): Static/Dynamic/Economic

2. **UnitSynergyMap**: `Dictionary<int, List<SynergyEntry>>`，key 为 UnitInfo.InstId，value 为该单位适用的已激活羁绊列表（仅 level > 0 的条目）

#### Scenario: 数据结构正确
- **WHEN** 查询快照中 InstId=1 的单位（迷你皮卡，标签 P.E.K.K.A/Brutalist）
- **THEN** 若 P.E.K.K.A level=1，Brutalist level=0，则 UnitSynergyMap[1] 仅包含 P.E.K.K.A 的 SynergyEntry（Brutalist 未激活不包含）
