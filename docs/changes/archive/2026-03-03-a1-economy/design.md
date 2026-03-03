# Technical Design: a1-economy

## 技术决策（TD）

### TD-1：EconomyService 作为纯静态服务类

圣水变更不挂载在任何 Entity/System 上，而是一个带 `[FriendOf]` 的纯静态类。

**理由：**
- 圣水变更在多处触发（RoundStart/买/卖/合成/淘汰），不属于任何单一 Entity 的生命周期
- 静态服务类 = 单一入口，方便单测和后续审计
- 符合 ET 框架"逻辑写在静态类"的 analyzer 规则

**结构：**
```
EconomyService（Hotfix/Server，namespace ET.Server）
├── [FriendOf(typeof(MatchPlayer))]
├── [FriendOf(typeof(EconomyLogComponent))]
├── GiveRoundIncome(MatchPlayer player, int round)        → +4
├── GiveCommanderPassive(MatchPlayer player, DeterministicRngComponent rng, int round) → +randInt(4,8)
├── TryDeductBuy(MatchPlayer player, int cost, int round) → bool，失败不扣
├── GiveSell(MatchPlayer player, int cost, int round)     → +max(cost-1, 0)
├── GiveMerge(MatchPlayer player, int round)              → +1
└── ApplyDelta(player, type, amount, round) [private]    → clamp + log + event
```

### TD-2：EconomyLogComponent 作为 ComponentOf(MatchPlayer)

每个 MatchPlayer 持有一个 EconomyLogComponent，记录本局所有圣水变动。

**EconomyDelta（Model/Share，struct）：**
```csharp
public struct EconomyDelta
{
    public EconomyDeltaType Type;   // 枚举：见下
    public int Amount;              // 正数=收入，负数=支出
    public int Round;               // 触发回合
    public string Context;          // 可选上下文（如 "unitId=5"）
}

public enum EconomyDeltaType
{
    RoundIncome = 1,
    CommanderPassive = 2,
    Buy = 3,
    Sell = 4,
    Merge = 5,
}
```

**EconomyLogComponent（Model/Server）：**
```csharp
[ComponentOf(typeof(MatchPlayer))]
public class EconomyLogComponent : Entity, IAwake, IDestroy
{
    public List<EconomyDelta> Deltas;
}
```

**EconomyLogComponentSystem（Hotfix/Server）：**
- Awake：初始化 Deltas = new List<EconomyDelta>()
- Destroy：Deltas = null
- 公开 `AddDelta(EconomyDelta delta)`
- 公开 `GetDeltasByRound(int round)` → IEnumerable<EconomyDelta>（LINQ where）

### TD-3：LastRoundLosers 添加到 MatchRoom

统帅被动在 RoundStart 发放给"上回合战败者"。需要将战斗结算结果（E5 写入）存储在 MatchRoom。

**修改 MatchRoom（Model/Server）：**
```csharp
public List<long> LastRoundLosers;  // 上回合战败的 PlayerId 列表，E5 写入，E2 读取
```

初始化：`Awake` 中 `LastRoundLosers = new List<long>()`，`Destroy` 中置 null。

**E2 和 E5 的解耦：**
- E2 只读取 `LastRoundLosers`，不写入
- E5（战斗结算）写入：`room.LastRoundLosers.Clear(); room.LastRoundLosers.Add(loserId);`
- Round 1 时 LastRoundLosers 为空 → 无统帅被动 → 符合 GDD（首回合无战斗）

### TD-4：经济触发点 — PhaseChangedEvent Handler

使用 ET 事件系统，在 RoundStart 阶段开始时（PhaseChangedEvent 触发时）发放收入，而非修改 RoundFSMComponent 内部逻辑。

**理由：**
- PhaseChangedEvent 在 `SwitchPhaseAsync` 中 `await PublishAsync` — 先于阶段计时器运行
- 经济逻辑与状态机解耦，FSM 无需知道经济系统
- 后续添加其他阶段触发逻辑（E9 网络广播、E6 战斗结算）都可以通过 handler 追加

