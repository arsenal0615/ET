---
name: qx-dev-exec
description: "在有已批准的实施计划时使用。读取计划，按任务分派子 Agent 并遵循 TDD 纪律，运行两阶段审查（规格合规 + 代码质量），并跟踪进度。支持 --loop 持久化执行。"
---

# 多 Agent 执行计划（Multi-Agent Plan Execution）

## 概述

通过为每个任务分派全新的子 Agent 来执行已批准的实施计划。每个任务遵循 TDD 纪律，接受两阶段审查（规格合规 + 代码质量），进度通过 TodoWrite 和计划文件复选框双重跟踪。

**核心原则：** 每个任务一个全新 Agent + TDD + 两阶段审查 = 高质量、快速迭代。

**启动时宣告：** "正在使用 qx-dev-exec 执行位于 [plan path] 的计划。"

## 何时使用

- 你有一个已批准的计划文件（来自 `/qx-dev-plan` 或 `/qx-dev-change design`）
- 任务大多独立（可由不同 Agent 分别处理）
- 你希望有系统化的执行和质量关卡

## 架构

```
/qx-dev-exec "docs/changes/feature/plan.md"
     │
     ▼
┌─ QX Exec ────────────────────────────────────────┐
│                                                    │
│  1.  读取计划，提取所有任务                         │
│  2.  检测模式（change vs quick）                    │
│  3.  创建 TodoWrite 包含所有任务                    │
│  1.5 构建静态上下文（计划级别）：                    │
│      - 读取计划头部（目标、架构、技术栈）            │
│      - 读取框架规则（CLAUDE.md + Rules:）           │
│      - 读取设计文档（Design Ref:）                  │
│      - 初始化 task_outputs = {}                    │
│  4.  对每个任务：                                   │
│      a. 构建任务上下文包：                          │
│         - static_context（复用）                    │
│         - Depends → 注入前置任务输出               │
│         - Reads → 注入文件内容                     │
│         - Why → 注入设计意图                       │
│      b. 分派实现者子 Agent                         │
│         （附带完整预加载上下文）                     │
│      c. 解析实现者输出 → task_outputs              │
│      d. 分派规格审查员（附带内联规则）              │
│      e. 分派质量审查员（附带规则）                  │
│      f. 标记任务完成（TodoWrite + 复选框）          │
│  5.  所有任务完成后：分派最终审查                    │
│  6.  提供 /qx-finishing 选项                       │
│                                                    │
│  --loop: 持续工作直到所有任务完成                    │
│          （通过 hook 实现持久化模式）                │
└────────────────────────────────────────────────────┘
```

## 流程

### 步骤 1：加载并验证计划

```
1. 读取计划文件
2. 确定模式：
   - Change Mode: 计划位于 docs/changes/<name>/plan.md
     → 启用复选框跟踪（- [ ] → - [x]）
     → 检查现有进度（从上次未完成处恢复）
   - Quick Mode: 计划位于 docs/plans/*.md
     → 仅使用 TodoWrite 跟踪
3. 提取所有任务的完整文本
4. 检查进度（Change Mode）：
   - 统计 - [x] vs - [ ] 复选框
   - 如果有进度："N/M 个任务已完成，从任务 K 恢复"
5. 为剩余任务创建 TodoWrite
```

### 步骤 1.5：构建静态上下文（计划级别）

静态上下文只构建一次，在所有任务分派中复用：

```
1. 读取计划头部 — 提取 Goal、Architecture、Tech Stack、Impact
2. 读取框架规则：
   a. 始终包含：CLAUDE.md 分析器规则摘要（8 条规则，约 15 行）
   b. 如果计划头部有 "Rules:" 字段：
      → 从 .claude/project-rules/ 读取每个列出的文件
      → 将其完整内容作为内联上下文包含
   c. 如果计划头部没有 "Rules:" 字段（向后兼容）：
      → 仅包含 CLAUDE.md 分析器规则摘要
      → 不要批量注入所有 project-rules（太大）
3. 读取设计文档：
   a. 如果计划头部有 "Design Ref:" 且不是 "None"：
      → 读取引用的文件
      → 包含相关章节
4. 从设计文档中提取审查约束（用于规格审查员）：
   a. 提取所有技术决策（TD-* 编号及其要求）
   b. 提取实体树结构图（[ComponentOf]/[ChildOf] 关系）
   c. 提取属性要求（如 [FriendOf] 声明列表）
   d. 存储为 `static_context.design_constraints` 以供审查员使用
5. 将所有内容存储为 `static_context` 以供复用
```

