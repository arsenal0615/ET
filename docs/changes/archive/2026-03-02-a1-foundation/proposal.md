## Why

A1 自走棋项目需要一套基础框架作为所有玩法系统的地基。当前 ET 项目只有 statesync 演示包，没有自走棋相关的代码。需要从零建立：

1. **对局数据模型** — 定义自走棋核心数据结构（单位模板、羁绊、技能、游戏常量）
2. **回合状态机** — 驱动 5 阶段回合循环（RoundStart→Deployment→PreBattle→Battle→RoundEnd）
3. **确定性 PRNG** — 所有随机行为可重放，这是战斗确定性的前提
4. **对局生命周期** — Match 创建/运行/结束的完整管理

这 4 块是 GDD 中 E1 基础框架（Stories 1.1~1.4），是依赖链的最上游。不做这个，后续经济/商店/单位/羁绊/战斗全部无法开始。

## What Changes

### 新建包

- 新建 `cn.etetet.autochess` 包，遵循 ET 包系统规范
- 四程序集分离：Model(Share/Server/Client) + Hotfix(Share/Server/Client)

### 新增 Component/Entity

- `MatchComponent` — 挂在 Scene 上，管理所有活跃对局
- `MatchRoom` — 单局对局实体，ChildOf MatchComponent
- `MatchPlayer` — 对局内玩家状态，ChildOf MatchRoom
- `RoundFSMComponent` — 回合状态机，ComponentOf MatchRoom
- `DeterministicRngComponent` — 确定性 PRNG，ComponentOf MatchRoom

### 新增配置表

- `UnitTemplateConfig` — 24 个单位模板（费用/属性/标签/技能引用）
- `SynergyConfig` — 13 种羁绊定义（阈值/效果参数）
- `SkillDefConfig` — 16 种技能 Cell 组合定义
- `GameConstantsConfig` — 游戏常量（阶段时长/人口上限/经济参数）

### 新增 Proto 消息

- 对局管理消息（进入/离开/状态同步）
- 阶段变更通知
- 暂不定义操作消息（经济/商店/单位操作留给后续 change）

### 新增 System/Handler

- MatchComponentSystem — 创建/销毁对局
- MatchRoomSystem — 对局初始化/回合驱动
- RoundFSMComponentSystem — 阶段切换/定时器/操作门控
- DeterministicRngComponentSystem — xxHash32 子种子 + xoshiro256** 步进

## Capabilities

### New Capabilities

- `match-lifecycle`: 对局创建、初始化、运行、结束、清理的完整生命周期
- `round-state-machine`: 5 阶段回合循环驱动 + 操作门控（PhaseGate）
- `deterministic-prng`: 固定种子 PRNG + 子种子派生 + 确定性保证
- `autochess-data-model`: 单位模板/羁绊/技能/常量的配置表和运行时数据结构

### Modified Capabilities

无。这是全新模块。

## Impact

### 框架影响清单

- [x] **模块/程序集受影响**: 新建 `cn.etetet.autochess` 包（Model + Hotfix，各含 Share/Server/Client）
- [x] **新协议/消息定义**: 需定义 AutoChess Proto 文件（Opcode 范围待分配，建议 12001-12999 外部 / 22001-22999 内部）
- [x] **新配置表**: UnitTemplate / Synergy / SkillDef / GameConstants（4 个 Excel）
- [x] **跨进程/Fiber 通信**: Match 可能运行在独立 Fiber（类似 Map），需 Actor 消息
- [x] **新数据模型类型**: MatchRoom(ChildOf:MatchComponent) / MatchPlayer(ChildOf:MatchRoom) / 5+ Components
- [x] **影响 system-map.md**: 新增 `cn.etetet.autochess` 系统节点及其依赖

### 新增 SceneType

| SceneType | 建议值 | 说明 |
|-----------|--------|------|
| AutoChessMatch | 10030 | 自走棋对局场景（服务端） |
| AutoChessClient | 10031 | 自走棋客户端根场景 |

### 不影响的系统

- 登录系统（login）— 复用现有流程
- 移动系统（move）— 自走棋不使用连续移动
- AOI 系统（aoi）— 自走棋使用棋盘格，不需 AOI
- AI 系统（ai）— 后续单独做 AI 对手，不在本 change 范围
