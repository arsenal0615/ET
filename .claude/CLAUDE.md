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
- `code-reviewer.md` — 代码质量 + 计划对齐审查
- `et-rule-reviewer.md` — ET 框架规则合规审查
- `security-reviewer.md` — 安全漏洞审查

## QX 命令索引

### 策划
- `/qx-brainstorm` — 游戏创意头脑风暴
- `/qx-gdd` — 创建/编辑游戏设计文档
- `/qx-narrative` — 叙事设计
- `/qx-game-arch` — 游戏系统架构
- `/qx-stories` — 拆分 Epic 和 Story
- `/qx-sprint` — Sprint 规划/状态
- `/qx-prd` — 产品需求文档

### 评审
- `/qx-review` — 开三方评审（多角色评审）
- `/qx-impact` — 系统影响分析

### 程序员
- `/qx-explore` — 代码库探索
- `/qx-change create|propose|spec|design|verify|archive` — 技术变更全生命周期
- `/qx-plan` — 编写实施计划
- `/qx-exec [--loop] [--worktree]` — 多 Agent 执行计划（--loop 持久化，--worktree 隔离）
- `/qx-worktree` — 创建隔离 worktree 进行功能开发
- `/qx-stop` — 停止持久化循环
- `/qx-debug` — 系统化调试

### 美术/UX
- `/qx-ux` — UX 设计

### QA
- `/qx-test design|automate|execute` — 测试设计/自动化/执行
- `/qx-playtest` — 玩家测试计划

### 通用
- `/qx-verify` — 完成前验证
- `/qx-compound` — 复合积累（提取知识更新知识库）
- `/qx-retro` — Sprint 回顾

## 工作流阶段（概览）

```
策划 → 开三方评审 → 技术设计 → 实现 → 多维审查 → QA测试 → 收尾+复合积累
```

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
| `.claude/qx/checklists/` | 流程检查清单 |
| `docs/changes/` | 技术变更（活跃 + 归档） |
| `docs/game-design/` | 策划文档（GDD、叙事等） |
| `docs/system-map.md` | 系统关系图 |
| `docs/reviews/` | 开三方评审记录 |
| `docs/sprints/` | Sprint 计划和回顾 |
| `docs/test-plans/` | 测试计划 |

<!-- QX:END -->
