# ET 游戏系统参考

## 数值组件 (NumericComponent)

### KV 设计

用 `Dictionary<int, int>` 存储所有数值，key 是 `NumericType` 枚举值：

```csharp
public enum NumericType
{
    Max = 10000,

    Speed = 1000,
    SpeedBase = Speed * 10 + 1,      // 10001 初始值
    SpeedAdd = Speed * 10 + 2,       // 10002 绝对加成
    SpeedPct = Speed * 10 + 3,       // 10003 百分比加成
    SpeedFinalAdd = Speed * 10 + 4,  // 10004 最终绝对加成
    SpeedFinalPct = Speed * 10 + 5,  // 10005 最终百分比加成

    Hp = 1001,
    HpBase = Hp * 10 + 1,
    // ...
}
```

### 统一计算公式

所有属性共用一条公式：

```
final = (((base + add) * (100 + pct) / 100) + finalAdd) * (100 + finalPct) / 100
```

key 编码规则自动定位 5 个子属性：

```csharp
int final = (int)numericType / 10;
int bas = final * 10 + 1;     // Base
int add = final * 10 + 2;     // Add
int pct = final * 10 + 3;     // Pct
int finalAdd = final * 10 + 4; // FinalAdd
int finalPct = final * 10 + 5; // FinalPct
```

### Buff 配置方式

Buff 表中只需填 `NumericType` 的 key 值：
- 疾跑 buff（+50% 速度）→ key=SpeedPct, value=50
- 力量 buff（+10 攻击）→ key=AttackAdd, value=10
- Buff 创建时加值，删除时减值，自动触发重新计算

### 数值变化事件

```csharp
[NumericWatcher(NumericType.Hp)]
public class NumericWatcher_Hp : INumericWatcher
{
    public void Run(long id, int value) { /* 响应血量变化 */ }
}
```

### 设计要点

- float 支持：乘以 10000 转整数存储，取值时除以 10000
- 灵活性：法师没能量就不加能量 key，按需挂载
- 可观测：任何属性变化都能被 NumericWatcher 订阅

---

## AI 框架 — 行为机 (Behavior Machine)

### 核心思想

AI 本质：不停地根据当前状态，执行相应的行为。两个要素：
1. **条件判断**（Check）：当前状态是否满足执行条件
2. **执行行为**（Run）：一个可取消的协程

```csharp
public class AINode
{
    public virtual bool Check(Unit unit) { }
    public virtual ETVoid Run(Unit unit, ETCancelToken cancelToken) { }
}
```

### 行为机运转逻辑

```csharp
AINode[] aiNodes = {xunLuoNode, gongjiNode, fanHuiNode};
AINode current;
ETCancelToken cancelToken;

while (true)
{
    await TimerComponent.Instance.Wait(1000); // 每秒检查一次
    AINode next = null;
    foreach (var node in aiNodes)
    {
        if (node.Check()) { next = node; break; }
    }
    if (next == null || next == current) continue;

    cancelToken.Cancel();                          // 打断当前行为
    cancelToken = new ETCancelToken();
    current = next;
    next.Run(unit, cancelToken).Coroutine();       // 启动新行为
}
```

### 关键原则

1. **节点不共用**：一个节点可以是一段庞大逻辑（如"自动做任务"）
2. **共用的是函数**：MoveToAsync、CastSpell 等被多个节点调用
3. **只关注当前行为**：满足条件就直接打断+切换，不关心上一个行为
4. **节点不要拆太细**：不要把"选择目标""追击""攻击"拆成三个节点
5. **协程必须可取消**：Run 中每个 await 都要传 cancelToken 并检查返回值
6. **防死循环**：while 循环中如果可能空转，要加短暂 Wait
