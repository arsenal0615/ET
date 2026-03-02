# ET ECS 模式与事件系统

## Entity-Component 模式

Entity 是纯数据容器。逻辑通过独立 System 类中的**静态扩展方法**实现。

### 设计哲学

反对传统 OOP 继承：父类改字段影响所有子类、无法热插拔、方法与数据耦合。
组件模式优势：高度模块化、运行时热插拔、零耦合。

### 树状结构（区别于传统扁平 ECS）

ET 的 Entity-Component 是**树状**的：Entity 可挂 Component，Component 可管理 Entity，Component 还可挂 Component。

```csharp
Head head = human.AddComponent<Head>();
head.AddComponent<Eye>();
head.AddComponent<Nose>();
human.AddComponent<Body>();
```

### InstanceId 机制

每个 Entity 都有 `InstanceId`（2字节进程编号 + 4字节时间 + 2字节递增），全局唯一：
- **对象池安全**：异步回调中检查 InstanceId 是否变化，防止操作已回收对象
- **Actor 路由**：通过 InstanceId 定位对象所在进程

### 组件生命周期

- **创建**：`AddComponent<T>()` / `AddChild<T>()`，禁止 `new`（分析器强制），使用对象池
- **释放**：`Dispose()` → 触发 DestroySystem → 归还对象池 → InstanceId 置 0
- Entity 的 `Dispose()` 会递归释放所有子 Component

### 父子关系约束（分析器强制）

`[ComponentOf(typeof(Unit))]` 声明只能挂在 Unit 上，`[ChildOf(typeof(Player))]` 声明只能是 Player 的子实体。`AddComponent` / `AddChild` 类型不匹配则编译报错。

## 事件系统

### 数据驱动原则

不是直接在消息处理中修改所有相关模块，而是修改数据后抛出事件，各模块自行订阅处理。
例：扣血消息只改血值 → 抛出 HpChange 事件 → 头顶血条和 UI 血条各自订阅处理。

### 生命周期 System 事件

所有事件支持**多次订阅**。

| System | 触发时机 |
|--------|---------|
| **AwakeSystem** | 组件创建后，只触发一次，可带参数 |
| **DestroySystem** | 组件 Dispose 时 |
| **UpdateSystem** | 每帧触发（需组件实现 IUpdate） |
| **StartSystem** | Update 调用前触发一次 |
| **ChangeSystem** | 内容变化时触发（需手动调用） |
| **DeserializeSystem** | 反序列化后触发 |
| **LoadSystem** | DLL 热重载时触发 |

### 自定义事件 (Event)

```csharp
public struct HpChangeEvent { public long UnitId; public int OldHp; public int NewHp; }

[Event(SceneType.Client)]
public class HpChange_UpdateUI : AEvent<HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args) { }
}
```

### Invoke 机制（函数式调用，有返回值）

```csharp
[Invoke(SceneType.Client)]
public class SomeInvokeHandler : AInvokeHandler<SomeRequest, SomeResponse>
{
    public override SomeResponse Handle(SomeRequest args) { return new SomeResponse { }; }
}
```

### 关键原则

- SceneType 分发：通过 `[Event(SceneType.X)]` / `[MessageHandler(SceneType.X)]` 指定生效场景
- ET9 使用 `[EntitySystemOf]` + `[EntitySystem]`，由 Source Generator 自动生成样板代码
