# ET 项目 AI Agent 记忆

> 本文件为项目内记忆（`.claude/memory/`），由自动记忆系统的 stub 引用。项目知识已全部整理到 `.claude/project-rules/`。

## 全兴（QX）工作流

- **状态**: 构建完成，验收中（A1 项目）
- **产出**: 17 Skills + 22 Commands + 6 Agents + 8 Templates + 3 Knowledge + 3 Checklists + 2 Hooks + system-map.md
- **验收进度**: GDD → 三方评审 → Story 拆分 → a1-foundation ✅ → a1-economy ✅ → a1-shop ✅ → 下一步: Sprint 规划
- **验收问题**: 见 `qx-workflow-issues.md`（8 个问题已记录）

## 自走棋（cn.etetet.autochess）开发经验

- **完成变更**: a1-foundation（Entity树+PRNG+FSM）、a1-economy（经济）、a1-shop（卡池+商店），详见 `.claude/memory/autochess-patterns.md`
- **坑1 [Event SceneType]**: autochess AEvent 必须用 `[Event(SceneType.Map)]`（非 Server/Main）— 比赛运行在 Map Scene
- **坑2 [FriendOf 完整性]**: 静态服务类访问多个 Entity 类型时，每个类型都要声明 `[FriendOf]`
- **坑3 [ET0032 EnableClass]**: Model 程序集中非 Entity 的普通 C# 类（如 ShopOffer）必须加 `[EnableClass]`
- **坑4 [ET0013 循环依赖]**: 两个静态类互调触发 ET0013，解决方案是内联被调用逻辑，不要跨服务类调用
- **模式 [DeriveSubSeed]**: 第二参数不限于 round，可以是 rollIndex+slot 或 playerIndex 任意区分值
- **模式 [静态服务类]**: 多处触发的横切逻辑（如经济、商店）用带 `[FriendOf]` 的纯静态类，而非 ComponentSystem
- **模式 [预扣机制]**: 生成 Offer 时立即预扣卡池，下次 GenerateOffersForPlayer 开始时先 ReturnCurrentOffers
- **Final Review 价值**: per-task review 会遗漏跨文件的架构问题，qx-exec Final Review 步骤不可省略

## 持久工作规则

> 以下规则适用于本项目的所有后续工作，必须严格遵守：

1. **步骤记录规则** — 每一步完成后，将有效信息（含 token 消耗，如有）追加到 `.claude/memory/qx-a1-workflow-analysis.md`
2. **问题记录规则** — 发现任何工作流问题、设计缺陷或改进建议，立即记录到 `.claude/memory/qx-workflow-issues.md`

Token 数据来源：Agent 返回结果中的 `<usage>` 字段。若无数据则记录为"不可用"。

---

## 项目规则索引（按需加载）

详细编码规则和参考资料在 `.claude/project-rules/`：

| 文件 | 内容 |
|------|------|
| `architecture.md` | 包系统、四程序集、CodeMode、目录规范、配置、构建命令 |
| `integrations.md` | MongoBson 序列化、FairyGUI、HybridCLR 热更、YooAsset 资源管理 |
| `ecs-patterns.md` | ECS 哲学、树状结构、InstanceId、生命周期、事件系统 |
| `code-templates.md` | Component/System/Handler 模板、属性速查、命名规范、ID 公式 |
| `messaging-network.md` | Proto 规范、Fiber、Actor 模型、Actor Location、死锁解法 |
| `async-patterns.md` | ETTask、ETCancelToken、InstanceId 安全检查、防死循环 |
| `game-systems.md` | NumericComponent KV 公式、AI 行为机 |
| `code-review-checklist.md` | 框架合规审查清单、常见陷阱速查 |
