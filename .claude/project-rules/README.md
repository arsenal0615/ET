# ET 项目规则

> 本目录包含 ET 框架的详细编码规则、模式和参考资料。
> Agent 和 Skill 在需要时按需加载这些文件。
>
> **快速参考**：根目录 `CLAUDE.md` 包含最重要的编译期规则摘要。

## 文件索引

| 文件 | 内容 | 何时读取 |
|------|------|----------|
| `architecture.md` | 包系统、四程序集分离、CodeMode、入口点、配置、构建命令 | 创建新包/模块、理解项目结构时 |
| `coding-patterns.md` | Entity-Component 模式、代码模板、属性速查、命名规范 | 编写新代码、审查代码时 |
| `messaging-network.md` | Proto 规范、消息类型、Fiber、Actor、网络 | 涉及消息/网络/跨 Fiber 通信时 |
| `code-review-checklist.md` | 框架合规审查清单 | 代码审查时 |