**static_context 包含：**
- 计划目标、架构、技术栈（来自头部）
- CLAUDE.md 分析器规则（始终包含）
- 相关 project-rules 内容（来自头部 Rules 字段）
- 设计文档摘录（如果 Design Ref 存在）
- 设计约束摘要（技术决策 + 结构要求 + 属性声明 -- 用于审查员注入）

同时初始化 `task_outputs = {}` — 一个映射，存储每个任务的完成输出以用于依赖解析。

### 步骤 2：执行任务（逐任务）

#### 2a. 构建任务上下文并分派实现者

对每个任务，在 static_context 基础上构建任务特定的上下文包，然后分派：

**上下文包构建算法（逐任务）：**

```
1. 以 static_context 为基础（计划头部 + 框架规则 + 设计文档）
2. 处理任务的 Context 块：
   a. Depends: 对每个依赖的任务 ID：
      - 检索 task_outputs[dep_id]（摘要、变更文件、关键决策）
      - 如果依赖任务创建了对当前任务重要的新文件：
        → 读取其当前内容并包含
   b. Reads: 对每个文件路径：
      - 读取文件内容（如果路径包含 :N-M 则读取指定行范围）
      - 作为内联代码块包含在 prompt 中
   c. Why: 包含设计意图句子
3. 从下面的模板组装完整 prompt
```

**实现者 Prompt 模板：**

```
Agent(subagent_type="programmer", prompt="""
## 你的任务

### [task title]

**设计意图:** [Why field from task Context]

**进度:** Task [N] of [total]

### 任务详情

[Full task text from plan, verbatim — including Steps, code snippets, commands]

---

## 预加载上下文（直接使用，无需再读文件）

### 项目框架规则

[CLAUDE.md analyzer rules summary — 8 rules, always included]

[Content from each project-rules file declared in plan header Rules field]

### 计划全局信息

**Goal:** [from plan header]
**Architecture:** [from plan header]
**Tech Stack:** [from plan header]

### 设计文档摘要

[Design doc relevant sections — if Design Ref exists; otherwise omit this section]

### 前置任务产出

[For each task in Depends:]
**Task [dep_id] — [dep_title]:**
- Files: [created/modified file paths]
- Summary: [implementation summary from task_outputs]
- Key decisions: [from task_outputs]

[If a dependent task created files this task needs, include their content:]
```filepath
[file content]
```

[If no Depends: omit this section entirely]

### 现有代码（需要阅读/修改的文件）

[For each file in Reads:]
```filepath
[file content or relevant line range]
```

[If no Reads: omit this section entirely]

---

## 工作纪律

1. **TDD:** RED → GREEN → REFACTOR
   - 写失败测试 → 运行确认失败 → 写最小实现 → 运行确认通过
2. **编译验证:** [build command from plan or CLAUDE.md]
3. **Commit:** 完成后提交，消息描述做了什么

## 输出格式（必须遵守，编排器依赖此格式解析）

完成后，严格按以下格式输出：

### 实现摘要
[What you implemented, 2-3 sentences]

### 文件变更
- Created: [file paths, one per line]
- Modified: [file paths, one per line]

### 测试结果
[pass/fail counts, specific test names]

### 关键决策
[Any design decisions made during implementation, or "None"]

### 问题或顾虑
[Any concerns, or "None"]
""")
```

**如果实现者提问：**
- 在让他们继续之前清楚回答
- 如需要提供额外上下文

**如果实现者失败：**
- 用具体的错误上下文 + 相同的预加载上下文分派修复 Agent
- 不要手动修复（避免上下文污染）

#### 2a-post. 解析实现者输出

实现者返回后，解析其结构化输出：

