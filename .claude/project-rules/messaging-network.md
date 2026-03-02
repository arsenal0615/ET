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
