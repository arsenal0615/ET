# ET Actor 模型与网络通信

## 架构背景

ET 采用 **单线程多进程** 架构（区别于 erlang 的单进程多线程）：
- 每个进程 ≈ 一个 erlang 进程 / 一个 skynet lua 虚拟机
- 优势：可复用系统 profiler 工具（top 等），单机/多机部署无差异
- 代价：跨进程消息需序列化，有几毫秒延迟（通常可忽略）

| 对比 | ET | Erlang | Skynet |
|------|------|--------|--------|
| 架构 | 单线程多进程 | 单进程多线程 | 单进程多线程 |
| Actor 单元 | Entity | erlang 进程 | lua 虚拟机 |
| Actor ID | InstanceId | 进程 Pid | 服务地址 |

## Actor 模型（基础版）

### 概念
ET 的 Actor 是 **Entity 级别** 的（不是进程级别），任何 Entity 挂上 `MailBoxComponent` 就是一个 Actor。通过 `InstanceId` 就能给它发消息，无需知道它在哪个进程。

### 使用流程
1. 目标 Entity 挂载 MailBoxComponent：
```csharp
session.AddComponent<MailBoxComponent, string>(MailboxType.GateSession);
```

2. 发送方通过 InstanceId 发消息：
```csharp
ActorSenderComponent actorSenderComponent = scene.GetComponent<ActorSenderComponent>();
ActorMessageSender sender = actorSenderComponent.Get(targetInstanceId);
sender.Send(message);                    // 单向发送
IResponse response = await sender.Call(request);  // RPC 调用
```

### MailBox 类型
- **GateSession**：收到消息立即转发给客户端（不做分发处理）
- **MessageDispatcher**（默认）：将消息分发到具体 Handler 处理

可自定义邮箱类型：实现 `IMailboxHandler` 接口 + `[MailboxHandler]` 标签。

### Actor 消息处理
```csharp
// 处理 Send 消息
[ActorMessageHandler(AppType.Map)]
public class Actor_TestHandler : AMActorHandler<Unit, Actor_Test>
{
    protected override ETTask Run(Unit unit, Actor_Test message) { }
}

// 处理 RPC 消息
[ActorMessageHandler(AppType.Map)]
public class Actor_TransferHandler : AMActorRpcHandler<Unit, Actor_TransferRequest, Actor_TransferResponse>
{
    protected override async ETTask Run(Unit unit, Actor_TransferRequest message, Action<Actor_TransferResponse> reply) { }
}
```

### Actor 死锁问题
MailBoxComponent 本质是消息队列，一个个处理。如果 A→B→C→A 形成循环 Call，会死锁。
**解决方案**：在消息处理中开新协程，不阻塞队列：
```csharp
protected override ETTask Run(Unit unit, Actor_Test message)
{
    RunAsync(unit, message).Coroutine(); // 新协程处理，不阻塞队列
}
```

## Actor Location 模型（进阶版）

### 解决什么问题
玩家可能换线、切场景，Entity 会迁移到其他进程，InstanceId 随之变化。Actor Location 通过 **不变的 Entity.Id** 来发送消息。

### 工作原理
1. **Location Server**：中央位置服务，存储 Entity.Id → InstanceId 的映射
2. Entity 创建/迁移时，向 Location Server 注册新的 InstanceId
3. 发送方通过 Entity.Id 查询 Location Server 获取 InstanceId，再用 Actor 发送

### 可靠性机制
- **缓存**：首次查询后缓存 InstanceId，后续直接用，失败才重新查询
- **重试**：发送失败（对象已迁移）→ 等待 1 秒 → 重新查询 → 重试，最多 5 次
- **锁机制**：对象迁移前在 Location Server 加锁，迁移期间其他请求排队等待，迁移完成后解锁并更新地址
- **回执**：所有 Send 消息也有回执（表示收到），必须等回执才能发下一条

### 使用方式
```csharp
// 通过 Entity.Id 获取 Sender
ActorLocationSender sender = scene.GetComponent<ActorLocationSenderComponent>().Get(unitId);
sender.Send(message);
IResponse response = await sender.Call(request);
```

### 消息处理（注意类名多了 Location）
```csharp
[ActorMessageHandler(AppType.Map)]
public class Frame_ClickMapHandler : AMActorLocationHandler<Unit, Frame_ClickMap>
{
    protected override ETTask Run(Unit unit, Frame_ClickMap message) { }
}

[ActorMessageHandler(AppType.Map)]
public class C2M_TestHandler : AMActorLocationRpcHandler<Unit, C2M_TestRequest, M2C_TestResponse>
{
    protected override async ETTask Run(Unit unit, C2M_TestRequest message, Action<M2C_TestResponse> reply) { }
}
```

## 邮递比喻（作者原文总结）

- **城市** = 进程，**人** = Entity
- **身份证号** = Entity.Id（永不变），**居住证号** = InstanceId（换城市就变）
- **普通邮政（Actor）**：知道居住证号直接寄信
- **高级邮政（Actor Location）**：只知道身份证号 → 去中央政府（Location Server）查居住证 → 再寄信
- 搬家（迁移）期间，中央政府加锁，其他信件排队等待

## 客户端消息与 Actor 的关系

Actor 是 **纯服务端** 的进程间通信机制。客户端消息之所以带 Actor 接口，是因为：
- 客户端消息发到 Gate → Gate 判断是 Actor 消息 → 直接转发到目标 Actor（如 Map 上的 Unit）
- 这样避免了额外的包装协议和二次分发
- 客户端代码仍然用 `session.Send()` / `session.Call()` 发消息，无感知

## Proto 消息定义规范

文件命名：`{Name}_{CS}_{StartOpcode}.proto`
- `_C_` = 客户端消息，`_S_` = 服务端内部消息
- 通过注释声明类型：`// IMessage`, `// IRequest`, `// IResponse`, `// IActorLocationMessage` 等
- `// ResponseType TypeName` 关联 Request 和 Response
