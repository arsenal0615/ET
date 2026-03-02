# ET 游戏系统：数值组件 & AI 行为机

## 数值组件 (NumericComponent)

### KV 存储

用 `Dictionary<int, int>` 存储所有属性，key 是 `NumericType` 编码：

```csharp
public enum NumericType
{
    Speed = 1000,
    SpeedBase = Speed * 10 + 1,      // 10001 初始值
    SpeedAdd = Speed * 10 + 2,       // 10002 绝对加成
    SpeedPct = Speed * 10 + 3,       // 10003 百分比加成
    SpeedFinalAdd = Speed * 10 + 4,  // 10004 最终绝对加成
    SpeedFinalPct = Speed * 10 + 5,  // 10005 最终百分比加成

    Hp = 1001,
    HpBase = Hp * 10 + 1,
    // ...同理
}
```

### 统一计算公式

```
final = (((base + add) * (100 + pct) / 100) + finalAdd) * (100 + finalPct) / 100
```

通过 key 编码自动定位 5 个子属性：
```csharp
int final = (int)numericType / 10;
int bas = final * 10 + 1;  int add = final * 10 + 2;
int pct = final * 10 + 3;  int finalAdd = final * 10 + 4;  int finalPct = final * 10 + 5;
```

### Buff 配置

Buff 表只需填 `NumericType` key + value。创建 buff 加值，删除 buff 减值，自动重算。

### 数值变化事件

```csharp
[NumericWatcher(NumericType.Hp)]
public class NumericWatcher_Hp : INumericWatcher
{
    public void Run(long id, int value) { }
}
```

float 支持：乘以 10000 转整数存储，取值时除以 10000。

---

## AI 行为机

### 核心结构

AI 节点 = 条件判断 (Check) + 可取消协程 (Run)：

```csharp
public class AINode
{
    public virtual bool Check(Unit unit) { }
    public virtual ETVoid Run(Unit unit, ETCancelToken cancelToken) { }
}
```

### 运转逻辑

```csharp
while (true)
{
    await TimerComponent.Instance.Wait(1000); // 每秒检查
    AINode next = aiNodes.FirstOrDefault(n => n.Check());
    if (next == null || next == current) continue;
    cancelToken.Cancel();                      // 打断当前行为
    cancelToken = new ETCancelToken();
    current = next;
    next.Run(unit, cancelToken).Coroutine();   // 启动新行为
}
```

### 编码要点

1. **每个 await 必须传 cancelToken 并检查返回值**：`if (!ret) return;`
2. 节点不要拆太细：一个节点可以包含"选目标→追击→攻击"完整流程
3. 共用的是函数（MoveToAsync、CastSpell），不是节点
4. 防死循环：while 循环可能空转时加 `await Wait(100, cancelToken)`
