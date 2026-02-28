# ET 异步机制 (ETTask / 协程)

## 核心理念

ET 框架的异步全部基于 **单线程异步**，不使用多线程。所有逻辑跑在同一线程上，通过协程（async/await）实现异步，避免了锁和线程安全问题。

## 从回调到 async/await 的演进

### 1. 回调模式（传统协程）
一连串回调就是协程。每次异步操作完成后回调下一个方法：
```csharp
WaitTimeAsync(5000, () => {
    Console.WriteLine("5秒后");
    WaitTimeAsync(3000, () => {
        Console.WriteLine("再3秒后");
    });
});
```
**缺陷**：嵌套深、插入/删除逻辑极易出错。

### 2. async/await 模式
利用 C# 的 `TaskCompletionSource` + `await` 将回调展平为同步写法：
```csharp
await WaitTimeAsync(5000);
Console.WriteLine("5秒后");
await WaitTimeAsync(3000);
Console.WriteLine("再3秒后");
```

### 3. 单线程异步的关键
`await` **不会开启多线程**，它是否用多线程取决于底层实现。ET 的定时器 `CheckTimerOut()` 在主线程每帧轮询，到期后通过 `tcs.SetResult()` 唤醒协程，全程单线程。

## ETTask vs Task

ET 没有使用 .NET 原生的 `Task`，而是自己实现了 `ETTask`：
- **不走 SynchronizationContext**：原生 Task 的回调会经过同步上下文再回到主线程，游戏开发中逻辑本身就是单线程的，走同步上下文是多余开销
- **更高效**：减少 GC，减少不必要的线程调度
- **强制规则**：分析器要求异步方法必须返回 `ETTask`，不能用 `Task` 或 `async void`

## 协程取消模式 (ETCancelToken)

ET 的协程支持取消，这在 AI、移动等场景中至关重要：
```csharp
// 创建取消令牌
ETCancelToken cancelToken = new ETCancelToken();

// 异步方法中检查取消
bool ret = await MoveToAsync(target, cancelToken);
if (!ret) return; // 被取消则退出

// 外部取消协程
cancelToken.Cancel();
```

**关键规则**：任何可能被打断的异步行为（移动、等待、技能释放等），其中的每个 `await` 都必须传入 `cancelToken` 并检查返回值。

## 编码规范

1. 异步方法 **必须返回 ETTask**，不能用 `Task` 或 `void`（分析器强制）
2. 不需要手动设置 `SynchronizationContext`
3. 协程中发起新的不等待的协程用 `.Coroutine()` 启动
4. 异步循环中如果可能出现"空转"（两个 while 都没进入），要加 `await TimerComponent.Instance.Wait(100, cancelToken)` 防止死循环
