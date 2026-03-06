## ADDED Requirements

### Requirement: Goblin Gift Trigger
Goblin 羁绊 SHALL 在 RoundStart 阶段检查玩家**上一回合**的羁绊状态（非当前回合）：
- 若上一回合 Goblin level >= 1，赠送 1 个哥布林单位到板凳
- 若上一回合 Goblin level >= 2，赠送 2 个哥布林单位到板凳

"上一回合"指 Round N-1 的 PreBattle 快照中的 Goblin 激活状态。

#### Scenario: Goblin Level 1 赠送
- **WHEN** Round 2 的 RoundStart，Round 1 快照显示 Goblin level=1
- **THEN** 赠送 1 个哥布林标签单位到玩家板凳

#### Scenario: Goblin Level 2 赠送
- **WHEN** Round 3 的 RoundStart，Round 2 快照显示 Goblin level=2
- **THEN** 赠送 2 个哥布林标签单位到玩家板凳

#### Scenario: 首回合不触发
- **WHEN** Round 1 的 RoundStart
- **THEN** 不触发 Goblin 赠送（无上一回合快照）

#### Scenario: 上一回合未激活
- **WHEN** Round 3 的 RoundStart，Round 2 快照显示 Goblin level=0
- **THEN** 不赠送任何单位

---

### Requirement: Goblin Unit Selection
赠送的单位 SHALL 从拥有 Goblin 标签的 UnitTemplateDef 中随机选择（使用 PRNG，purpose=GoblinGift）。

当前拥有 Goblin 标签的单位：
- Id=2 哥布林(2费, Goblin/Assassin)
- Id=3 吹箭哥布林(2费, Goblin/Ranger)
- Id=12 矛兵哥布林(3费, Goblin/Blaster)
- Id=20 哥布林机甲(4费, Goblin/Brutalist)

选择范围为所有 Goblin 标签单位，不受费用限制。

#### Scenario: 随机选择确定性
- **WHEN** 相同 matchSeed、相同 round，触发 Goblin 赠送
- **THEN** 使用 DeriveSubSeed(PrngPurpose.GoblinGift, round) 选择单位，结果确定性可重放

#### Scenario: 等概率选取
- **WHEN** Goblin 赠送触发
- **THEN** 从 4 个 Goblin 模板中等概率随机选取

---

### Requirement: Goblin Gift Properties
赠送的单位 MUST 标记为 `isGift=true`，star=1，放入板凳。

#### Scenario: isGift 标记
- **WHEN** Goblin 赠送触发
- **THEN** 创建的 UnitInfo 有 IsGift=true, Star=1

#### Scenario: 不消耗圣水
- **WHEN** Goblin 赠送触发
- **THEN** 玩家圣水不变

#### Scenario: 不扣卡池
- **WHEN** Goblin 赠送触发
- **THEN** SharedPoolComponent.Remaining 不变（isGift 单位不占用卡池配额）

#### Scenario: 出售不回流
- **WHEN** 玩家出售 Goblin 赠送的 isGift=true 单位
- **THEN** 单位直接销毁，不回流卡池（此行为已在 a1-unit 中实现）

---

### Requirement: Goblin Gift Bench Overflow
若板凳已满（5/5），Goblin 赠送 SHALL 被跳过（不赠送该单位）。

#### Scenario: 板凳满不赠送
- **WHEN** Goblin level=1，玩家板凳 5/5
- **THEN** 不赠送，不报错

#### Scenario: 部分赠送
- **WHEN** Goblin level=2（赠送 2 个），玩家板凳 4/5
- **THEN** 赠送第 1 个成功，第 2 个因板凳满被跳过

---

### Requirement: Goblin Snapshot Persistence
系统 SHALL 在每回合的 TraitSnapshot 中记录 Goblin 的激活状态，供下一回合 RoundStart 使用。系统需在 SynergyComponent 中保存"上一回合 Goblin level"以跨回合传递。

#### Scenario: 跨回合数据传递
- **WHEN** Round 2 PreBattle 生成 TraitSnapshot，Goblin level=1
- **THEN** SynergyComponent 保存 lastGoblinLevel=1，Round 3 RoundStart 可读取
