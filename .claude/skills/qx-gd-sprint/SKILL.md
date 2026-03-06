---
name: qx-gd-sprint
description: "用于 Sprint 规划、Sprint 状态跟踪和 Sprint 回顾。管理从 Epic/Story 拆分到执行跟踪的开发节奏。"
---

# Sprint 管理

## 概述

管理开发 Sprint：规划工作、跟踪状态、进行回顾。结合游戏设计中的 Epic 和 Story 来组织实现工作。

**开始时宣告：** "正在使用 qx-gd-sprint 来 [规划/跟踪/回顾] Sprint。"

## 模式

本 Skill 根据上下文以三种模式运行：

| 模式 | 触发条件 | 输出 |
|------|---------|--------|
| **规划** | "plan sprint"、"sprint planning"、新 sprint | `docs/sprints/sprint-N-plan.md` |
| **状态** | "sprint status"、"where are we" | 更新 Sprint 计划中的状态 |
| **回顾** | "retrospective"、"sprint retro"、sprint 结束 | `docs/sprints/sprint-N-retro.md` |

## Sprint 规划模式

### 前提条件
- 游戏设计文档已存在（`docs/game-design/`）
- Story 已经拆分完成（通过 `/qx-gd-stories` 或手动）
- 已了解团队产能（每个 Sprint 多少故事点？）

### 流程

**步骤 1：收集上下文**
1. 阅读现有游戏设计文档和 Story 待办列表
2. 检查上一次 Sprint 回顾（如果不是第一个 Sprint）中的遗留事项
3. 识别当前项目优先级

**步骤 2：选择 Story**

与用户协作选择本次 Sprint 的 Story：
- 按优先级排序展示待办列表
- 对每个 Story 展示：ID、标题、估算、依赖关系
- 尊重产能限制
- 标记依赖链（Story B 需要 Story A 先完成）

**步骤 3：生成 Sprint 计划**

```markdown
# Sprint N 计划

**周期：** [开始日期] → [结束日期]
**目标：** [一句话 Sprint 目标]
**产能：** [X 故事点]

## Stories

| # | Story | 点数 | 负责人 | 依赖 | 状态 |
|---|-------|------|--------|------|------|
| 1.1 | [标题] | 3 | - | 无 | backlog |
| 1.2 | [标题] | 2 | - | 1.1 | backlog |
| 2.1 | [标题] | 5 | - | 无 | backlog |

## Sprint 待办列表（未纳入本次 Sprint）
[按优先级排序的剩余 Story]

## 风险与备注
- [需要标记的风险或阻塞项]
```

保存到：`docs/sprints/sprint-N-plan.md`

## Sprint 状态模式

### 状态机

**Story 状态流转：**
```
backlog → ready → in-progress → review → done
```

| 状态 | 含义 |
|--------|---------|
| `backlog` | Story 已存在但未开始工作 |
| `ready` | Story 已细化，可以开始开发 |
| `in-progress` | 开发者正在积极工作 |
| `review` | 代码完成，正在评审中 |
| `done` | 已评审、已测试、已合并 |

### 状态更新流程

1. 读取当前 Sprint 计划文件
2. 检查 git 历史和活跃变更（`docs/changes/`）中的进度信号
3. 向用户展示当前状态
4. 询问是否需要修正/更新
5. 将新状态更新到 Sprint 计划文件

### 状态报告格式

```
Sprint N 状态更新 — [日期]

进度：[X/Y] 个 Story 完成（[Z] 点 / [总计] 点）

  [done]        1.1 Story 标题 (3 pts)
  [in-progress] 1.2 Story 标题 (2 pts) — 活跃变更: feature-name
  [backlog]     2.1 Story 标题 (5 pts)

阻塞项：
  - [已识别的阻塞项]

速率：本 Sprint 至今已完成 [X] 点
```

## Sprint 回顾模式

### 流程

**步骤 1：收集数据**
- 计划 vs 实际完成情况
- Sprint 速率（完成的故事点）
- 遗留 Story（未完成）
- Sprint 期间的重要事件

**步骤 2：引导讨论**

一次一个地提出这些问题：
1. **做得好的？** — 我们应该继续做什么？
2. **做得不好的？** — 什么导致了摩擦或问题？
3. **我们学到了什么？** — 关于游戏、代码或流程的新见解？
4. **我们应该改变什么？** — 下个 Sprint 的具体行动项

**步骤 3：生成回顾文档**

```markdown
# Sprint N 回顾

**日期：** [日期]
**Sprint 目标：** [是否达成？]
**速率：** [计划 X 点，完成 Y 点]

## 已完成
- [已完成的 Story]

## 遗留
- [未完成的 Story，附原因]

## 做得好的
- [条目]

## 做得不好的
- [条目]

## 经验教训
- [条目]

## 下个 Sprint 行动项
- [ ] [具体行动]
- [ ] [具体行动]
```

保存到：`docs/sprints/sprint-N-retro.md`

### 复合积累

回顾完成后，建议运行 `/qx-compound` 将本次 Sprint 的知识提取到知识库中。

## 与 QX 工作流的集成

```
GDD → /qx-gd-stories → Stories → /qx-gd-sprint plan → Sprint 计划
→ /qx-dev-change create（每个 Story）→ 开发 → /qx-gd-sprint status
→ Sprint 完成 → /qx-gd-sprint retro → /qx-compound
→ 下个 Sprint → /qx-gd-sprint plan
```

## 关键原则

- **一个 Sprint 目标** — 每个 Sprint 有一个清晰的目标
- **尊重产能** — 不要过度承诺；为 Bug 留出缓冲
- **依赖优先** — 仔细规划依赖链
- **诚实地处理遗留** — 不要隐藏未完成的工作
- **回顾驱动改进** — 行动项必须具体且可执行

## 相关 Skills

- **qx-gd-brainstorm** — 用于 Sprint 规划前的功能构思
- **qx-gd-gdd** — GDD 提供 Story 待办列表的来源
- **qx-dev-change** — 每个 Story 在执行时变为一个变更
- **qx-dev-plan** — 用于为每个 Story 创建实施计划
