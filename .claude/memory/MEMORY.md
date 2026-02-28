# ET 框架知识库索引

本目录整理自 `Book/` 文档和项目 CLAUDE.md，为 AI 辅助 ET 框架游戏开发提供深度参考。

## 知识文件

| 文件 | 内容 | 关键词 |
|------|------|--------|
| [et-async-pattern.md](et-async-pattern.md) | ETTask 原理、单线程异步、协程取消 | ETTask, async/await, ETCancelToken, 单线程 |
| [et-entity-component.md](et-entity-component.md) | 实体组件设计哲学、树状结构、InstanceId | Entity, Component, ECS, InstanceId, 对象池 |
| [et-event-system.md](et-event-system.md) | 事件系统、数据驱动、生命周期System | EventSystem, Awake/Destroy/Update, Event, Invoke |
| [et-actor-network.md](et-actor-network.md) | Actor 消息、Actor Location、锁机制 | Actor, MailBox, Location Server, 进程间通信 |
| [et-game-systems.md](et-game-systems.md) | 数值组件 KV 设计、AI 行为机 | NumericComponent, Buff, 行为机, AINode, CancelToken |
| [et-package-structure.md](et-package-structure.md) | 包目录规范、包列表、MongoBson 序列化 | Package, Scripts, CodeMode, BsonSerializer |

## 快速参考：ET9 核心编码规则

1. **Entity 不能有方法** → 逻辑写在 Hotfix 的静态扩展方法中
2. **Entity 字段是 private** → 通过 `[FriendOf]` 访问
3. **禁止 new Entity** → 必须用 AddComponent/AddChild
4. **异步必须返回 ETTask** → 不能用 Task 或 async void
5. **Hotfix 程序集只能有静态类** → 或用 `[EnableClass]` 标注
6. **禁止静态字段** → 除非标注 `[StaticField]`
7. **四程序集分离**：Model(数据) / Hotfix(逻辑) / ModelView(客户端数据) / HotfixView(客户端逻辑)
8. **三目录分离**：Client/ Server/ Share/ 控制代码可见性

## 快速参考：常用操作

- 编译热更 DLL：F6
- 热重载（Play 模式）：F7
- 初始化项目：`ET -> StateSync -> Init`
- Proto 生成：`dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./`
- Excel 导出：`dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./`
- 启动服务器：`dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=StartConfig/Localhost --Console=1`
