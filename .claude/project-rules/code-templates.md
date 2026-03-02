# ET 代码模板与速查

## Component 模板（Model 程序集）

```csharp
[ComponentOf(typeof(ParentEntity))]
public class MyComponent : Entity, IAwake, IDestroy
{
    public int MyField;
}
```

## System 模板（Hotfix 程序集）

```csharp
[EntitySystemOf(typeof(MyComponent))]
[FriendOf(typeof(MyComponent))]
public static partial class MyComponentSystem
{
    [EntitySystem]
    private static void Awake(this MyComponent self) { }
    [EntitySystem]
    private static void Destroy(this MyComponent self) { }
    public static void MyMethod(this MyComponent self) { }
}
```

## 消息处理器模板（Hotfix 程序集）

```csharp
[MessageSessionHandler(SceneType.Gate)]
public class C2G_MyRequestHandler : MessageSessionHandler<C2G_MyRequest, G2C_MyResponse>
{
    protected override async ETTask Run(Session session, C2G_MyRequest request, G2C_MyResponse response) { }
}
```

## 事件处理器模板（Hotfix 程序集）

```csharp
[Event(SceneType.Client)]
public class HpChange_UpdateUI : AEvent<Scene, HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args) { }
}
```

## 关键属性速查

| 属性 | 用途 |
|------|------|
| `[ComponentOf(typeof(Parent))]` | 声明 Component 所属的 Entity 类型 |
| `[ChildOf(typeof(Parent))]` | 声明父子 Entity 关系 |
| `[FriendOf(typeof(Entity))]` | 授予对 Entity 私有字段的访问权限 |
| `[EntitySystemOf(typeof(Entity))]` | 自动生成生命周期 System 样板代码 |
| `[EntitySystem]` | 标记 Awake/Destroy/Update 方法 |
| `[Event(SceneType.X)]` | 事件处理器注册 |
| `[MessageHandler(SceneType.X)]` | 网络消息处理器 |
| `[MessageSessionHandler(SceneType.X)]` | RPC 处理器 |
| `[Invoke(SceneType.X)]` | 带返回值的函数式调用 |
| `[EnableMethod]` | 允许 Entity 类内定义方法（例外情况） |

## 文件命名规范

| 类型 | 格式 | 示例 |
|------|------|------|
| Component | `{Name}Component.cs` | `MoveComponent.cs` |
| System | `{Component}System.cs` | `MoveComponentSystem.cs` |
| 消息处理器 | `{MessageName}Handler.cs` | `C2G_EnterMapHandler.cs` |
| 事件处理器 | `{Event}_{Action}.cs` | `ChangePosition_NotifyAOI.cs` |
| 工厂 | `{Entity}Factory.cs` | `UnitFactory.cs` |
| 辅助类 | `{Entity}Helper.cs` | `UnitHelper.cs` |

## 常量 ID 公式

- `SceneType = PackageType * 1000 + offset`
- `TimerInvokeType = PackageType * 1000 + offset`
