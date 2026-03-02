# ET 消息与网络参考

## Proto Message Conventions

Proto files follow naming convention: `{Name}_{CS}_{StartOpcode}.proto`
- `_C_` = client-only messages, `_S_` = server-only (inner) messages
- Message types declared via comments: `// IMessage`, `// IRequest`, `// IResponse`, `// ISessionRequest`, `// ILocationRequest`, etc.
- `// ResponseType TypeName` comment links Request to its Response
- Message prefixes: `C2G_`, `C2M_`, `G2C_`, `M2C_`

## Fiber (Concurrency Model)

Fiber is the core scheduling unit, similar to Erlang processes. Each Fiber has its own Scene root and runs on one of three schedulers:
- **Main** — Unity main thread
- **Thread** — Dedicated thread per Fiber
- **ThreadPool** — Shared thread pool

Fibers communicate via Actor messages through `MessageQueue`.

## Networking

- **Session**: Network connection wrapper, handles RPC via `session.Call(request)`
- **KCP**: Default protocol, supports TCP/WebSocket fallback, runtime protocol switching
- **Actor**: Location-transparent messaging. Entities with `MailBoxComponent` can receive messages by ID from any server

## Cross-Fiber Communication Rules

- **NEVER** directly access objects across Fibers — must use Actor messages
- Entities with `MailBoxComponent` can receive Actor messages
- Use `MessageHelper.CallLocationActor()` for location-based messaging
