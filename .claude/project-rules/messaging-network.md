# ET 消息与网络参考

## Proto 消息规范

Proto 文件命名格式：`{Name}_{CS}_{StartOpcode}.proto`
- `_C_` = 仅客户端消息，`_S_` = 仅服务端（内部）消息
- 通过注释声明消息类型：`// IMessage`、`// IRequest`、`// IResponse`、`// ISessionRequest`、`// ILocationRequest` 等
- `// ResponseType TypeName` 注释关联 Request 与其 Response
- 消息前缀：`C2G_`、`C2M_`、`G2C_`、`M2C_`

## Fiber（并发模型）

Fiber 是核心调度单元，类似 Erlang 进程。每个 Fiber 拥有自己的 Scene 根节点，运行在三种调度器之一：
- **Main** — Unity 主线程
- **Thread** — 每个 Fiber 独占线程
- **ThreadPool** — 共享线程池

Fiber 之间通过 `MessageQueue` 使用 Actor 消息通信。

## 网络

- **Session**：网络连接封装，通过 `session.Call(request)` 处理 RPC
- **KCP**：默认协议，支持 TCP/WebSocket 回退，运行时协议切换
- **Actor**：位置透明消息传递。带有 `MailBoxComponent` 的 Entity 可以从任何服务器通过 ID 接收消息

## 跨 Fiber 通信规则

- **绝对不能**直接跨 Fiber 访问对象 — 必须使用 Actor 消息
- 带有 `MailBoxComponent` 的 Entity 可以接收 Actor 消息
- 使用 `MessageHelper.CallLocationActor()` 进行基于位置的消息传递

## Actor 模型详解

ET 采用单线程多进程架构。Actor 是 **Entity 级别**的（不是进程级别），任何 Entity 挂上 `MailBoxComponent` 就是一个 Actor，通过 `InstanceId` 即可发消息。

### MailBox 类型

- **GateSession**：收到消息立即转发给客户端（不做分发处理）
- **MessageDispatcher**（默认）：将消息分发到具体 Handler 处理
- 可自定义：实现 `IMailboxHandler` + `[MailboxHandler]` 标签

### Actor 消息处理

```csharp
// Send 消息处理
[ActorMessageHandler(AppType.Map)]
public class Actor_TestHandler : AMActorHandler<Unit, Actor_Test>
{
    protected override ETTask Run(Unit unit, Actor_Test message) { }
}

// RPC 消息处理
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

## Actor Location 模型

### 解决的问题

玩家换线/切场景时 Entity 迁移到其他进程，InstanceId 随之变化。Actor Location 通过**不变的 Entity.Id** 发送消息。

### 工作原理

1. **Location Server**：中央位置服务，存储 Entity.Id → InstanceId 映射
2. Entity 创建/迁移时向 Location Server 注册新的 InstanceId
3. 发送方通过 Entity.Id 查询 Location Server 获取 InstanceId 再发送

### 可靠性机制

- **缓存**：首次查询后缓存 InstanceId，失败才重新查询
- **重试**：发送失败 → 等待 1 秒 → 重新查询 → 重试，最多 5 次
- **锁机制**：迁移前在 Location Server 加锁，迁移期间请求排队，完成后解锁更新
- **回执**：所有 Send 消息也有回执，必须等回执才能发下一条

### 使用方式

```csharp
ActorLocationSender sender = scene.GetComponent<ActorLocationSenderComponent>().Get(unitId);
sender.Send(message);
IResponse response = await sender.Call(request);
```

### 客户端消息与 Actor 的关系

Actor 是纯服务端进程间通信机制。客户端消息带 Actor 接口是因为：客户端 → Gate → Gate 判断是 Actor 消息 → 直接转发到目标 Actor（如 Map 上的 Unit），避免额外包装和二次分发。客户端代码仍用 `session.Send()` / `session.Call()`。
