# Stories: A1 自走棋 MVP

> **Source:** docs/game-design/gdd-a1.md + a1-master/design/detail/*
> **Date:** 2026-03-02
> **Scope:** MVP（首个可玩版本）

## 依赖关系总览

```
E1 基础框架
 ├→ E2 经济系统
 │   └→ E3 商店与卡池
 │       └→ E4 单位系统
 │           ├→ E5 羁绊系统
 │           └→ E6 技能系统
 │               └→ E7 战斗模拟器
 │                   └→ E8 回合结算
 └→ E9 网络与同步（可与 E2-E8 并行开发，联调时集成）
     └→ E10 UI
         └→ E11 断线兜底
```

---

## Epic 1: 基础框架（对局生命周期 + 数据模型）

### Story 1.1: 对局数据模型与配置表结构
- **Description:** As a 程序员, I want 定义核心数据结构（UnitTemplate, UnitInstance, SynergyDef, SkillDef, GameConstants）, so that 所有系统有统一的数据基础
- **Acceptance Criteria:**
  - [ ] GameConstants 包含所有不可变常量（棋盘8×5、板凳5、商店3、初始HP12等）
  - [ ] UnitTemplate 包含 24 个单位的完整定义（cost/rarity/base/tags/skill）
  - [ ] SynergyDef 包含 13 种羁绊的阈值和效果参数
  - [ ] 坐标系采用 odd-r offset（可转 axial 计算距离）
  - [ ] 所有 ID 生成确定性（instId = matchId-round-spawnIndex）
- **Estimate:** 5
- **Dependencies:** None
- **Priority:** P0

### Story 1.2: 回合状态机（RoundFSM）
- **Description:** As a 服务端, I want 驱动回合阶段流转（RoundStart→Deployment→PreBattle→Battle→RoundEnd）, so that 对局有正确的时序控制
- **Acceptance Criteria:**
  - [ ] 5 个阶段按时长自动流转（2s/20s/3s/30s/3s）
  - [ ] 每个阶段维护 `phaseEndsAt` 权威时间戳
  - [ ] 操作门控（PhaseGate）正确：各阶段允许/禁止的操作符合操作矩阵
  - [ ] 支持回合递增和终局判定（只剩1人→结束）
- **Estimate:** 5
- **Dependencies:** 1.1
- **Priority:** P0

### Story 1.3: 确定性 PRNG 框架
- **Description:** As a 程序员, I want 实现固定种子 PRNG + 子种子派生, so that 所有随机行为可重放
- **Acceptance Criteria:**
  - [ ] 实现 xxHash32 作为 hash 函数
  - [ ] 子种子派生：hash(matchSeed, round, purpose)，purpose 枚举明确
  - [ ] 全局禁止 System.Random / Math.random，只用项目 PRNG
  - [ ] 金标测试：同 seed 跑 100 次，输出一致
- **Estimate:** 3
- **Dependencies:** 1.1
- **Priority:** P0

### Story 1.4: 对局管理（Match 生命周期）
- **Description:** As a 服务端, I want 管理对局创建/运行/结束的完整生命周期, so that 4 名玩家可以进入同一局游戏
- **Acceptance Criteria:**
  - [ ] Match 创建时生成 matchSeed
  - [ ] 维护 4 名玩家状态（存活/淘汰/排名）
  - [ ] 对局结束后清理所有 Entity（防内存泄漏）
  - [ ] 支持 FinalResults 结算和排名输出
- **Estimate:** 5
- **Dependencies:** 1.2, 1.3
- **Priority:** P0

---

## Epic 2: 经济系统

### Story 2.1: 圣水收支核心
- **Description:** As a 玩家, I want 每回合获得圣水收入并在购买/出售时正确扣除/返还, so that 我可以管理资源做构筑决策
- **Acceptance Criteria:**
  - [ ] 回合开始：所有存活玩家 +4 圣水
  - [ ] 统帅被动：上回合战败者 +randInt(4,8)（用 PRNG）
  - [ ] 购买：elixir -= cost
  - [ ] 出售：elixir += max(cost-1, 0)
  - [ ] 合成返还：每次合并 +1
  - [ ] clamp 到 [0, 999]
  - [ ] 平局不触发战败补偿
- **Estimate:** 3
- **Dependencies:** 1.2
- **Priority:** P0

### Story 2.2: 经济日志（EconomyDelta）
- **Description:** As a 程序员, I want 记录每笔经济变动, so that 可以用于调试和后续结算展示
- **Acceptance Criteria:**
  - [ ] 每笔收支生成 EconomyDelta 记录（type/amount/context）
  - [ ] 支持按回合查询经济流水
- **Estimate:** 2
- **Dependencies:** 2.1
- **Priority:** P1

---

## Epic 3: 商店与共享卡池

### Story 3.1: 共享卡池（SharedPool）
- **Description:** As a 系统, I want 维护全玩家共享的单位库存池, so that 实现"同池博弈"核心机制
- **Acceptance Criteria:**
  - [ ] 初始化 24 种 × 8 份 = 192 份
  - [ ] 购买扣减（通过 Offer 预扣机制）
  - [ ] 出售回流：remaining[templateId] += copiesOfStar(star)
  - [ ] isGift 单位出售/淘汰不回流
  - [ ] 淘汰回收：棋盘+板凳+商店 offer 全部回收
  - [ ] 库存不可为负
- **Estimate:** 5
- **Dependencies:** 1.1
- **Priority:** P0

### Story 3.2: 商店系统（Shop）
- **Description:** As a 玩家, I want 从商店购买单位来构筑阵容, so that 我有构筑选择
- **Acceptance Criteria:**
  - [ ] 3 槽位，同屏不重复 templateId
  - [ ] 首回合初始化 3 个 offer
  - [ ] 购买成功后整行 3 槽重随
  - [ ] Offer 预扣：生成时 remaining -=1，未购买时归还
  - [ ] 购买失败不刷新商店
  - [ ] 出货概率：从 remaining>0 的模板中等概率抽取
  - [ ] 种子确定性：shopRollIndex 递增
- **Estimate:** 5
- **Dependencies:** 3.1, 2.1
- **Priority:** P0

### Story 3.3: 首回合赠送
- **Description:** As a 玩家, I want 首回合免费获得一个2费单位, so that 我有起步基础
- **Acceptance Criteria:**
  - [ ] Round 1 RoundStart 时随机赠送 1 个 2 费单位入板凳
  - [ ] 赠送不消耗圣水、不扣卡池（isGift=true）
  - [ ] 赠送使用 PRNG
- **Estimate:** 1
- **Dependencies:** 3.1, 1.3
- **Priority:** P0

---

## Epic 4: 单位系统

### Story 4.1: 单位实例管理（Roster）
- **Description:** As a 系统, I want 管理玩家的棋盘和板凳单位, so that 单位有正确的位置和状态
- **Acceptance Criteria:**
  - [ ] 棋盘 8×5 格位管理（占用/空闲）
  - [ ] 板凳 5 槽管理（空位选最小 index）
  - [ ] 人口上限随回合变化（R1=2→R5+=6）
  - [ ] popUsed 实时统计
- **Estimate:** 3
- **Dependencies:** 1.1
- **Priority:** P0

### Story 4.2: 摆位操作
- **Description:** As a 玩家, I want 在棋盘上放置和调整单位站位, so that "站位决定命运"
- **Acceptance Criteria:**
  - [ ] PLACE_TO_BOARD：板凳→棋盘（检查人口上限）
  - [ ] MOVE_ON_BOARD：棋盘内移动
  - [ ] SWAP：任意两位置互换（棋盘↔棋盘、棋盘↔板凳、板凳↔板凳）
  - [ ] 阶段门控：Deployment 允许，Battle 禁止
  - [ ] 合法性校验：坐标合法、POS_OCCUPIED、POPCAP_EXCEEDED
- **Estimate:** 3
- **Dependencies:** 4.1
- **Priority:** P0

### Story 4.3: 自动合成（Merge）
- **Description:** As a 玩家, I want 2个同种同星单位自动合成升星, so that 我的单位变强
- **Acceptance Criteria:**
  - [ ] 触发条件：2 个相同 templateId + 相同 star
  - [ ] 确定性顺序：低星先扫→templateId→棋盘优先板凳→y→x→instId
  - [ ] primary 保留（instId/pos 不变），secondary 消耗移除
  - [ ] 合成后 star+=1，hp=maxHp（满血），返还 1 圣水
  - [ ] 连锁合成：合并后重新从 1★ 扫描直到稳定
  - [ ] Deployment 阶段触发，Battle 阶段不触发（延后到下次 Deployment）
  - [ ] 合成不改变卡池库存
- **Estimate:** 5
- **Dependencies:** 4.1, 2.1
- **Priority:** P0

### Story 4.4: PreBattle 合法性校验与自动修正
- **Description:** As a 系统, I want PreBattle 阶段校验并修正非法阵容, so that 战斗输入始终合法
- **Acceptance Criteria:**
  - [ ] 校验：popUsed<=popCap、坐标合法、无重复占用
  - [ ] 超人口修正：按优先级保留（星级→费用→位置→instId），多余下板凳
  - [ ] 板凳满则强制出售（返还圣水+回流卡池）
  - [ ] 进入 Deployment 时跑一次合成链（处理 Battle 阶段买入的重复单位）
- **Estimate:** 3
- **Dependencies:** 4.3
- **Priority:** P0

---

## Epic 5: 羁绊系统

### Story 5.1: 羁绊统计与快照
- **Description:** As a 系统, I want 统计上场单位的羁绊激活状态, so that 战斗中能正确应用羁绊效果
- **Acceptance Criteria:**
  - [ ] 遍历上场单位的 tags，统计每种羁绊的 count
  - [ ] 按阈值 2/4 计算 level（0/1/2）
  - [ ] PreBattle 生成 TraitSnapshot，战斗全程不变
  - [ ] 部署阶段实时统计用于 UI 展示
- **Estimate:** 3
- **Dependencies:** 4.1
- **Priority:** P0

### Story 5.2: 13 种羁绊效果实现
- **Description:** As a 玩家, I want 凑齐羁绊获得战斗增益, so that 构筑阵容有方向感
- **Acceptance Criteria:**
  - [ ] 静态羁绊（Brawler/Giant/Noble/Blaster）：属性加成正确
  - [ ] 动态羁绊（Assassin/Brutalist/Clan/Ranger）：战斗中触发逻辑正确
  - [ ] 特殊羁绊（Ace/Superstar/Undead/P.E.K.K.A）：队长选取/连发/诅咒/击杀回血
  - [ ] 经济羁绊（Goblin）：次回合 RoundStart 赠送单位（isGift，不扣卡池）
  - [ ] 叠加顺序：基础→星级倍率→羁绊静态→羁绊动态→技能/Buff
- **Estimate:** 13
- **Dependencies:** 5.1, 7.1（部分需战斗框架支持）
- **Priority:** P0

---

## Epic 6: 技能系统

### Story 6.1: Cell 流水线框架
- **Description:** As a 程序员, I want 实现 TriggerCell + TargetCell + EffectCell 的组合框架, so that 技能可数据驱动
- **Acceptance Criteria:**
  - [ ] TriggerCell 6 种：COMBAT_START/INTERVAL/ON_HIT_COUNT/ON_KILL/ON_HP_BELOW/MANA_FULL
  - [ ] TargetCell 9 种：SELF/NEAREST/FARTHEST/LOWEST_HP/CLUSTER/AREA_RADIUS/LINE_PIERCE/CONE/MULTI_TARGETS
  - [ ] EffectCell 10 种：DAMAGE/HEAL/STUN/KNOCKBACK/INVISIBILITY/SUMMON/SPAWN_ON_KILL/CLONE_SELF/REFLECT/ROCKET
  - [ ] TimingCell/ScalingCell 可选扩展
  - [ ] 目标选取 Tiebreaker 确定性（instId 升序）
- **Estimate:** 8
- **Dependencies:** 1.1
- **Priority:** P0

### Story 6.2: 法力条系统
- **Description:** As a 单位, I want 通过普攻和受击积累法力并释放技能, so that 技能有节奏感
- **Acceptance Criteria:**
  - [ ] 普攻 +10 法力，受击 +6 法力
  - [ ] 满 100 触发 MANA_FULL 技能
  - [ ] 释放后清零
  - [ ] Superstar 预充（1/3 或 2/3）
- **Estimate:** 2
- **Dependencies:** 6.1
- **Priority:** P0

### Story 6.3: 16 个 SkillDef 配置
- **Description:** As a 程序员, I want 用 Cell 组合定义 24 个单位的技能, so that 每个单位有独特表现
- **Acceptance Criteria:**
  - [ ] 16 个 SkillDef 模板正确组合 Cell
  - [ ] 星级参数通过 ScalingCell 绑定
  - [ ] 每个单位绑定正确的 SkillDef
  - [ ] 特殊技能验证：刺客跳后排、王子冲锋、女巫召唤、骷髅龙克隆等
- **Estimate:** 8
- **Dependencies:** 6.1, 6.2
- **Priority:** P0

---

## Epic 7: 战斗模拟器

### Story 7.1: 战斗 Tick 循环
- **Description:** As a 服务端, I want 以 20 ticks/s 驱动确定性战斗模拟, so that 战斗结果可重放
- **Acceptance Criteria:**
  - [ ] Tick Rate = 20 (dt=50ms)，最大 600 ticks (30s)
  - [ ] 每 Tick 固定顺序：计划任务→状态到期→单位行动(instId升序)→死亡清理→Frenzy 检测
  - [ ] 所有随机来自 combatSeed 派生的 PRNG
  - [ ] 输出 CombatEvent[] 事件流
- **Estimate:** 8
- **Dependencies:** 1.3
- **Priority:** P0

### Story 7.2: 六边形寻路与移动
- **Description:** As a 单位, I want 在六边形棋盘上向目标移动, so that 近战单位能接近敌人
- **Acceptance Criteria:**
  - [ ] 六边形距离计算（axial 坐标）
  - [ ] 6 方向邻居计算（odd-r offset）
  - [ ] 移动：选能使距离严格减少的候选格，优先最大减少
  - [ ] 同等情况按固定方向序列（右→右下→左下→左→左上→右上）
  - [ ] 全部被堵：原地不动
  - [ ] 多单位占格冲突处理
- **Estimate:** 5
- **Dependencies:** 7.1
- **Priority:** P0

### Story 7.3: 目标选择与普攻
- **Description:** As a 单位, I want 选择射程内目标并进行普攻, so that 战斗能推进
- **Acceptance Criteria:**
  - [ ] 目标选择：射程内最近→仇恨优先→instId 升序
  - [ ] 近战 range=1，远程 range=3
  - [ ] 伤害 = floor(atk * damageMultiplier)
  - [ ] 暴击：rng.nextFloat() < critChance → damage * critMultiplier
  - [ ] 护盾优先扣除
  - [ ] 攻速控制（atkSpeed → ticks 间隔）
- **Estimate:** 5
- **Dependencies:** 7.1, 7.2
- **Priority:** P0

### Story 7.4: 技能执行集成
- **Description:** As a 系统, I want 在战斗中正确执行技能 Cell 流水线, so that 技能影响战斗结果
- **Acceptance Criteria:**
  - [ ] 各 TriggerCell 在正确时机检测
  - [ ] TargetCell 选取目标
  - [ ] EffectCell 应用效果（伤害/治疗/眩晕/击退/隐身/召唤等）
  - [ ] 技能事件输出 CAST + 结算变化
  - [ ] SUMMON/CLONE 单位标记 isEffectiveForDamageCount=false
- **Estimate:** 8
- **Dependencies:** 6.1, 6.3, 7.3
- **Priority:** P0

### Story 7.5: Frenzy Time
- **Description:** As a 系统, I want 战斗最后10秒进入加速模式, so that 超时局能尽快结束
- **Acceptance Criteria:**
  - [ ] tick >= 400 时触发 Frenzy
  - [ ] 攻速 ×2
  - [ ] 隐身持续时间 ×0.5
  - [ ] 输出 FRENZY_START 事件
- **Estimate:** 2
- **Dependencies:** 7.3
- **Priority:** P0

### Story 7.6: 战报事件流（CombatEvent）
- **Description:** As a 系统, I want 战斗模拟输出完整事件流, so that 客户端可以播放战斗
- **Acceptance Criteria:**
  - [ ] 每条事件有 i（严格递增）和 tick（非递减）
  - [ ] 事件类型完整：ROUND_START/SPAWN/MOVE/ATTACK/CAST/DAMAGE/HEAL/BUFF_ADD/BUFF_REMOVE/DEATH/FRENZY_START/ROUND_END
  - [ ] 同 tick 内排序：MOVE→ATTACK→CAST→DAMAGE/HEAL/BUFF→DEATH
  - [ ] ROUND_END 包含 winner(L/R/DRAW) 和 leftAliveEffective/rightAliveEffective
  - [ ] Proto 序列化，支持压缩
- **Estimate:** 5
- **Dependencies:** 7.1
- **Priority:** P0

---

## Epic 8: 回合结算

### Story 8.1: 配对算法
- **Description:** As a 系统, I want 每回合确定性配对玩家, so that 每人有对手
- **Acceptance Criteria:**
  - [ ] Fisher-Yates 洗牌（pairSeed）
  - [ ] playerId 小者为 L
  - [ ] 4人→2场1v1，3人→1v1+Ghost，2人→固定对打
  - [ ] Ghost = 最近淘汰者阵容快照（PreBattle 时刻）
- **Estimate:** 3
- **Dependencies:** 1.3, 1.4
- **Priority:** P0

### Story 8.2: 扣血与淘汰
- **Description:** As a 系统, I want 战斗后正确扣血并判定淘汰, so that 对局能收敛结束
- **Acceptance Criteria:**
  - [ ] 输家扣血 = 对面剩余有效单位数 + 1
  - [ ] Skeleton/Clone 不计入有效数
  - [ ] 超时平局：双方各扣 1
  - [ ] 同归于尽平局：双方各扣 1
  - [ ] hp<=0 淘汰
  - [ ] 同回合多人淘汰排名 Tiebreaker（hp→hpBefore→剩余单位→playerId）
  - [ ] 淘汰时触发卡池回收
  - [ ] 赢 Ghost 不扣血
- **Estimate:** 5
- **Dependencies:** 7.6, 8.1
- **Priority:** P0

---

## Epic 9: 网络与同步

### Story 9.1: 对局 Proto 消息定义
- **Description:** As a 程序员, I want 定义客户端-服务端通信消息, so that 操作和事件可以正确传输
- **Acceptance Criteria:**
  - [ ] 操作意图消息：C2S_Buy/C2S_Sell/C2S_Place/C2S_Swap
  - [ ] 操作结果消息：S2C_OpResult（成功/失败+错误码）
  - [ ] 状态同步消息：S2C_RoundState/S2C_PhaseChange
  - [ ] 战斗事件消息：S2C_CombatEvents（完整事件流）
  - [ ] 结算消息：S2C_RoundResult/S2C_MatchResult
- **Estimate:** 3
- **Dependencies:** 1.1
- **Priority:** P0

### Story 9.2: 服务端操作处理（Handler）
- **Description:** As a 服务端, I want 接收并校验玩家操作意图, so that 共享资源安全
- **Acceptance Criteria:**
  - [ ] 所有操作在同一 Actor 内串行处理（天然序列化）
  - [ ] PhaseGate 检查
  - [ ] 校验并返回错误码
  - [ ] 操作成功后广播结果给相关玩家
- **Estimate:** 5
- **Dependencies:** 9.1, 2.1, 3.2, 4.2
- **Priority:** P0

### Story 9.3: 客户端状态同步
- **Description:** As a 客户端, I want 接收服务端状态并更新本地数据, so that UI 能正确展示
- **Acceptance Criteria:**
  - [ ] 接收阶段切换通知
  - [ ] 接收操作结果（成功/失败）
  - [ ] 接收战斗事件流
  - [ ] 部署阶段乐观更新 + 服务端拒绝时回滚
- **Estimate:** 5
- **Dependencies:** 9.1
- **Priority:** P0

---

## Epic 10: UI

### Story 10.1: 部署阶段 UI
- **Description:** As a 玩家, I want 在部署阶段看到棋盘/板凳/商店/圣水并进行操作, so that 我能构筑阵容
- **Acceptance Criteria:**
  - [ ] 棋盘 8×5 六边形网格展示
  - [ ] 板凳 5 槽展示
  - [ ] 商店 3 槽展示（单位卡面+费用）
  - [ ] 圣水数值展示
  - [ ] 人口展示（当前/上限）
  - [ ] 倒计时展示
  - [ ] 拖拽操作（买/摆/换位）
  - [ ] 羁绊面板（实时统计，亮起激活状态）
  - [ ] 对手 HP 展示
- **Estimate:** 13
- **Dependencies:** 9.3
- **Priority:** P0

### Story 10.2: 战斗阶段 UI（事件流播放）
- **Description:** As a 玩家, I want 观看自动战斗过程, so that 我能看到构筑决策的结果
- **Acceptance Criteria:**
  - [ ] 按 CombatEvent tick 时间线播放
  - [ ] 单位移动动画
  - [ ] 攻击/技能特效
  - [ ] 血条/伤害数字
  - [ ] 死亡动画
  - [ ] Frenzy 金色高亮
  - [ ] 战斗阶段仍可操作商店（只买到板凳）
- **Estimate:** 13
- **Dependencies:** 9.3, 7.6
- **Priority:** P0

### Story 10.3: 结算 UI
- **Description:** As a 玩家, I want 看到回合结算和最终排名, so that 我知道输赢
- **Acceptance Criteria:**
  - [ ] 回合结算：胜负/扣血动画
  - [ ] 淘汰通知
  - [ ] 最终排名（1-4名）
  - [ ] "再来一局"按钮
- **Estimate:** 5
- **Dependencies:** 9.3
- **Priority:** P0

### Story 10.4: 大厅与匹配 UI
- **Description:** As a 玩家, I want 从大厅开始匹配进入对局, so that 我能开始游戏
- **Acceptance Criteria:**
  - [ ] 大厅界面（开始匹配按钮）
  - [ ] 匹配中等待界面
  - [ ] 加载界面
  - [ ] 错误提示（匹配失败等）
- **Estimate:** 3
- **Dependencies:** 9.1
- **Priority:** P1

---

## Epic 11: 断线兜底

### Story 11.1: 断线检测与超时认负
- **Description:** As a 系统, I want 检测玩家断线并在超时后自动处理, so that 对局不会卡住
- **Acceptance Criteria:**
  - [ ] Gate Session 断开时标记 PlayerState=Disconnected
  - [ ] 断线后该玩家不执行任何操作（挂机）
  - [ ] 其他玩家继续正常游戏
  - [ ] 超时 60 秒未重连→认负淘汰（hp=0）
  - [ ] 淘汰后正常回收卡池
- **Estimate:** 3
- **Dependencies:** 1.4, 9.2
- **Priority:** P0

---

## Summary

| Epic | 名称 | Stories | 总点数 | 优先级 |
|------|------|---------|--------|--------|
| E1 | 基础框架 | 4 | 18 | P0 |
| E2 | 经济系统 | 2 | 5 | P0 |
| E3 | 商店与卡池 | 3 | 11 | P0 |
| E4 | 单位系统 | 4 | 14 | P0 |
| E5 | 羁绊系统 | 2 | 16 | P0 |
| E6 | 技能系统 | 3 | 18 | P0 |
| E7 | 战斗模拟器 | 6 | 33 | P0 |
| E8 | 回合结算 | 2 | 8 | P0 |
| E9 | 网络与同步 | 3 | 13 | P0 |
| E10 | UI | 4 | 34 | P0/P1 |
| E11 | 断线兜底 | 1 | 3 | P0 |
| **Total** | | **34** | **173** | |

### 关键路径（最长依赖链）

```
E1(18) → E3(11) → E4(14) → E6(18) → E7(33) → E8(8) = 102 点
```

### 可并行的工作流

- E2（经济）和 E4（单位）可在 E1 完成后并行
- E5（羁绊）和 E6（技能）可在 E4 完成后并行
- E9（网络）可与 E2-E8 并行开发，联调时集成
- E10（UI）可在框架就绪后逐步开发
