## 为什么

自走棋 MVP 的核心后端系统（E1-E8）已全部完成。所有游戏逻辑（经济、商店、单位、羁绊、技能、战斗模拟、配对、结算）均在服务端 Map Fiber 内运行，但目前没有任何客户端-服务端通信协议。

E9 是将单机逻辑接入多人在线的桥梁，也是 E10（UI）和 E11（断线兜底）的前提。

## 变更内容

- 定义 autochess 专用 Proto 消息（C2M 操作意图 + M2C 状态推送）
- 实现服务端操作 Handler（Buy/Sell/Place/Swap），含 PhaseGate 门控和校验
- 实现服务端状态广播（阶段切换、操作结果、战斗事件流、回合结算、对局结束）
- 实现客户端消息接收 Handler（接收阶段切换、操作反馈、战斗事件、结算信息）
- 建立对局加入流程（玩家从 Gate 进入 Map，绑定 MatchPlayer）

## Capabilities

### 新增 Capabilities
- `autochess-proto`: 自走棋 Proto 消息定义（操作意图 C2M + 状态推送 M2C + 服务端间 Inner）
- `autochess-op-handlers`: 服务端操作 Handler（Buy/Sell/Place/Swap），PhaseGate 门控 + 校验 + 错误码
- `autochess-state-broadcast`: 服务端主动广播（PhaseChange/CombatEvents/RoundResult/MatchResult），通过 MailBox GateSession 推送
- `autochess-client-handlers`: 客户端消息接收 Handler（接收并缓存服务端推送的状态数据）
- `autochess-match-join`: 对局加入流程（匹配完成 → 创建 Map Unit → 绑定 MatchPlayer → 下发初始状态）

## 影响

### 框架影响检查清单
- [x] **受影响的模块/程序集**: cn.etetet.autochess（全部四个程序集：Model/Share, Model/Server, Hotfix/Server, Hotfix/Client）
- [x] **需要新的协议/消息定义？** 是，需新建 Proto 文件（AutoChessOuter + AutoChessInner）
- [ ] **需要新的配置/数据文件？** 否
- [x] **跨进程/跨线程通信？** 是，Client ↔ Gate ↔ Map 三层通信
- [x] **新的数据模型类型？** AutoChessUnit（Map 上的 Unit Entity，承载 MailBox）、操作错误码枚举
- [x] **影响 system-map.md？** 新增网络与同步章节

### 上游依赖（已完成）
- E1 基础框架：MatchRoom、RoundFSM、PRNG ✅
- E2 经济系统：EconomyService ✅
- E3 商店与卡池：ShopService ✅
- E4 单位系统：UnitService、RosterService ✅
- E5 羁绊系统：SynergyComponent ✅
- E6 技能系统：SkillExecutor ✅
- E7+E8 战斗+结算：CombatSimulator、PairingService、SettlementService ✅

### 下游消费者（未来）
- E10 UI：客户端 Handler 解析后驱动 UI 展示
- E11 断线兜底：基于 Session 断开事件 + 超时认负
