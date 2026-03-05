## Why

单位系统（a1-unit）已完成，玩家可以购买、摆位、合成单位。但目前上场单位没有任何联动效果——缺少"构筑方向感"。羁绊系统是自走棋核心循环的关键一环：**标签组合 → 羁绊激活 → 属性加成**，驱动玩家围绕特定标签组合阵容。

GDD 定义了 13 种羁绊（阈值 2/4），每单位携带 2 个标签。本变更实现羁绊统计、激活判定、TraitSnapshot 生成、以及可在战斗框架（E7）之前独立实现的效果子集。

## What Changes

- **羁绊统计引擎**：遍历玩家棋盘单位的 tags，统计每种羁绊 count，按阈值判定 level（0/1/2）
- **TraitSnapshot 数据模型**：PreBattle 阶段冻结羁绊快照，供战斗全程使用
- **部署阶段实时统计**：Deployment 阶段每次棋盘变化时重新计算，用于 UI 展示
- **静态羁绊效果**：PreBattle 时将属性加成写入战斗用数据（Brawler 生命、Brutalist 攻速+攻击、Noble 减伤/增伤等）
- **Goblin 经济效果**：RoundStart 时检查上一回合是否有 Goblin 羁绊激活，赠送哥布林单位（isGift=true，不扣卡池）
- **SynergyDef 参数校准**：当前 ConfigLoader 中的 SynergyDef 参数与 GDD 不一致，需按 GDD 重新校准
- **羁绊变化事件**：发布 SynergyChangedEvent 供 UI 层订阅

**不包含（留给 E7 战斗模拟器）：**
- 动态战斗羁绊的运行时触发逻辑（Assassin 跳后排、Clan 低血爆发、Ranger 叠攻速、P.E.K.K.A 击杀回血、Ace 吸血、Superstar 连发、Undead 诅咒）
- 这些羁绊的 TraitSnapshot 标记会在本变更中写入，但实际执行逻辑在 E7 中实现

## Capabilities

### New Capabilities
- `synergy-counting`: 羁绊统计引擎（count + level 计算）
- `synergy-snapshot`: PreBattle TraitSnapshot 生成与冻结
- `synergy-static-effects`: 静态羁绊属性加成（PreBattle 应用）
- `synergy-goblin-gift`: 哥布林经济羁绊（RoundStart 赠送单位）
- `synergy-deployment-tracking`: 部署阶段实时羁绊追踪

### Modified Capabilities
- 无现有 spec 需修改

## Impact

### 受影响的代码

| 位置 | 变更类型 | 说明 |
|------|---------|------|
| `Model/Server/AutoChess/` | 新增 | SynergyComponent、TraitSnapshot 数据模型 |
| `Model/Share/AutoChess/Events/` | 新增 | SynergyChangedEvent |
| `Model/Share/AutoChess/Defs/SynergyDef.cs` | 修改 | 添加 SynergyType 枚举区分静态/动态/经济 |
| `Hotfix/Server/AutoChess/` | 新增 | SynergyService、PhaseChangedEventHandler_Synergy |
| `Hotfix/Share/AutoChess/AutoChessConfigLoader.cs` | 修改 | SynergyDef 参数按 GDD 校准 |
| `Hotfix/Server/AutoChess/PreBattleService.cs` | 修改 | 调用 SynergyService 生成快照 |
| `Hotfix/Server/AutoChess/MatchRoomFactory.cs` | 修改 | 添加 SynergyComponent |

### Framework Impact Checklist
- [x] **Modules/assemblies affected**: cn.etetet.autochess（Model/Server + Hotfix/Server + Model/Share）
- [ ] **New protocol/message definitions needed?** 不需要（E9 网络层尚未实现）
- [ ] **New config/data files needed?** 不需要（已有 SynergyDef 配置）
- [ ] **Cross-process/cross-thread communication?** 不涉及（同 Fiber 内）
- [x] **New data model types?** SynergyComponent（ComponentOf: MatchPlayer）、TraitSnapshot（纯 C# 类）、SynergyEntry（纯 C# 类）
- [x] **Affects system-map.md?** 是 — 新增羁绊子系统到 autochess 区块

### 羁绊分类（按实现时机）

| 类型 | 羁绊 | 本变更实现 | E7 实现 |
|------|------|-----------|---------|
| 静态（属性加成） | Brawler(+HP)、Brutalist(+攻速+攻击)、Noble(前排减伤/后排增伤)、Blaster(+射程+距离增伤)、Giant(减伤) | ✓ 属性写入 | — |
| 动态（战斗触发） | Assassin(跳后排)、Clan(低血爆发)、Ranger(叠攻速)、Ace(吸血)、P.E.K.K.A(击杀回血)、Superstar(预充+连发)、Undead(诅咒) | ✓ 快照标记 | ✓ 运行时逻辑 |
| 经济 | Goblin(赠送单位) | ✓ 完整实现 | — |

### 依赖
- **前置**: a1-unit（已完成）
- **后续**: E7 战斗模拟器将读取 TraitSnapshot 执行动态羁绊
