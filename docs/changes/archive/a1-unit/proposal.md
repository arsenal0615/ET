## Why

a1-foundation 建立了对局框架（Entity 树、PRNG、RoundFSM），a1-economy 实现了圣水收支，a1-shop 实现了共享卡池和商店系统。

E4 在此基础上实现**单位实例的完整生命周期**：从商店购买到放上棋盘，从棋盘移动到合成升星，从 PreBattle 校验到淘汰清理。这是承上（经济/商店）启下（羁绊/技能/战斗）的关键层——没有单位实例，后续所有系统都没有操作对象。

## What Changes

- 新增 `RosterComponent`：挂在 MatchPlayer，管理棋盘 8×5 格位和板凳 5 槽位的占用状态
- 新增 `UnitInfo`：非 Entity 的纯 C# 类，表示一个单位实例（templateId、star、isGift、位置等）
- 新增 `RosterService`：静态服务类，处理添加/移除/查找/统计单位
- 新增 `PlacementService`：静态服务类，处理 Place（板凳→棋盘）、Move（棋盘内移动）、Swap（任意位置互换）
- 新增 `MergeService`：静态服务类，处理自动合成逻辑（确定性扫描、连锁合成、圣水返还、卡池守恒）
- 新增 `PreBattleService`：静态服务类，处理 PreBattle 阶段的合法性校验与自动修正（超人口下板凳、板凳满强制出售）
- 新增 `PhaseChangedEventHandler_Unit`：订阅 Deployment/PreBattle 阶段事件，触发合成链和校验
- 扩展 `ShopService.TryBuy`：购买成功后调用 RosterService 创建 UnitInfo 入板凳
- 扩展 `ShopService.TrySell`：从 RosterService 查找并移除单位实例
- 扩展 `MatchRoomFactory`：创建 MatchPlayer 时初始化 RosterComponent
- 扩展 `MatchRoomSystem.EliminatePlayer`：淘汰时回收所有棋盘+板凳单位到卡池
- 扩展 `AutoChessTestHelper`：增加 TestUnit 验证 Roster/摆位/合成/PreBattle

**关于 E4 与 E3/E5 的边界：**
- E3（商店）已实现圣水和卡池逻辑，E4 负责实际 UnitInfo 的创建和销毁
- E4 购买流程：ShopService.TryBuy 返回 templateId → RosterService 创建 UnitInfo 入板凳
- E4 出售流程：RosterService 移除 UnitInfo → ShopService.TrySell 处理圣水和卡池
- E5（羁绊）将读取上场单位的 tags 进行统计，E4 提供查询接口但不实现羁绊逻辑

## Capabilities

### New Capabilities

- `roster-management`：单位实例管理，棋盘/板凳格位追踪，人口统计
- `placement`：摆位操作（Place/Move/Swap），含阶段门控和合法性校验
- `merge`：自动合成，2 合 1 升星，确定性扫描顺序，连锁合成
- `prebattle-validation`：PreBattle 合法性校验与自动修正

### Modified Capabilities

- `shop`：TryBuy/TrySell 与 RosterService 集成，实际创建/销毁 UnitInfo

## Impact

### Framework Impact Checklist

- [x] **Modules/assemblies affected**: `cn.etetet.autochess` Model.Server + Hotfix.Server（新增组件和服务类），Hotfix.Server 现有文件扩展（ShopService、MatchRoomFactory、MatchRoomSystem）
- [ ] **New protocol/message definitions needed?** 否（网络层在 E9 实现）
- [ ] **New config/data files needed?** 否（使用现有 UnitTemplateDef 和 AutoChessDefine）
- [ ] **Cross-process/cross-thread communication?** 否
- [x] **New data model types?**
  - `RosterComponent` (ComponentOf: MatchPlayer) — 棋盘格位数组 + 板凳槽位数组 + popUsed 计数
  - `UnitInfo` (纯 C# 类，非 Entity，[EnableClass]) — 单位实例数据（instId、templateId、star、isGift、col、row）
- [x] **Affects system-map.md?** 是，需要在 cn.etetet.autochess 章节补充单位系统条目
