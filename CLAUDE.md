# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在本仓库中工作提供指导。

## 项目概述

ET 是一个开源游戏框架，Unity（客户端）和 .NET（服务端）双端使用 C#。特性：Entity-Component 架构（数据/逻辑分离）、Fiber 并发模型（类似 Erlang 进程）、Actor 消息、KCP 网络、HybridCLR 热更新。版本：ET9.0，Unity 6000.0.25，.NET 8。

## 开发环境

- **IDE**：Rider 2024.3+（必需），VS 不受官方支持
- **Unity**：6000.0.25（首次搭建请使用此版本）
- **.NET**：需要 .NET 8 SDK
- **代理**：Unity 包和 NuGet 下载需要全局代理/VPN

## 关键命令

| 快捷键 / 命令 | 功能 |
|----------------|------|
| **F6** | 编译热更 DLL |
| **F7** | 热重载 DLL（仅 Play 模式） |
| `ET/StateSync/Init` | 项目初始化（首次） |
| `dotnet build ET.sln` | 编译解决方案 |
| `dotnet .../ET.Proto2CS.dll ./` | Proto 转 C# 代码生成 |
| `dotnet .../ET.ExcelExporter.dll ./` | Excel 配置导出 |
| `dotnet Bin/ET.App.dll --Console=1` | 启动服务器 |

## 分析器规则（编译期强制）

1. **Entity 类不能声明方法** — 逻辑写在静态 System 类中
2. **禁止 `new` Entity 类型** — 使用 `AddComponent`/`AddChild`（对象池）
3. **异步必须返回 ETTask** — 不能用 `Task` 或 `void`
4. **禁止静态字段** — 除非标注 `[StaticField]`
5. **Hotfix 程序集只允许静态类** — 或标注 `[EnableClass]`
6. **AddComponent/AddChild 类型检查** — 必须匹配 `[ComponentOf]`/`[ChildOf]`
7. **命名空间分离** — `ET`（共用）、`ET.Client`、`ET.Server`
8. **Client 类不能出现在 Server 程序集**

## 命名空间

- `ET` — 共用代码（客户端和服务端）
- `ET.Client` — 客户端专用代码
- `ET.Server` — 服务端专用代码

## 详细规则

详细的架构、编码模式和参考资料见 `.claude/project-rules/`：

| 文件 | 内容 |
|------|------|
| `architecture.md` | 包系统、四程序集分离、CodeMode、目录规范、配置、构建命令 |
| `integrations.md` | MongoBson 序列化、FairyGUI、HybridCLR 热更、YooAsset 资源管理 |
| `ecs-patterns.md` | ECS 哲学、树状结构、InstanceId、生命周期、事件系统 |
| `code-templates.md` | Component/System/Handler 模板、属性速查、命名规范、ID 公式 |
| `messaging-network.md` | Proto 规范、Fiber、Actor 模型、Actor Location、死锁解法 |
| `async-patterns.md` | ETTask、ETCancelToken、InstanceId 安全检查、防死循环 |
| `game-systems.md` | NumericComponent KV 公式、AI 行为机 |
| `code-review-checklist.md` | 框架合规审查清单、常见陷阱速查 |
