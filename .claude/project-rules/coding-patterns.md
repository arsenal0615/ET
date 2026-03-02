# ET 编码模式参考

## Entity-Component Pattern

Entities are pure data containers. Logic is implemented as **static extension methods** in separate System classes.

### Component 模板（Model 程序集）

```csharp
[ComponentOf(typeof(ParentEntity))]
public class MyComponent : Entity, IAwake, IDestroy
{
    public int MyField;
}
```

### System 模板（Hotfix 程序集）

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

### 消息处理器模板（Hotfix 程序集）

```csharp
[MessageSessionHandler(SceneType.Gate)]
public class C2G_MyRequestHandler : MessageSessionHandler<C2G_MyRequest, G2C_MyResponse>
{
    protected override async ETTask Run(Session session, C2G_MyRequest request, G2C_MyResponse response) { }
}
```

### 事件处理器模板（Hotfix 程序集）

```csharp
[Event(SceneType.Client)]
public class HpChange_UpdateUI : AEvent<Scene, HpChangeEvent>
{
    protected override async ETTask Run(Scene scene, HpChangeEvent args) { }
}
```

## Key Attributes

| Attribute | Purpose |
|-----------|---------|
| `[ComponentOf(typeof(Parent))]` | Declares which Entity a Component belongs to |
| `[ChildOf(typeof(Parent))]` | Declares parent-child Entity relationship |
| `[FriendOf(typeof(Entity))]` | Grants access to Entity's private fields |
| `[EntitySystemOf(typeof(Entity))]` | Auto-generates lifecycle System boilerplate |
| `[EntitySystem]` | Marks Awake/Destroy/Update methods |
| `[Event(SceneType.X)]` | Event handler registration |
| `[MessageHandler(SceneType.X)]` | Network message handler |
| `[MessageSessionHandler(SceneType.X)]` | RPC handler |
| `[Invoke(SceneType.X)]` | Function-style call with return value |
| `[EnableMethod]` | Allows methods inside Entity class (exception) |

## File Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Component | `{Name}Component.cs` | `MoveComponent.cs` |
| System | `{Component}System.cs` | `MoveComponentSystem.cs` |
| Message Handler | `{MessageName}Handler.cs` | `C2G_EnterMapHandler.cs` |
| Event Handler | `{Event}_{Action}.cs` | `ChangePosition_NotifyAOI.cs` |
| Factory | `{Entity}Factory.cs` | `UnitFactory.cs` |
| Helper | `{Entity}Helper.cs` | `UnitHelper.cs` |

## Constant ID Formulas

- `SceneType = PackageType * 1000 + offset`
- `TimerInvokeType = PackageType * 1000 + offset`