**Handler 结构（Hotfix/Server）：**
```csharp
// Scene type 需要在实现时确认（MatchComponent 挂载的 Scene 类型）
[Event(SceneType.Server)]
public class PhaseChangedEventHandler_Economy : AEvent<Scene, PhaseChangedEvent>
{
    protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
    {
        if (args.NewPhase != RoundPhase.RoundStart) return;

        MatchComponent matchComp = scene.GetComponent<MatchComponent>();
        MatchRoom room = matchComp?.GetRoom(args.MatchRoomId);
        if (room == null) { await ETTask.CompletedTask; return; }

        DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
        int round = args.Round;

        // 1. 所有存活玩家 +4 圣水
        foreach (MatchPlayer player in room.GetAlivePlayers())
            EconomyService.GiveRoundIncome(player, round);

        // 2. 上回合战败者统帅被动（round > 1 且有战斗结果）
        if (round > 1 && rng != null)
        {
            foreach (long loserId in room.LastRoundLosers)
            {
                MatchPlayer loser = room.FindPlayerById(loserId);
                if (loser?.IsAlive == true)
                    EconomyService.GiveCommanderPassive(loser, rng, round);
            }
        }
    }
}
```

**需要在 MatchRoomSystem 中追加两个辅助方法：**
- `GetAlivePlayers()` → IEnumerable<MatchPlayer>（遍历 Children.Values）
- `FindPlayerById(long playerId)` → MatchPlayer?

### TD-5：ElixirChangedEvent（Model/Share/Events）

```csharp
public struct ElixirChangedEvent
{
    public long PlayerId;
    public int OldValue;
    public int NewValue;
}
```

在 `EconomyService.ApplyDelta` 末尾同步发布（`EventSystem.Instance.Publish`，非 async）。
E9 网络层接入时订阅此事件向客户端推送。

### TD-6：AutoChessDefine 追加经济常量

不新建文件，直接追加到现有 `AutoChessDefine.cs`：

```csharp
// 经济
public const int RoundBaseIncome = 4;
public const int CommanderPassiveMin = 4;
public const int CommanderPassiveMax = 8;
public const int MergeElixirReturn = 1;
// SellReturn = Math.Max(cost - 1, 0)，不需要常量
```

---

## 文件清单

### 新增（Model/Share）
| 文件 | 内容 |
|------|------|
| `Model/Share/AutoChess/EconomyDeltaType.cs` | enum EconomyDeltaType（5 值） |
| `Model/Share/AutoChess/EconomyDelta.cs` | struct EconomyDelta（4 字段） |
| `Model/Share/AutoChess/Events/ElixirChangedEvent.cs` | struct ElixirChangedEvent（3 字段） |

### 新增（Model/Server）
| 文件 | 内容 |
|------|------|
| `Model/Server/AutoChess/EconomyLogComponent.cs` | ComponentOf(MatchPlayer)，List<EconomyDelta> |

### 新增（Hotfix/Server）
| 文件 | 内容 |
|------|------|
| `Hotfix/Server/AutoChess/EconomyLogComponentSystem.cs` | Awake/Destroy + AddDelta + GetDeltasByRound |
| `Hotfix/Server/AutoChess/EconomyService.cs` | 5 个收支方法 + ApplyDelta |
| `Hotfix/Server/AutoChess/PhaseChangedEventHandler_Economy.cs` | AEvent handler，RoundStart 触发收入 |

### 修改（已存在）
| 文件 | 修改内容 |
|------|----------|
| `Model/Share/AutoChess/AutoChessDefine.cs` | 追加 4 个经济常量 |
| `Model/Server/AutoChess/MatchRoom.cs` | 追加 `List<long> LastRoundLosers` |
| `Hotfix/Server/AutoChess/MatchRoomSystem.cs` | 追加 GetAlivePlayers()、FindPlayerById() 辅助方法；Awake/Destroy 初始化 LastRoundLosers |
| `Hotfix/Server/AutoChess/MatchRoomFactory.cs` | 修改 Init 流程：为每个 MatchPlayer 添加 EconomyLogComponent |

---

## 完成标准

- [ ] ET.Model、ET.Hotfix 编译 0 错误 0 警告
- [ ] EconomyService 所有 5 个方法单测通过（直接调用，验证 Elixir 变化量和 delta 记录）
- [ ] GiveCommanderPassive 使用 PrngPurpose.CommanderPassive 子种子，同 seed 结果一致
- [ ] Round 1 不触发统帅被动（LastRoundLosers 为空）
- [ ] clamp：Elixir 不超过 MaxElixir(999)，不低于 0
- [ ] EconomyLogComponent.GetDeltasByRound 返回对应回合的全部记录
- [ ] PhaseChangedEventHandler_Economy 仅在 RoundPhase.RoundStart 时触发（其他阶段无副作用）
- [ ] ElixirChangedEvent 在每次 Elixir 变化时发布（Publish，非 async）
