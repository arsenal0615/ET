## 为什么

自走棋 MVP 的核心系统（E1-E6）已全部完成：基础框架、经济、商店、单位、羁绊、技能执行引擎。现在需要实现 **E7 战斗模拟器 + E8 回合结算**，将所有构筑决策转化为可观赏、可回放、确定性的战斗结果。

这是 MVP 关键路径上的最后一个核心后端系统（E7→E8→E9 网络→E10 UI）。

## 变更内容

- 实现确定性战斗 Tick 循环（20 ticks/s，最大 600 ticks / 30 秒）
- 实现单位初始化（UnitInfo + TraitSnapshot → CombatUnitState）
- 实现六边形移动与寻路（基于已有 HexUtil）
- 实现普攻系统（目标选取、伤害计算、暴击、法力获得）
- 集成已有技能执行引擎（SkillExecutor）到 Tick 循环
- 实现战斗开始特殊处理（刺客跳后排、亡灵诅咒）
- 实现 Frenzy Time（最后 10 秒加速）
- 实现 CombatEvent 事件流输出
- 实现配对算法（Fisher-Yates 确定性配对）
- 实现扣血、淘汰判定、Ghost 补齐
- 实现动态羁绊效果（Ace/Assassin/Clan/P.E.K.K.A/Ranger/Undead 战斗中触发）

## Capabilities

### 新增 Capabilities
- `combat-tick-loop`: 确定性 Tick 循环引擎，每 tick 固定顺序处理计划任务→状态到期→单位行动→死亡清理→Frenzy 检测
- `combat-unit-init`: UnitInfo + TraitSnapshot → CombatUnitState 转换，含镜像站位和星级倍率
- `combat-movement`: 六边形寻路与移动（距离递减贪心，moveSpeed 间隔控制）
- `combat-attack`: 普攻系统（目标选取、伤害公式、暴击、护盾、攻速间隔）
- `combat-start-effects`: 战斗开始特殊处理（刺客跳后排、亡灵诅咒）
- `combat-dynamic-traits`: 动态羁绊战斗中触发（Ace 队长、Clan 低血爆发、P.E.K.K.A 击杀回血、Ranger 攻速叠加、Undead 增伤）
- `combat-frenzy`: Frenzy Time（tick>=400 攻速×2、隐身×0.5）
- `combat-events`: CombatEvent 事件流生成（12 种事件类型，同 tick 内固定排序）
- `round-pairing`: 确定性配对算法（Fisher-Yates + Ghost 补齐）
- `round-settlement`: 扣血、淘汰判定、排名 Tiebreaker

## 影响

### 框架影响检查清单
- [x] **受影响的模块/程序集**: cn.etetet.autochess（Model/Share + Model/Server + Hotfix/Server）
- [ ] **需要新的协议/消息定义？** 否（E9 网络层处理）
- [ ] **需要新的配置/数据文件？** 否（所有配置已在 AutoChessConfigLoader 中）
- [ ] **跨进程/跨线程通信？** 否（战斗模拟在单个 Map Fiber 内完成）
- [x] **新的数据模型类型？** CombatEvent（事件流）、CombatSimulator（Tick 引擎）、PairingResult（配对结果）
- [x] **影响 system-map.md？** 新增战斗模拟器和回合结算章节

### 上游依赖（已完成）
- E1 基础框架：MatchRoom、RoundFSM、PRNG ✅
- E4 单位系统：UnitInfo、RosterComponent ✅
- E5 羁绊系统：TraitSnapshot、UnitBattleModifiers ✅
- E6 技能系统：SkillExecutor、TriggerChecker、TargetSelector、EffectApplier、ManaService ✅

### 下游消费者（未来）
- E9 网络层：订阅战斗结果事件，下发 CombatEvent[] 给客户端
- E10 UI：客户端按事件流播放战斗动画