```
1. 从输出中提取结构化章节：
   - 实现摘要 → task_outputs[task_id].summary
   - 文件变更 → task_outputs[task_id].files (Created + Modified lists)
   - 测试结果 → task_outputs[task_id].tests
   - 关键决策 → task_outputs[task_id].decisions
   - 问题或顾虑 → task_outputs[task_id].concerns
2. 存储到 task_outputs 映射，用于：
   - 提供给审查员（当前任务，立即）
   - 提供给依赖任务（后续任务通过 Depends）
3. 如果输出无结构（Agent 未遵循格式）：
   - 降级方案：从上次提交以来的 `git diff --name-only` 提取文件列表
   - 使用完整 Agent 输出作为摘要
```

#### 2b. 分派规格审查员

实现者完成后：

```
Agent(subagent_type="code-reviewer", prompt="""
  ## 规格合规审查

  ### 设计意图
  [Why field from task Context — 审查员需要理解"为什么"]

  ### 计划任务规格
  [Full task text from plan]

  ### 项目框架规则（与本任务相关）
  [Same framework rules injected to implementer — from static_context]

  ### 设计文档约束（必须对照检查）
  [From static_context.design_constraints — key technical decisions (TD-*),
   structure requirements, attribute requirements (e.g. [FriendOf], [ComponentOf], [ChildOf]),
   sub-seed isolation requirements, entity tree constraints.
   Only include decisions relevant to this task's scope.]

  ### 变更的文件
  [From implementer structured output — file paths]

  **重要：你必须使用 Read 工具读取上述每个文件的当前磁盘内容。不要依赖实现者摘要中引用的代码片段——它们可能是过期的。**

  ### 实现者摘要
  [From implementer structured output — 实现摘要 + 关键决策]

  检查：
  1. 规格中的所有需求已实现
  2. 没有超出规格的额外内容
  3. 实现遵循框架规则（对照上面注入的规则）
  4. 设计意图被保留（对照设计意图）
  5. 设计文档的技术决策和结构约束被遵循（对照设计文档约束，特别是属性声明、种子隔离、实体树关系）

  裁定: PASS / FAIL
  如果 FAIL: 列出具体的缺口或多余内容
""")
```

**如果规格审查失败：**
- 同一实现者 Agent 修复缺口（用相同的预加载上下文重新分派）
- 重新运行规格审查
- 循环直到 PASS

#### 2c. 分派代码质量审查员

规格审查通过后：

```
Agent(subagent_type="code-reviewer", prompt="""
  ## 代码质量审查

  ### 项目框架规则
  [Same framework rules from static_context — 审查员直接使用]

  ### 变更的文件
  [From implementer structured output — file paths]

  **重要：你必须使用 Read 工具读取上述每个文件的当前磁盘内容。不要依赖实现者摘要中引用的代码片段——它们可能是过期的。**

  ### 实现者摘要
  [From implementer structured output — 实现摘要 + 关键决策]

  检查：
  1. 代码遵循项目约定（对照上面注入的框架规则）
  2. 无反模式或代码异味
  3. 错误处理恰当
  4. 测试有意义（不是仅仅能通过）

  问题分级：
  - **Critical** — 必须修复
  - **Important** — 应当修复
  - **Suggestion** — 最好能改

  裁定: APPROVE / REQUEST CHANGES
""")
```

**如果质量审查要求修改：**
- 实现者修复问题（用相同的预加载上下文重新分派）
- 重新运行质量审查
- 循环直到 APPROVE

**Important 问题追踪：**

质量审查 APPROVE 后，如果报告中包含 Important 级别问题（未被要求修复），将其记录到 `important_issues` 累积列表：

```
important_issues.push({
  task_id: current_task_id,
  task_title: current_task_title,
  issues: [extracted Important issues from quality review output]
})
```

> **注意：** 只有 APPROVE 后的未修复 Important 问题才追踪。如果 REQUEST CHANGES 触发了修复循环，修复后重新审查中仍标注为 Important 的问题才需要追踪。

#### 2d. 标记任务完成

```
1. 更新 TodoWrite：标记任务为已完成
2. 如果 Change Mode：更新计划文件复选框（- [ ] → - [x]）
3. 记录："任务 N 完成。[来自 task_outputs 的简要摘要]"
```

