# ET Actor 模型与网络通信

## Actor 基础

任何 Entity 挂上 `MailBoxComponent` 就是一个 Actor，通过 `InstanceId` 即可跨进程发消息：

```csharp
// 目标 Entity 挂载 MailBox
session.AddComponent<MailBoxComponent, string>(MailboxType.GateSession);

// 发送方通过 InstanceId 发消息
ActorMessageSender sender = scene.GetComponent<ActorSenderComponent>().Get(targetInstanceId);
sender.Send(message);                           // 单向
IResponse response = await sender.Call(request); // RPC
```

### MailBox 类型
- **GateSession**：收到消息直接转发给客户端
- **MessageDispatcher**（默认）：分发到 Handler 处理
- 可自定义：实现 `IMailboxHandler` + `[MailboxHandler]`

## Actor Location（可迁移 Entity）

Entity 可能换线/切场景导致 InstanceId 变化。Actor Location 通过**不变的 Entity.Id** 发消息：

```csharp
// 通过 Entity.Id 发消息（自动查询 Location Server 获取 InstanceId）
ActorLocationSender sender = scene.GetComponent<ActorLocationSenderComponent>().Get(unitId);
sender.Send(message);
IResponse response = await sender.Call(request);
```

### 可靠性机制
- 首次查询 Location Server 后缓存 InstanceId，失败才重新查询
- 发送失败 → 等1秒 → 重新查询 → 重试，最多5次
- Entity 迁移前在 Location Server 加锁，迁移完成后解锁并更新地址

## Actor 死锁

MailBoxComponent 是消息队列逐条处理。A→B→C→A 循环 Call 会死锁。

**解法**：在消息处理中开新协程，不阻塞队列：
```csharp
protected override ETTask Run(Unit unit, Actor_Test message)
{
    RunAsync(unit, message).Coroutine(); // 新协程，不阻塞队列
    return ETTask.CompletedTask;
}
```

## 客户端消息与 Actor

客户端用 `session.Send()` / `session.Call()` 发消息 → Gate 判断是 Actor 消息 → 直接转发到目标 Actor（如 Map 上的 Unit）。客户端代码无感知。
