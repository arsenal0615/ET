## Context

ET9 框架提供了 ECS + Fiber + Actor 的基础架构和 statesync 演示包。A1 自走棋需要在此基础上新建 `cn.etetet.autochess` 包，实现对局管理、回合状态机、确定性 PRNG 和配置表数据模型。

本 change 是依赖链最上游，后续经济/商店/卡池/单位/羁绊/技能/战斗全部依赖这里的基础结构。

## Goals / Non-Goals

**Goals:**
- 建立自走棋包的骨架结构（目录/程序集/命名空间）
- 定义核心数据模型（配置表 + 运行时 Component）
- 实现回合状态机（5 阶段循环 + 操作门控 + 定时器）
- 实现确定性 PRNG（xxHash32 子种子 + xoshiro256**）
- 实现 Match 生命周期（创建→运行→结束→清理）
- 可跑通：4 个 MatchPlayer 进入对局 → 回合循环自动推进 → 终局结束

**Non-Goals:**
- 不实现经济/商店/卡池（E2/E3）
- 不实现单位摆位/合成（E4）
- 不实现羁绊/技能/战斗（E5/E6/E7）
- 不实现客户端 UI
- 不实现网络消息处理（本 change 可先用单进程测试验证）

## Decisions

### Decision 1: 包名与目录结构

**Choice:** `cn.etetet.autochess`，遵循 ET 包系统标准结构

```
Packages/cn.etetet.autochess/
  Scripts/
    Model/
      Share/AutoChess/      ← 双端共用 Component + 枚举 + 常量
      Server/AutoChess/     ← 服务端 Component
      Client/AutoChess/     ← 客户端 Component（本 change 暂空）
    Hotfix/
      Share/AutoChess/      ← 双端共用 System（PRNG）
      Server/AutoChess/     ← 服务端 System/Handler
      Client/AutoChess/     ← 客户端 Handler（本 change 暂空）
    ModelView/AutoChess/    ← 客户端 View 数据（本 change 暂空）
    HotfixView/AutoChess/   ← 客户端 View 逻辑（本 change 暂空）
  Config/                   ← Excel 配置表
```

**Rationale:** 遵循 ET 现有包规范（参考 cn.etetet.statesync）。自走棋模块足够大，独立成包利于管理。

### Decision 2: Entity 树结构

**Choice:**

```
Scene (SceneType.AutoChessMatch)
  └── MatchComponent [ComponentOf: Scene]
        └── MatchRoom [ChildOf: MatchComponent]
              ├── RoundFSMComponent [ComponentOf: MatchRoom]
              ├── DeterministicRngComponent [ComponentOf: MatchRoom]
              └── MatchPlayer [ChildOf: MatchRoom] × 4
                    ├── elixir, hp, popCap 等字段
                    └── (后续 change 添加: BoardComponent, BenchComponent, ShopComponent)
```

**Alternatives considered:**
- A) MatchRoom 直接挂 Scene → 不利于多 Match 并行
- B) 每个 Player 独立 Fiber → 过重，自走棋单局 4 人数据量小

**Rationale:** MatchRoom 作为 Child 挂在 MatchComponent 下，支持一个 Scene 承载多局。MatchPlayer 作为 Child 挂在 MatchRoom 下，保持树状结构清晰。所有对局操作在同一 Fiber 内串行处理，天然序列化，无并发问题。

### Decision 3: 回合状态机实现

**Choice:** 5 状态 + ETCancelToken 驱动的协程式状态机

```
枚举: RoundPhase { None, RoundStart, Deployment, PreBattle, Battle, RoundEnd }

切换流程:
  RoundStart(2s) → Deployment(20s) → PreBattle(3s) → Battle(30s) → RoundEnd(3s) → 下一轮 RoundStart
```

每个阶段用 `TimerComponent.Instance.WaitAsync(duration, cancelToken)` 控制时长。阶段切换时：
1. Cancel 当前阶段的 ETCancelToken
2. 创建新 ETCancelToken
3. 发布 PhaseChangedEvent
4. 运行新阶段逻辑

