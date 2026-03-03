## Why

A1 自走棋的一切玩法决策都围绕"圣水"进行——买单位、卖单位、合成，都涉及圣水收支。当前 a1-foundation 已建立 MatchPlayer.Elixir 字段和回合状态机（RoundStart 阶段），但没有任何经济逻辑：回合开始无收入、购买/出售无扣除、战败无补偿。没有经济系统，后续 E3（商店）和 E4（单位管理）均无法落地——购买逻辑和出售逻辑都依赖本 change。

E2 对应 GDD 3.1 节经济系统，覆盖 Epic 2 的两个 Story：
- **Story 2.1（P0）:** 圣水收支核心——收入/购买/出售/合成/战败补偿
- **Story 2.2（P1）:** 经济日志 EconomyDelta——每笔收支的可审计记录

## What Changes

### 修改现有 Entity
- `MatchPlayer`（已存在）— 无需新增字段，Elixir 已在 a1-foundation 中定义

### 新增 Component（Model/Server）
- `EconomyLogComponent` — ChildOf MatchPlayer，持有本局所有 EconomyDelta 记录（List）

### 新增值类型（Model/Share）
- `EconomyDeltaType` — 枚举：RoundIncome / CommanderPassive / Buy / Sell / Merge
- `EconomyDelta` — struct：Type, Amount, Round, Context(string)

### 新增 System 逻辑（Hotfix/Server）
- `EconomyService` — 纯静态服务类，集中所有圣水变更入口：
  - `GiveRoundIncome(player, round)` — +4，记录 RoundIncome delta
  - `GiveCommanderPassive(player, rng, round)` — +randInt(4,8)，记录 CommanderPassive delta
  - `DeductBuy(player, cost)` — elixir -= cost，记录 Buy delta；返回 bool（是否成功）
  - `GiveSell(player, cost)` — elixir += max(cost-1,0)，记录 Sell delta
  - `GiveMerge(player, round)` — elixir += 1，记录 Merge delta
  - 所有操作 clamp 到 [0, 999]
- `EconomyLogComponentSystem` — Awake/Destroy 生命周期 + GetDeltasByRound(round)
- `MatchRoomSystem` 修改 — RoundStart 阶段调用 GiveRoundIncome（存活玩家），战斗结算调用 GiveCommanderPassive（上回合战败者）

### 新增事件（Model/Share）
- `ElixirChangedEvent` — struct：PlayerId, OldValue, NewValue，供后续 UI 监听

## Capabilities

### New Capabilities
- `economy-income`: 回合收入自动发放（基础 +4 + 统帅被动补偿）
- `economy-transactions`: 购买/出售/合成的圣水变更入口（单一职责，全部经过 EconomyService）
- `economy-log`: 按回合查询经济流水（调试/结算展示）

### Modified Capabilities
- `round-state-machine`（已有）— RoundStart 阶段增加收入发放逻辑
- `match-lifecycle`（已有）— 战斗结算后增加统帅被动补偿触发点

## Impact

### 框架影响清单
- [x] **模块/程序集受影响**: cn.etetet.autochess（Hotfix/Server + Model/Server + Model/Share）
- [ ] **新协议/消息定义**: 暂不需要（经济变更服务端内部处理，待 E9 网络层再对外同步）
- [ ] **新配置表**: 无（经济参数已在 AutoChessDefine 中定义）
- [x] **跨进程/Fiber 通信**: 无（纯服务端本局逻辑）
- [x] **新数据模型类型**: EconomyLogComponent（ChildOf MatchPlayer）
- [x] **影响 system-map.md**: 新增 economy-service 节点，依赖 match-player + round-fsm

### 不影响的系统
- 商店（E3）— 将调用 EconomyService.DeductBuy/GiveSell，本 change 只提供接口
- 单位管理（E4）— 合成返还调用 EconomyService.GiveMerge
- 网络同步（E9）— ElixirChangedEvent 预留，E9 时接入