### 步骤 3：最终审查

所有任务完成后：

```
Agent(subagent_type="code-reviewer", prompt="""
  ## 最终实现审查

  计划中的所有 [N] 个任务已全部实现。
  从整体角度审查完整实现。

  **计划:** [plan file path]
  **所有变更文件:** [aggregate list]

  检查：
  1. 所有计划任务已实现
  2. 任务之间正确集成
  3. 组件之间没有缺失的连接
  4. 整体架构合理

  裁定: APPROVE / REQUEST CHANGES
""")
```

### 步骤 4：完成

**写入技术债务记录（如有）：**

如果 `important_issues` 列表不为空（即执行过程中有未修复的 Important 问题），生成 tech-debt 文件：

```
如果是 Change Mode:
  写入: docs/changes/<name>/tech-debt.md

格式:
  # 技术债务 — [变更名称]

  > 以下 Important 级别问题在代码审查中被发现但未修复。
  > 请在后续迭代中处理，或在 archive 时确认。

  ## 未修复的 Important 问题

  ### Task [id] — [title]
  - **[I1]** [问题描述] （来自质量审查）
  - **[I2]** [问题描述]

  ### Task [id] — [title]
  - **[I1]** [问题描述]
```

然后在完成摘要中增加一行：
```
技术债务: [N] 个 Important 问题待处理 → 详见 docs/changes/<name>/tech-debt.md
```

**完成摘要：**

```
计划执行完成 — [plan name]

任务: [N/N] 完成
审查: 全部通过
最终审查: APPROVED
技术债务: [N] 个 Important 问题待处理（如有）

后续步骤:
1. /qx-verify — 在宣称完成前运行完整验证
2. /qx-finishing — 合并、PR 或保留分支
```

## --loop 模式（持久化执行）

使用 `--loop` 调用时：

```
/qx-dev-exec --loop "docs/changes/feature/plan.md"
```

**行为：**
- 在 `.claude/qx/state/exec.json` 创建状态文件
- 持续工作直到所有任务完成或被阻塞
- 如果会话结束，下次会话可从状态文件恢复
- 使用 `/qx-dev-stop` 停止

**状态文件格式：**
```json
{
  "active": true,
  "plan_path": "docs/changes/feature/plan.md",
  "mode": "change",
  "started_at": "2026-03-02T10:00:00Z",
  "current_task": 3,
  "total_tasks": 8,
  "completed_tasks": [1, 2],
  "failed_tasks": [],
  "session_id": "abc-123"
}
```

> **注意：** `--loop` 持久化模式需要配置 `qx-loop-hook`。没有 hook 时，`--loop` 降级为单会话执行。

## --worktree 模式（隔离执行）

使用 `--worktree` 调用时：

```
/qx-dev-exec --worktree "docs/changes/feature/plan.md"
```

**行为：**
- 执行任务前，通过 `EnterWorktree` 创建隔离的 git worktree
- 所有实现者子 Agent 在 worktree 中工作（不影响主工作区）
- 完成后，用户通过 `/qx-finishing` 选择合并或丢弃

**何时使用：**
- 可能破坏主工作区的大型或高风险变更
- 可能想要丢弃的实验性功能
- 并行开发：主工作区保持干净以进行其他工作

**何时不使用：**
- 小型、安全的变更（worktree 开销不值得）
- 想要立即应用的快速修复

**Agent 隔离：** 单个实现者子 Agent 也可以使用 `isolation: "worktree"` 进行逐任务隔离。这更重但能防止任务间相互干扰。当任务修改重叠文件时使用。

## 并行 vs 顺序执行

**默认：顺序执行** — 按计划顺序一次一个任务。

**何时并行化：**
- 计划明确标记任务可并行
- 任务没有共享文件或依赖
- 使用 `Agent` 工具的 `run_in_background: true` 进行并行分派
- 对可能冲突的并行任务考虑 `isolation: "worktree"`

**何时不并行化：**
- 任务修改相同文件
- 后续任务依赖前序任务的输出
- 任务顺序对集成有影响

## 错误处理

