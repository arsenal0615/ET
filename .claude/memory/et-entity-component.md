# ET 实体组件系统 (Entity-Component)

## 设计哲学

### 为什么不用面向对象
ET 作者明确反对在游戏开发中使用传统面向对象继承：
1. **数据结构耦合**：父类改字段影响所有子类，继承层次频繁调整是噩梦
2. **无法热插拔**：接口和继承关系编译时固定，无法运行时增删能力
3. **方法与数据耦合**：类内方法导致虚函数膨胀，设计模式（依赖注入等）只是在"脱裤子放屁"

### 组件模式的优势
1. **高度模块化**：一个组件 = 一份数据 + 一段逻辑
2. **热插拔**：需要就挂上，不需要就摘掉（如骑马组件）
3. **零耦合**：增删组件不影响其它类型
4. **菜鸟友好**：不用纠结继承关系，开发新功能直接挂组件

## ET 的 ECS 特色（与传统 ECS 的区别）

### 树状结构（非扁平式）
传统 ECS 是扁平的（Entity → Component 一层），ET 是 **树状** 的：
- Entity 可以挂 Component
- Component 可以管理 Entity
- Component 还可以挂 Component

```csharp
// 人 → 头 → 眼睛/耳朵/鼻子/嘴巴
Head head = human.AddComponent<Head>();
head.AddComponent<Eye>();
head.AddComponent<Mouse>();
head.AddComponent<Nose>();
head.AddComponent<Ear>();
human.AddComponent<Body>();
human.AddComponent<Hand>();
human.AddComponent<Leg>();
```

这种树状设计让游戏架构有层次感，模块化清晰。

### 数据与逻辑完全分离
- **Entity/Component**：纯数据容器，不允许有方法（分析器强制）
- **System**：逻辑写成静态扩展方法，放在 Hotfix 程序集中

```csharp
// Model 程序集 —— 纯数据
[ComponentOf(typeof(Unit))]
public class MoveComponent : Entity, IAwake
{
    public float3 Target;
    public float Speed;
}

// Hotfix 程序集 —— 逻辑
[FriendOf(typeof(MoveComponent))]
public static partial class MoveComponentSystem
{
    public static void MoveTo(this MoveComponent self, float3 target)
    {
        self.Target = target;
    }
}
```

## 一切皆 Entity

所有数据都是 Entity。通用字段直接放 Entity 上，不通用的挂组件：
```csharp
// 道具：通用字段直接在 Entity 上
class Item : Entity
{
    public int ConfigId { get; set; }
    public int Count { get; set; }
    public int Level { get; set; }
}

// 玩家：通过组件扩展能力
player.AddComponent<MoveComponent>();    // 可移动
player.AddComponent<ItemsComponent>();   // 有背包
player.AddComponent<SpellComponent>();   // 能放技能
player.AddComponent<BuffComponent>();    // 有 buff
```

## InstanceId 机制

每个 Entity 都有 `InstanceId`，在构造或从对象池取出时重新分配：
- **格式**：2字节进程编号 + 4字节时间 + 2字节递增
- **全局唯一**，带有位置信息
- **用途1**：区分对象池复用后的"新旧"对象（异步回调中检查 InstanceId 是否变化）
- **用途2**：Actor 消息路由，通过 InstanceId 定位对象所在进程

```csharp
long instanceId = self.InstanceId;
await SomeAsyncOp();
if (self.InstanceId != instanceId)
    return; // 对象已被释放或复用，终止逻辑
```

## 组件生命周期

### 创建
通过 `AddComponent<T>()` / `AddChild<T>()` 创建，**禁止用 new**（分析器强制）。
框架使用对象池管理，创建时会触发 `AwakeSystem`。

### 释放
调用 `Dispose()` 释放，会：
1. 触发 `DestroySystem`
2. 归还对象池（如果是池创建的）
3. 从 EventSystem 注销，InstanceId 置 0

Entity 的 `Dispose()` 会自动调用所有子 Component 的 `Dispose()`。

## 父子关系约束（分析器强制）

```csharp
[ComponentOf(typeof(Unit))]   // 声明此组件只能挂在 Unit 上
public class SpellComponent : Entity, IAwake { }

[ChildOf(typeof(Player))]     // 声明此实体只能是 Player 的子实体
public class Item : Entity { }
```

`AddComponent` / `AddChild` 的类型必须匹配声明，否则编译报错。
