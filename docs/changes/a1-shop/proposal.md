## Why

a1-foundation 建立了对局框架（Entity 树、PRNG、RoundFSM），a1-economy 实现了圣水收支。
E3 在两者之上实现自走棋的核心博弈机制：**共享卡池 + 商店**。

玩家从共享卡池中争抢单位是自走棋"同池博弈"的灵魂。没有这一系统，后续的单位、羁绊、技能系统都无法落地。

## What Changes

- 新增 `SharedPoolComponent`：挂在 MatchRoom，跟踪全局单位库存（24 种 × 8 份 = 192 份）
- 新增 `ShopComponent`：挂在 MatchPlayer，管理 3 个商店槽位和刷新索引
- 新增 `ShopService`：静态服务类，处理生成 Offer、购买、出售操作
- 新增 `PhaseChangedEventHandler_Shop`：RoundStart 时为所有玩家生成首次商店 Offer
- 新增 `FirstRoundGiftService`：Round 1 RoundStart 时为每个玩家随机赠送 1 个 2 费单位（isGift=true，不扣卡池）
- 新增 `ShopOffer`：商店槽位数据结构（templateId + isGift 标记）
- 扩展 `AutoChessTestHelper`：增加 TestShop 验证
- 扩展 `MatchRoomFactory`：创建 MatchRoom 时初始化 SharedPoolComponent 和 ShopComponent

**关于 E3 与 E4 的边界：**
买入操作完成后返回 templateId，实际的 UnitInstance 创建由 E4（单位系统）负责；
出售操作接收 templateId + cost 参数，实际的 UnitInstance 移除由 E4 负责。
E3 只负责圣水和卡池的状态变化。

## Capabilities

### New Capabilities

- `shared-pool`：共享卡池，初始化/预扣/回流逻辑
- `shop`：商店系统，Offer 生成、购买、出售
- `first-round-gift`：首回合赠送，确定性选取 2 费单位，不扣卡池

## Impact

### Framework Impact Checklist

- [ ] **Modules/assemblies affected**: `cn.etetet.autochess` Model.Server + Hotfix.Server
- [ ] **New protocol/message definitions needed?** 否（网络层在 E9 实现）
- [ ] **New config/data files needed?** 否（使用现有 UnitTemplateDef）
- [ ] **Cross-process/cross-thread communication?** 否
- [ ] **New data model types?**
  - `SharedPoolComponent` (ComponentOf: MatchRoom) — 共享卡池，持有 `int[] Remaining`
  - `ShopOffer` (纯 C# 类，非 Entity) — 单个槽位数据（templateId = -1 表示空）
  - `ShopComponent` (ComponentOf: MatchPlayer) — 商店状态，持有 3 个 ShopOffer + shopRollIndex
- [ ] **Affects system-map.md?** 是，需要在 cn.etetet.autochess 章节补充商店/卡池系统条目
