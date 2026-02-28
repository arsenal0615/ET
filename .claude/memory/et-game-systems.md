# ET 游戏系统：数值组件 & AI 行为机

---

## 一、数值组件 (NumericComponent)

### 设计动机
MMO/MOBA 游戏属性极多（速度、力量、血量、魔法等几十种），每种属性还受 buff 影响（加绝对值、加百分比、最终加成等）。传统做法（每个属性一个字段 + 继承）会导致类爆炸、公式重复、配置困难。

### KV 设计方案
ET 用 `Dictionary<int, int>` 存储所有数值，key 是 `NumericType` 枚举值：

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

    MaxHp = 1002,
    MaxHpBase = MaxHp * 10 + 1,
    MaxHpAdd = MaxHp * 10 + 2,
    MaxHpPct = MaxHp * 10 + 3,
    MaxHpFinalAdd = MaxHp * 10 + 4,
    MaxHpFinalPct = MaxHp * 10 + 5,
}
```

### 统一计算公式
所有属性共用 **一条公式**，无需为每种属性单独写计算：
```
final = (((base + add) * (100 + pct) / 100) + finalAdd) * (100 + finalPct) / 100
```

key 的编码规则使得公式可以通过数学运算自动定位 5 个子属性：
```csharp
int final = (int)numericType / 10;
int bas = final * 10 + 1;
int add = final * 10 + 2;
int pct = final * 10 + 3;
int finalAdd = final * 10 + 4;
int finalPct = final * 10 + 5;
```

### Buff 配置方式
Buff 表中只需填 `NumericType` 的 key 值：
- 疾跑 buff（+50% 速度）→ 配置 key=SpeedPct, value=50
- 力量 buff（+10 攻击）→ 配置 key=AttackAdd, value=10
- Buff 创建时加值，Buff 删除时减值，自动触发 Update 重新计算

### 数值变化事件
属性变化会自动抛出事件，其他模块可订阅：
```csharp
[NumericWatcher(NumericType.Hp)]
public class NumericWatcher_Hp : INumericWatcher
{
    public void Run(long id, int value)
    {
        if (value > 1000)
        {
            // 获得"长寿大师"成就
        }
    }
}
```

### 设计优势总结
1. **灵活**：法师没能量就不加能量 key，盗贼没法力就不加法力 key
2. **统一**：一条公式覆盖所有属性计算
3. **易配置**：Buff 表直接填 NumericType 枚举值
4. **可观测**：任何属性变化都能被订阅监控
5. **float 支持**：乘以 10000 转整数存储，取值时除以 10000

---

## 二、AI 框架 —— 行为机 (Behavior Machine)

### 状态机和行为树的缺陷

**状态机**：
- 节点间转换是网状结构，复杂度 O(N²)
- 状态多了难以维护，分层状态机也只是打补丁

**行为树**：
- 复杂度 O(N)，优于状态机
- 但 AI 复杂时树极大，难以重构
- 持续性行为（移动等协程）处理尴尬

### 行为机的核心思想

**AI 的本质**：不停地根据当前状态，执行相应的行为。

两个要素：
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
        if (node.Check())
        {
            next = node;
            break; // 找到第一个满足条件的节点
        }
    }

    if (next == null || next == current) continue;

    cancelToken.Cancel();                          // 打断当前行为
    cancelToken = new ETCancelToken();
    current = next;
    next.Run(unit, cancelToken).Coroutine();       // 启动新行为
}
```

### 怪物 AI 示例

**定义行为**（根据策划案的字面意思直接定义）：
- 巡逻：出生点附近随机移动
- 追击并攻击：发现敌人后追过去打
- 返回出生点：离出生点太远则无敌回跑

**填充条件**：
- 巡逻条件：没有目标 + 周围没敌人 + 距出生点 < 10米
- 追击攻击条件：周围有敌人 + 距出生点 < 10米
- 返回条件：距出生点 > 20米

**实现行为**（每个都是协程）：
```csharp
// 巡逻伪代码
while (true)
{
    pos = 出生点周围找一个点;
    bool ret = await MoveToAsync(pos, cancelToken);
    if (!ret) return;
    ret = await TimerComponent.Instance.Wait(RandomHelper.Random(2000, 4000), cancelToken);
    if (!ret) return;
}
```

### 行为机的关键原则

1. **节点不共用**：行为机的节点只是一段逻辑，可以非常庞大（如"自动做任务"一个节点）
2. **共用的是函数**：MoveToAsync、CastSpell 等函数被多个节点调用
3. **只关注当前行为**：永远不需要关心上一个行为是什么，满足条件就直接打断+切换
4. **节点不要拆太细**：不要把"选择目标""追击""攻击"拆成三个节点，合在一起更清晰
5. **协程必须可取消**：Run 方法中每个 await 都要传 cancelToken 并检查返回值
6. **防死循环**：协程中如果可能出现空转（while 循环没进入任何 await），要加一个短暂的 Wait

### 扩展：压测机器人

把"自动做任务""自动打怪""自动玩系统""自动聊天"等每个做成一个 AI 节点，就是一个完整的压测机器人。
