## ADDED Requirements

### Requirement: Tag Counting
系统 SHALL 遍历玩家**棋盘上**（Row >= 0）的所有 UnitInfo，通过 TemplateId 查找 UnitTemplateDef.Tags，累加每种羁绊标签的出现次数。板凳单位（Row == -1）不计入统计。

#### Scenario: 基础标签统计
- **WHEN** 玩家棋盘上有 3 个单位：迷你皮卡(P.E.K.K.A/Brutalist)、哥布林(Goblin/Assassin)、皇家幽灵(Undead/Assassin)
- **THEN** 统计结果为：P.E.K.K.A=1, Brutalist=1, Goblin=1, Assassin=2, Undead=1

#### Scenario: 板凳单位不计入
- **WHEN** 玩家棋盘上有 1 个迷你皮卡(P.E.K.K.A/Brutalist)，板凳上有 1 个皮卡超人(P.E.K.K.A/Brawler)
- **THEN** P.E.K.K.A 的 count 为 1（不是 2）

#### Scenario: 同模板多单位
- **WHEN** 玩家棋盘上有 2 个迷你皮卡（不同实例，同 TemplateId=1）
- **THEN** P.E.K.K.A=2, Brutalist=2（每个实例分别贡献标签）

#### Scenario: 棋盘无单位
- **WHEN** 玩家棋盘上没有任何单位（所有单位在板凳）
- **THEN** 所有羁绊 count 均为 0

---

### Requirement: Level Calculation
系统 SHALL 根据每种羁绊的 count 与 SynergyDef.Thresholds 比对，计算激活等级（level）：
- count < Thresholds[0] → level = 0（未激活）
- Thresholds[0] <= count < Thresholds[1] → level = 1
- count >= Thresholds[1] → level = 2

#### Scenario: 未激活
- **WHEN** Assassin count = 1，阈值为 [2, 4]
- **THEN** Assassin level = 0

#### Scenario: 一档激活
- **WHEN** Assassin count = 2，阈值为 [2, 4]
- **THEN** Assassin level = 1

#### Scenario: 二档激活
- **WHEN** Assassin count = 4，阈值为 [2, 4]
- **THEN** Assassin level = 2

#### Scenario: 超过二档阈值
- **WHEN** Assassin count = 5，阈值为 [2, 4]
- **THEN** Assassin level = 2（不会超过 2）

#### Scenario: 恰好达到一档
- **WHEN** Brawler count = 2，阈值为 [2, 4]
- **THEN** Brawler level = 1

---

### Requirement: Effect Scope
羁绊效果 SHALL 仅作用于拥有该标签的单位（tag holders），不影响队伍中不拥有该标签的单位。

#### Scenario: 效果仅限标签持有者
- **WHEN** Brawler 激活 level=1（+50% HP），玩家棋盘有迷你皮卡(P.E.K.K.A/Brutalist) 和 野蛮人(Clan/Brawler)
- **THEN** 只有野蛮人获得 Brawler 加成，迷你皮卡不受影响（它没有 Brawler 标签）

---

### Requirement: SynergyDef Config Alignment
AutoChessConfigLoader 中的 SynergyDef 参数 MUST 与 GDD 定义一致。当前配置需要按以下 GDD 定义校准：

| 羁绊 | GDD 效果 | 类型 |
|------|---------|------|
| Ace | 队长+伤害+吸血(40%/70%) | 动态 |
| Assassin | 开战跳后排+暴击(30%/60%) | 动态 |
| Blaster | 射程+1+距离增伤(10%/15%每格) | 静态 |
| Brawler | 最大生命(+50%/+100%) | 静态 |
| Brutalist | 攻速(+30%/+60%)+印记削最大生命 | 混合 |
| Clan | 低血触发治疗+攻速(35%/70%)持续5秒 | 动态 |
| Giant | 受伤-40%（仅1档） | 静态 |
| Goblin | 次回合白嫖哥布林单位 | 经济 |
| P.E.K.K.A | 击杀后回血60%+增伤40%（仅1档） | 动态 |
| Noble | 前排减伤/后排增伤(25%/40%) | 静态 |
| Ranger | 攻击叠攻速(+10%/+15%每次，有上限) | 动态 |
| Superstar | 技能预充(1/3或2/3)+连发50%概率(最多4次) | 动态 |
| Undead | 诅咒高血敌人削最大生命(25%/50%) | 动态 |

#### Scenario: 配置一致性验证
- **WHEN** AutoChessConfigLoader.Init() 执行完毕
- **THEN** 每个 SynergyDef 的 Effects 参数与 GDD 表格中的数值一致
