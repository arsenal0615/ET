# ET 异步机制 (ETTask)

## 核心规则

- 异步方法**必须返回 `ETTask` / `ETTask<T>`**，禁止 `Task` 或 `async void`（编译器强制）
- ET 是**单线程异步**，`await` 不会开启新线程，通过主线程每帧轮询 `tcs.SetResult()` 唤醒协程
- 不需要 `SynchronizationContext`
- 启动不等待的协程用 `.Coroutine()`

## 协程取消 (ETCancelToken)

任何可能被打断的异步行为（移动、等待、技能释放等），每个 `await` 都必须传 `cancelToken` 并检查返回值：

```csharp
ETCancelToken cancelToken = new ETCancelToken();

// 异步方法中
bool ret = await MoveToAsync(target, cancelToken);
if (!ret) return; // 被取消

// 外部取消
cancelToken.Cancel();
```

## 防死循环

异步循环中如果可能出现空转（while 循环没进入任何 await），必须加短暂等待：
```csharp
await TimerComponent.Instance.Wait(100, cancelToken);
```
