# ET 异步模式参考

## 核心理念

ET 框架的异步全部基于 **单线程异步**，不使用多线程。所有逻辑跑在同一线程上，通过协程（async/await）实现异步，避免了锁和线程安全问题。

## ETTask vs Task

ET 自实现 `ETTask` 替代 .NET 原生 `Task`：
- **不走 SynchronizationContext**：游戏逻辑本身单线程，走同步上下文是多余开销
- **更高效**：减少 GC，减少不必要的线程调度
- **强制规则**：分析器要求异步方法必须返回 `ETTask`，不能用 `Task` 或 `async void`

`await` 不会开启多线程。ET 的定时器 `CheckTimerOut()` 在主线程每帧轮询，到期后通过 `tcs.SetResult()` 唤醒协程，全程单线程。

## 协程取消模式 (ETCancelToken)

任何可能被打断的异步行为（移动、等待、技能释放等），必须支持取消：

```csharp
// 创建取消令牌
ETCancelToken cancelToken = new ETCancelToken();

// 异步方法中检查取消
bool ret = await MoveToAsync(target, cancelToken);
if (!ret) return; // 被取消则退出

// 外部取消协程
cancelToken.Cancel();
```

**关键规则**：可打断异步行为中的**每个 await** 都必须传入 `cancelToken` 并检查返回值。

## InstanceId 异步安全检查

Entity 从对象池取出时 InstanceId 会重新分配。异步回调中必须检查：

```csharp
long instanceId = self.InstanceId;
await SomeAsyncOp();
if (self.InstanceId != instanceId)
    return; // 对象已被释放或复用，终止逻辑
```

## 编码规范

1. 异步方法 **必须返回 ETTask**（分析器强制）
2. 不需要手动设置 `SynchronizationContext`
3. 发起不等待的协程用 `.Coroutine()` 启动
4. **防死循环**：异步循环中如果可能出现空转（while 循环没进入任何 await），要加 `await TimerComponent.Instance.Wait(100, cancelToken)`