| 情况 | 处理方式 |
|------|----------|
| 实现者任务失败 | 用错误上下文分派修复 Agent |
| 规格审查连续失败 3 次 | 停止，报告给用户，请求计划澄清 |
| 质量审查发现 Critical 问题 | 必须修复后才能继续 |
| 任务后构建/测试失败 | 在下一个任务前调查 |
| 被不清楚的需求阻塞 | 停止，询问用户，回答后恢复 |

### Orchestrator 直接修复标准

当审查发现需要修复的问题时，orchestrator 可以选择直接修复（而非重新分派 Agent），但**仅限于以下情况**：

**可以直接修复：**
- 单行属性修改（如 `SceneType.Main` → `SceneType.Map`、添加 `[FriendOf(typeof(X))]`）
- Import / using 语句增删
- 常量值调整
- 明显的拼写或命名修正

**必须重新分派 Agent：**
- 涉及多个文件的架构性修改
- 算法或流程逻辑变更
- 新增文件或大段代码（>20 行）
- 需要运行测试验证的修复

直接修复后仍需重新运行审查（使用 haiku 模型快速复查即可）。

## 向后兼容

在此上下文注入更新之前创建的计划可能缺少 `**Context:**` 块或头部 `**Rules:**` 字段。优雅地处理：

**如果任务缺少 `**Context:**` 块：**
- `Depends`：从任务顺序推断（假设对前一个任务有顺序依赖）
- `Reads`：从 `**Files:** Modify:` 条目提取（如果任务修改现有文件，预读它们）
- `Why`：使用任务标题作为降级的设计意图

**如果计划头部缺少 `**Rules:**`：**
- 始终包含 CLAUDE.md 分析器规则摘要（8 条规则，约 15 行 — 始终安全）
- 不要批量注入所有 9 个 project-rules 文件（共 664 行 — 太大）
- 如果任务文本提到特定模式（如 "Component"、"Proto"、"async"），尝试推断相关规则

**如果计划头部缺少 `**Design Ref:**`：**
- Change Mode：检查 `docs/changes/<name>/design.md` 是否存在并读取
- Quick Mode：省略设计上下文

## 关键原则

- **预加载，而非延迟加载** — 编排器读取并注入所有上下文；子 Agent 永远不需要"去读 X"
- **每个任务一个全新 Agent** — 任务间无上下文污染
- **结构化输出** — 实现者返回可解析的输出用于依赖链
- **始终 TDD** — 每个任务都是 Red → Green → Refactor
- **两阶段审查** — 先规格合规，再代码质量
- **检查点进度** — 每个任务后更新 TodoWrite + 计划文件
- **阻塞时停止** — 不要猜测，去问
- **优雅恢复** — Change Mode 复选框支持跨会话恢复

## 危险信号

**绝不：**
- 告诉子 Agent "自己去读 CLAUDE.md" — 将规则注入到 prompt 中
- 跳过规格审查（"看起来差不多了"）
- 跳过代码质量审查（"我们赶时间"）
- 将多个实现者并行分派到相同文件
- 在审查失败后不修复就继续
- 未经用户同意在主分支上开始实现
- 只提供部分任务上下文给实现者（给出完整文本）

**始终：**
- 预读并将所有必要上下文注入子 Agent 的 prompt
- 给实现者计划中的完整任务文本
- 内联包含框架规则（来自 static_context）
- 对有 Depends 的任务包含依赖任务的输出
- 按顺序运行审查：先规格，再质量
- 修复后重新审查（不要跳过重新审查循环）
- 在 TodoWrite 和计划文件中双重跟踪进度（Change Mode）
- 将实现者输出解析到 task_outputs 中用于依赖链
- 所有任务完成时提供 `/qx-finishing` 选项

## 相关 Skills

- **qx-dev-plan** — 创建本 Skill 执行的计划
- **qx-tdd** — 实现者 Agent 遵循的 TDD 纪律
- **qx-code-review** — 审查员 Agent 使用的审查方法论
- **qx-verify** — 所有任务完成后的完整验证
- **qx-finishing** — 执行完成后的分支收尾
- **qx-dev-change** — 包装计划执行的变更生命周期
