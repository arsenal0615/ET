# ET 事件系统

## 生命周期 System

所有事件支持多次订阅。System 类必须标注 `[EntitySystemOf]` + `[FriendOf]`，方法标注 `[EntitySystem]`：

```csharp
[EntitySystemOf(typeof(XxxComponent))]
[FriendOf(typeof(XxxComponent))]
public static partial class XxxComponentSystem
{
    [EntitySystem]
    private static void Awake(this XxxComponent self) { }  // 创建后触发一次

    [EntitySystem]
    private static void Destroy(this XxxComponent self) { }  // Dispose 时触发

    [EntitySystem]
    private static void Update(this XxxComponent self) { }  // 每帧触发
}
```

其他生命周期：`Start`（Update 前触发一次）、`Change`（手动触发）、`Deserialize`（反序列化后）、`Load`（DLL 热重载时）。

## 自定义事件

定义事件结构体 → 多个模块各自订阅：

```csharp
// 事件结构体
public struct HpChangeEvent
{
    public long UnitId;
    public int OldHp;
    public int NewHp;
}

// 订阅者（可以有多个）
[Event(SceneType.Client)]
public class HpChange_UpdateUI : AEvent<Scene, HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args) { }
}
```

用事件解耦模块：扣血只改数值 → 抛 HpChangeEvent → UI 模块和模型模块各自订阅处理。

## Invoke 机制

函数式调用，有返回值（区别于事件的通知模式）：
```csharp
[Invoke(SceneType.Client)]
public class SomeInvokeHandler : AInvokeHandler<SomeRequest, SomeResponse>
{
    public override SomeResponse Handle(SomeRequest args) { return new SomeResponse { }; }
}
```

定时器 Invoke：
```csharp
[Invoke(TimerInvokeType.MoveTimer)]
public class MoveTimer : ATimer<MoveComponent>
{
    protected override void Run(MoveComponent self) { }
}
```

## 消息处理器

```csharp
// 单向消息
[MessageHandler(SceneType.StateSync)]
public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
{
    protected override async ETTask Run(Scene root, M2C_PathfindingResult message) { }
}

// RPC（带响应）
[MessageSessionHandler(SceneType.Gate)]
public class C2G_EnterMapHandler : MessageSessionHandler<C2G_EnterMap, G2C_EnterMap>
{
    protected override async ETTask Run(Session session, C2G_EnterMap request, G2C_EnterMap response) { }
}
```

## 关键原则

- `[Event(SceneType.X)]` / `[MessageHandler(SceneType.X)]` 指定在哪类场景生效
- 所有 System/Handler 在 Hotfix 程序集中，支持热更新
- Source Generator 通过 `[EntitySystemOf]` + `[EntitySystem]` 自动生成样板代码
