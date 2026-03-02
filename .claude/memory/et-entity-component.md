# ET 实体组件系统

## 树状结构（非扁平 ECS）

Entity 可以挂 Component，Component 可以管理子 Entity，子 Entity 还可以挂 Component：
```csharp
Head head = human.AddComponent<Head>();
head.AddComponent<Eye>();
head.AddComponent<Nose>();
human.AddComponent<Body>();
human.AddComponent<Hand>();
```

## 父子关系约束（编译期检查）

```csharp
[ComponentOf(typeof(Unit))]   // 只能挂在 Unit 上
public class SpellComponent : Entity, IAwake { }

[ChildOf(typeof(Player))]     // 只能是 Player 的子实体
public class Item : Entity { }
```

`AddComponent` / `AddChild` 的类型必须匹配声明，否则编译报错。

## InstanceId 机制

- **格式**：2字节进程编号 + 4字节时间 + 2字节递增，全局唯一
- **异步安全检查**：对象池复用后 InstanceId 变化，异步回调中必须检查：
```csharp
long instanceId = self.InstanceId;
await SomeAsyncOp();
if (self.InstanceId != instanceId)
    return; // 对象已释放或复用，终止
```
- **Actor 路由**：通过 InstanceId 定位对象所在进程

## 生命周期

**创建**：`AddComponent<T>()` / `AddChild<T>()` → 触发 AwakeSystem（禁止用 `new`）

**释放**：`Dispose()` → 触发 DestroySystem → 归还对象池 → InstanceId 置 0。父 Entity 的 Dispose 会递归释放所有子 Component。

## Entity 字段访问

Entity 字段默认私有，外部访问需要：
- System 类标注 `[FriendOf(typeof(XxxComponent))]`
- 或 Entity 自身的 System 类中直接访问
