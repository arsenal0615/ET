<!-- QX:START -->
<!-- QX:VERSION:0.1.0 -->
# 全兴（QX）— Unity 游戏开发工作流

你正在使用全兴（QX, QuanXing）工作流。QX 是一条自包含的端到端 Unity 游戏开发工作流，覆盖策划、程序、美术、QA 四个工种。

## 核心原则

1. **手动触发交接** — 阶段之间需人工确认后再触发下一阶段
2. **角色专属上下文** — 每个角色只注入其需要的知识和规则
3. **复合积累** — 每次工作完成后提取知识，持续改进
4. **两层系统分析** — 持久化系统图 + 按需影响分析

## 角色 Agent

| Agent | 文件 | 用途 |
|-------|------|------|
| 游戏策划 | `.claude/agents/game-designer.md` | 创意、GDD、叙事、系统架构、Story 拆分 |
| 程序员 | `.claude/agents/programmer.md` | 代码开发、TDD、调试、技术变更管理 |
| 美术/UX | `.claude/agents/artist-ux.md` | UX 设计、UI 原型、美术规范 |
| QA 测试 | `.claude/agents/qa-tester.md` | 测试设计、自动化测试、质量验证 |

**专家 Agent（被 Skills 内部调用）**：
- `code-reviewer.md` — 代码质量 + 计划对齐 + 框架合规审查
- `security-reviewer.md` — 安全漏洞审查

## QX 命令索引

### [策划] 设计文档
- `/qx-gd-gdd` — 创建/编辑游戏设计文档（阶段感知 + 草稿缓存）
- `/qx-gd-brainstorm` — 创意发散 + 隐藏决策点
- `/qx-gd-narrative` — 叙事设计

### [策划] 文档审查与精修
- `/qx-gd-audit-design` — 盲点挖掘（未完善/未决策/隐含假设）
- `/qx-gd-audit-decision` — 决策点审视（同类对标 + 优化建议）
- `/qx-gd-ambiguity` — 三阶段歧义审查
- `/qx-gd-refine` — 中文文本精炼
- `/qx-gd-prototype` — HTML 原型可视化验证

### [策划] 规划
- `/qx-gd-stories` — 拆分 Epic 和 Story
- `/qx-gd-sprint` — Sprint 规划/状态

### [程序] 开发
- `/qx-dev-explore` — 代码库探索（自由探索 + 架构可视化）
- `/qx-dev-impact` — 系统影响分析
- `/qx-dev-create` — 创建变更 + 提案（可选: spec 细化需求）
- `/qx-dev-design` — 编写技术方案
- `/qx-dev-continue` — 自动推进变更到下一步
- `/qx-dev-status` — 查看变更状态 / 列出所有变更
- `/qx-dev-close` — 验证并归档变更
- `/qx-dev-plan` — 编写实施计划
- `/qx-dev-exec` — 多 Agent 执行计划（--loop 持久化，--worktree 隔离）
- `/qx-dev-worktree` — 创建隔离 worktree 进行功能开发
- `/qx-dev-stop` — 停止持久化循环
- `/qx-dev-debug` — 系统化调试

### [美术] UX
- `/qx-art-ux` — UX 设计

### [QA] 测试
- `/qx-qa-test` — 测试设计/自动化/执行（design|automate|execute）
- `/qx-qa-playtest` — 玩家测试计划

### [跨岗] 评审与验证
- `/qx-meeting` — 开三方评审（多角色评审）
- `/qx-verify` — 完成前验证

### [跨岗] 知识管理
- `/qx-compound` — 复合积累（提取知识更新知识库）
- `/qx-retro` — Sprint 回顾

## 工作流阶段（概览）

```
策划 → 技术设计 → 实现 → 收尾+复合积累
         ↑              ↑
     [可选: 三方评审]  [可选: 多维审查/QA测试]
```

**变更核心流程:** /qx-dev-create → /qx-dev-design → /qx-dev-plan → /qx-dev-exec → /qx-dev-close
**可选步骤（按需触发，非必经）:**
- `/qx-dev-create spec` — 仅复杂功能需要细化需求
- `/qx-meeting` 三方评审 — 跨角色交接或高风险变更时推荐
- `/qx-qa-test` QA 测试 — 有正式测试需求时使用

每个阶段之间需要人工确认触发。

## 关键目录

| 路径 | 用途 |
|------|------|
| `.claude/agents/` | 角色和专家 Agent 定义 |
| `.claude/skills/` | QX 工作流 Skills |
| `.claude/commands/` | `/qx-*` 命令入口 |
| `.claude/qx/templates/` | 文档模板 |
| `.claude/qx/knowledge/` | 角色知识库 |
| `.claude/qx/state/` | 运行时状态（gitignored） |
| `.claude/qx/hooks/` | Hook 脚本 |
| `.claude/project-rules/` | 项目框架详细规则（按需加载） |
| `.claude/qx/checklists/` | 流程检查清单 |
| `docs/changes/` | 技术变更（活跃 + 归档） |
| `docs/game-design/` | 策划文档（GDD、叙事等） |
| `docs/system-map.md` | 系统关系图 |
| `docs/reviews/` | 开三方评审记录 |
| `docs/sprints/` | Sprint 计划和回顾 |
| `docs/test-plans/` | 测试计划 |

<!-- QX:END -->