**操作门控（PhaseGate）：** 静态方法 `RoundFSMComponentSystem.CanOperate(phase, operationType)` 返回 bool，基于操作矩阵表判定。

**Alternatives considered:**
- A) 用 ET AI 行为机框架 → 行为机是条件触发式，状态机是时序驱动式，不匹配
- B) IUpdate 每帧轮询 → 不够精确，且浪费性能

**Rationale:** 协程式（async ETTask）最契合 ET 的异步模型，且时长精确可控。

### Decision 4: PRNG 算法选择

**Choice:** xxHash32 做子种子派生 + xoshiro256** 做步进

**子种子派生:**
```csharp
uint SubSeed(uint matchSeed, int round, PrngPurpose purpose)
    => XxHash32(matchSeed, round, (int)purpose);

enum PrngPurpose { ShopReroll=1, Pairing=2, Combat=3, CommanderPassive=4, GoblinGift=5, FirstGift=6 }
```

**PRNG 步进:** xoshiro256** — 4×ulong 状态，用 SplitMix64 从 seed 初始化

**Alternatives considered:**
- A) PCG → C# 没有广泛使用的标准实现
- B) Mulberry32 → 只有 32bit 状态，周期太短
- C) System.Random with seed → 不保证跨平台一致

**Rationale:** xxHash32 在 .NET 8 有 `System.IO.Hashing.XxHash32` 可用，但为了 Share 程序集（客户端也能用），自实现一个纯 C# 版本更安全。xoshiro256** 是高质量 PRNG，分布均匀，周期 2^256-1。

### Decision 5: 配置表方案

**Choice:** 本 change 先用手写常量类（`AutoChessDefine`）+ 手写配置类（`UnitTemplateDef` 等），**不走 Excel 导出**。

**Rationale:** Excel 导出流程（`ET.ExcelExporter`）需要建表、配置 column 映射、运行导出工具。在基础框架阶段，数据结构还在快速迭代，手写更灵活。等数据结构稳定后（E4/E5 完成后），再迁移到 Excel。

**手写配置结构:**
```csharp
// Model/Share/AutoChess/
public static class AutoChessDefine { ... }       // 游戏常量
public class UnitTemplateDef { ... }               // 单位模板（24个）
public class SynergyDef { ... }                    // 羁绊定义（13个）
public static class AutoChessConfigLoader { ... }  // 加载/注册
```

### Decision 6: SceneType 分配

**Choice:**

| SceneType | 值 | 说明 |
|-----------|-----|------|
| AutoChessMatch | 10030 | 自走棋对局场景（服务端，一个 Fiber 一局或多局） |

**Rationale:** 现有 SceneType 最大值是 10021（Current），10030 留出缓冲。客户端 SceneType 留给 UI change 时再定。

## Framework-Specific Decisions

### Entity 所有权声明

```csharp
[ComponentOf(typeof(Scene))]       public class MatchComponent
[ChildOf(typeof(MatchComponent))]  public class MatchRoom
[ChildOf(typeof(MatchRoom))]       public class MatchPlayer
[ComponentOf(typeof(MatchRoom))]   public class RoundFSMComponent
[ComponentOf(typeof(MatchRoom))]   public class DeterministicRngComponent
```

### Fiber 模型

MVP 阶段：所有对局运行在同一个 Map Fiber 中。如果性能不足，后续可拆分为每局独立 Fiber（只需改创建逻辑，不影响业务代码）。

### 异步安全

所有 async 方法遵循 ET 三项纪律：
1. 返回 `ETTask` / `ETTask<T>`
2. 每个 `await` 后检查 `self.InstanceId != instanceId`
3. 传入 `ETCancelToken` 并处理取消

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 手写配置后续迁移 Excel 有工作量 | 数据结构尽量与 Excel 格式对齐，迁移时只改加载逻辑 |
| 单 Fiber 多 Match 可能性能瓶颈 | 4 人局计算量很小，MVP 够用；后续可拆 |
| PRNG 自实现可能有 bug | 写金标测试：同 seed 100 次输出一致 |
| 回合状态机与网络消息的集成 | 本 change 不做网络，用单元测试验证状态机正确性 |
