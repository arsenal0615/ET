## ADDED Requirements

### Requirement: Real-time Synergy Recalculation
系统 SHALL 在 Deployment 阶段，每次棋盘单位发生变化时重新计算羁绊统计。触发重新计算的操作包括：
- 购买单位到板凳后触发合成（合成可能将单位送上棋盘）
- 放置单位到棋盘（PLACE_TO_BOARD）
- 棋盘内移动（MOVE_ON_BOARD）— 仅影响 Noble 位置相关效果
- 互换位置（SWAP）— 当涉及棋盘↔板凳时影响上场单位数
- 出售棋盘上的单位

#### Scenario: 放置触发重新计算
- **WHEN** Deployment 阶段，玩家将板凳上的刺客放到棋盘
- **THEN** 重新统计所有羁绊 count 和 level

#### Scenario: 出售触发重新计算
- **WHEN** Deployment 阶段，玩家出售棋盘上的一个 Brawler 标签单位
- **THEN** Brawler count -1，若低于阈值则 level 降低

#### Scenario: 板凳操作不触发
- **WHEN** 玩家在板凳内交换两个单位（SWAP 板凳↔板凳）
- **THEN** 不重新计算（棋盘单位未变化）

---

### Requirement: SynergyChangedEvent
每次羁绊统计重新计算后，若结果与上一次不同，系统 SHALL 发布 SynergyChangedEvent。

事件 MUST 包含：
- MatchPlayerId (long): 玩家 Entity Id
- ActiveSynergies: 当前所有已激活（level > 0）羁绊的列表

#### Scenario: 羁绊激活变化时发布
- **WHEN** 放置第 2 个 Assassin 到棋盘，Assassin 从 level 0 → level 1
- **THEN** 发布 SynergyChangedEvent，ActiveSynergies 包含 Assassin(level=1)

#### Scenario: count 变化但 level 不变时发布
- **WHEN** 放置第 3 个 Assassin 到棋盘，Assassin count 从 2→3，level 仍为 1
- **THEN** 发布 SynergyChangedEvent（count 变化也需通知 UI）

#### Scenario: 无变化不发布
- **WHEN** 棋盘内移动一个单位（MOVE_ON_BOARD），且该单位没有 Noble 标签
- **THEN** 不发布 SynergyChangedEvent（统计结果无变化）

---

### Requirement: Deployment Phase Scope
实时羁绊追踪 SHALL 仅在 Deployment 和 Battle 阶段生效。其他阶段（RoundStart/PreBattle/RoundEnd）的单位变化不触发重新计算。

#### Scenario: RoundStart 阶段不追踪
- **WHEN** RoundStart 阶段 Goblin 赠送新单位到板凳
- **THEN** 不触发羁绊重新计算（单位在板凳，且不在追踪阶段）

#### Scenario: Battle 阶段购买不触发
- **WHEN** Battle 阶段玩家购买单位到板凳
- **THEN** 不触发重新计算（购买到板凳不影响棋盘单位统计）

---

### Requirement: SynergyComponent Live State
SynergyComponent SHALL 维护一份"当前实时统计"（LiveSynergies），与 TraitSnapshot（冻结快照）分开存储：
- LiveSynergies: Deployment 阶段实时更新，供 UI 读取
- TraitSnapshot: PreBattle 生成后冻结，供战斗读取

#### Scenario: 两份数据独立
- **WHEN** Deployment 阶段玩家调整阵容
- **THEN** LiveSynergies 实时更新，上一回合的 TraitSnapshot 不受影响

#### Scenario: PreBattle 时统一
- **WHEN** PreBattle 阶段开始
- **THEN** 基于当前棋盘状态生成 TraitSnapshot，LiveSynergies 与 TraitSnapshot 此时内容一致
