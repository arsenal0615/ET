# ET 事件系统 (EventSystem)

## 核心理念：数据驱动逻辑

ECS 最重要的特性：数据与逻辑分离 + 数据驱动逻辑。

**什么是数据驱动**：不是直接在消息处理中修改所有相关模块，而是修改数据后抛出事件，各模块自行订阅处理。

**示例**：血量变化
- 错误做法：扣血消息中同时修改血值、更新头顶血条、更新 UI 血条（模块耦合）
- 正确做法：扣血消息只改血值 → 抛出 HpChange 事件 → 头顶血条模块和 UI 模块各自订阅处理（解耦）

## 生命周期 System 事件

所有事件都支持 **多次订阅**。

### AwakeSystem
组件创建后触发，只触发一次，可带参数：
```csharp
[EntitySystemOf(typeof(XxxComponent))]
public static partial class XxxComponentSystem
{
    [EntitySystem]
    private static void Awake(this XxxComponent self)
    {
        // 初始化逻辑
    }
}
```

### DestroySystem
组件 Dispose 时触发：
```csharp
[EntitySystem]
private static void Destroy(this XxxComponent self)
{
    // 清理逻辑
}
```

### UpdateSystem
每帧触发（需要组件注册了 Update）：
```csharp
[EntitySystem]
private static void Update(this XxxComponent self)
{
    // 每帧逻辑
}
```

### 其他生命周期
- **StartSystem**：Update 调用前触发一次
- **ChangeSystem**：内容变化时触发（需手动调用）
- **DeserializeSystem**：反序列化后触发
- **LoadSystem**：DLL 热重载时触发

## 自定义事件 (Event)

开发者自行定义和抛出的事件，支持多个模块订阅同一事件：
```csharp
// 定义事件结构
public struct HpChangeEvent
{
    public long UnitId;
    public int OldHp;
    public int NewHp;
}

// 订阅事件（可以有多个订阅者）
[Event(SceneType.Client)]
public class HpChange_UpdateUI : AEvent<HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args)
    {
        // 更新 UI 血条
    }
}

[Event(SceneType.Client)]
public class HpChange_UpdateModel : AEvent<HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args)
    {
        // 更新头顶血条
    }
}
```

## 消息处理事件

网络消息的处理也是事件机制的一种：
```csharp
[MessageHandler(SceneType.Gate)]
public class C2G_LoginGateHandler : MessageHandler<C2G_LoginGate, G2C_LoginGate>
{
    protected override async ETTask Run(Session session, C2G_LoginGate request, G2C_LoginGate response)
    {
        // 处理登录网关消息
    }
}
```

## Invoke 机制

函数式调用，有返回值，区别于事件的"通知"模式：
```csharp
[Invoke(SceneType.Client)]
public class SomeInvokeHandler : AInvokeHandler<SomeRequest, SomeResponse>
{
    public override SomeResponse Handle(SomeRequest args)
    {
        return new SomeResponse { ... };
    }
}
```

## 关键设计原则

1. **事件解耦**：模块之间通过事件通信，不直接调用
2. **SceneType 分发**：事件/消息处理器通过 `[Event(SceneType.X)]` / `[MessageHandler(SceneType.X)]` 指定在哪类场景生效
3. **热更友好**：所有 System/Handler 都在 Hotfix 程序集中，支持热更新
4. **ET9 使用 [EntitySystemOf] + [EntitySystem] 特性**，由 Source Generator 自动生成样板代码
