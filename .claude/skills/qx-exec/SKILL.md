---
name: qx-exec
description: "Use when you have an approved implementation plan to execute. Reads the plan, dispatches sub-agents per task with TDD discipline, runs two-stage review (spec + code quality), and tracks progress. Supports --loop for persistent execution."
---

# 多 Agent 执行计划（Multi-Agent Plan Execution）

## Overview

Execute an approved implementation plan by dispatching fresh sub-agents per task. Each task follows TDD discipline, gets two-stage review (spec compliance + code quality), and progress is tracked via both TodoWrite and plan file checkboxes.

**Core principle:** Fresh agent per task + TDD + two-stage review = high quality, fast iteration.

**Announce at start:** "Using qx-exec to execute the plan at [plan path]."

## When to Use

- You have an approved plan file (from `/qx-plan` or `/qx-change design`)
- Tasks are mostly independent (can be worked on by separate agents)
- You want systematic execution with quality gates

## Architecture

```
/qx-exec "docs/changes/feature/plan.md"
     │
     ▼
┌─ QX Exec ────────────────────────────────────────┐
│                                                    │
│  1.  Read plan, extract all tasks                  │
│  2.  Detect mode (change vs quick)                 │
│  3.  Create TodoWrite with all tasks               │
│  1.5 Build static context (plan-level):            │
│      - Read plan header (goal, arch, tech stack)   │
│      - Read framework rules (CLAUDE.md + Rules:)   │
│      - Read design doc (Design Ref:)               │
│      - Initialize task_outputs = {}                │
│  4.  For each task:                                │
│      a. Build task context package:                │
│         - static_context (reused)                  │
│         - Depends → inject prior task outputs      │
│         - Reads → inject file contents             │
│         - Why → inject design intent               │
│      b. Dispatch implementer sub-agent             │
│         (with full pre-loaded context)             │
│      c. Parse implementer output → task_outputs    │
│      d. Dispatch spec reviewer (with rules inline) │
│      e. Dispatch quality reviewer (with rules)     │
│      f. Mark task complete (TodoWrite + checkbox)  │
│  5.  After all tasks: dispatch final review        │
│  6.  Offer /qx-finishing                           │
│                                                    │
│  --loop: Keep working until all tasks done         │
│          (persistent mode via hook)                │
└────────────────────────────────────────────────────┘
```

## The Process

### Step 1: Load and Validate Plan

```
1. Read the plan file
2. Determine mode:
   - Change Mode: plan at docs/changes/<name>/plan.md
     → checkbox tracking enabled (- [ ] → - [x])
     → check for existing progress (resume from last incomplete)
   - Quick Mode: plan at docs/plans/*.md
     → TodoWrite tracking only
3. Extract all tasks with full text
4. Check for progress (Change Mode):
   - Count - [x] vs - [ ] checkboxes
   - If progress exists: "N/M tasks complete, resuming from task K"
5. Create TodoWrite for remaining tasks
```

### Step 1.5: Build Static Context (Plan-Level)

Static context is built ONCE and reused across all task dispatches:

```
1. Read plan header — Extract Goal, Architecture, Tech Stack, Impact
2. Read framework rules:
   a. Always include: CLAUDE.md analyzer rules summary (8 rules, ~15 lines)
   b. If plan header has "Rules:" field:
      → Read each listed file from .claude/project-rules/
      → Include their full content as inline context
   c. If plan header has NO "Rules:" field (backward compat):
      → Only include CLAUDE.md analyzer rules summary
      → Do NOT bulk-inject all project-rules (too large)
3. Read design doc:
   a. If plan header has "Design Ref:" and it's not "None":
      → Read the referenced file
      → Include relevant sections
4. Store everything as `static_context` for reuse
```

**What static_context contains:**
- Plan goal, architecture, tech stack (from header)
- CLAUDE.md analyzer rules (always)
- Relevant project-rules content (from header Rules field)
- Design document excerpts (if Design Ref exists)

Also initialize `task_outputs = {}` — a map to store each task's completion output for dependency resolution.

### Step 2: Execute Tasks (Per Task)

#### 2a. Build Task Context & Dispatch Implementer

For each task, build a task-specific context package on top of static_context, then dispatch:

**Context Package Algorithm (per task):**

```
1. Start with static_context (plan header + framework rules + design doc)
2. Process task's Context block:
   a. Depends: For each dependent task ID:
      - Retrieve task_outputs[dep_id] (summary, files changed, key decisions)
      - If dependent task created new files important for this task:
        → Read their current content and include
   b. Reads: For each file path:
      - Read the file content (or specified line range if path includes :N-M)
      - Include in prompt as inline code block
   c. Why: Include the design intent sentence
3. Assemble the full prompt from template below
```

**Implementer Prompt Template:**

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

**If implementer asks questions:**
- Answer clearly before letting them proceed
- Provide additional context if needed

**If implementer fails:**
- Dispatch a fix agent with specific error context + the same pre-loaded context
- Don't try to fix manually (context pollution)

#### 2a-post. Parse Implementer Output

After implementer returns, parse its structured output:

```
1. Extract structured sections from output:
   - 实现摘要 → task_outputs[task_id].summary
   - 文件变更 → task_outputs[task_id].files (Created + Modified lists)
   - 测试结果 → task_outputs[task_id].tests
   - 关键决策 → task_outputs[task_id].decisions
   - 问题或顾虑 → task_outputs[task_id].concerns
2. Store in task_outputs map for:
   - Feeding to reviewers (this task, immediately)
   - Feeding to dependent tasks (future tasks via Depends)
3. If output is unstructured (agent didn't follow format):
   - Fallback: extract file list from `git diff --name-only` since last commit
   - Use the full agent output as summary
```

#### 2b. Dispatch Spec Reviewer

After implementer completes:

```
Agent(subagent_type="code-reviewer", prompt="""
  ## Spec Compliance Review

  ### 设计意图
  [Why field from task Context — reviewer needs to understand the "why"]

  ### Plan Task Spec
  [Full task text from plan]

  ### 项目框架规则（与本任务相关）
  [Same framework rules injected to implementer — from static_context]

  ### Files Changed
  [From implementer structured output — file paths]

  ### Implementer Summary
  [From implementer structured output — 实现摘要 + 关键决策]

  Check:
  1. All requirements in the spec are implemented
  2. Nothing extra was added beyond the spec
  3. Implementation follows framework rules (对照上面注入的规则)
  4. Design intent is preserved (对照设计意图)

  Verdict: PASS / FAIL
  If FAIL: List specific gaps or extras
""")
```

**If spec review fails:**
- Same implementer agent fixes the gaps (re-dispatch with same pre-loaded context)
- Re-run spec review
- Loop until PASS

#### 2c. Dispatch Code Quality Reviewer

After spec review passes:

```
Agent(subagent_type="code-reviewer", prompt="""
  ## Code Quality Review

  ### 项目框架规则
  [Same framework rules from static_context — reviewer uses these directly]

  ### Files Changed
  [From implementer structured output — file paths]

  ### Implementer Summary
  [From implementer structured output — 实现摘要 + 关键决策]

  Check:
  1. Code follows project conventions (对照上面注入的框架规则)
  2. No anti-patterns or code smells
  3. Error handling is appropriate
  4. Tests are meaningful (not just green)

  Classify issues:
  - **Critical** — Must fix
  - **Important** — Should fix
  - **Suggestion** — Nice to have

  Verdict: APPROVE / REQUEST CHANGES
""")
```

**If quality review requests changes:**
- Implementer fixes the issues (re-dispatch with same pre-loaded context)
- Re-run quality review
- Loop until APPROVE

#### 2d. Mark Task Complete

```
1. Update TodoWrite: mark task as completed
2. If Change Mode: update plan file checkbox (- [ ] → - [x])
3. Log: "Task N complete. [brief summary from task_outputs]"
```

### Step 3: Final Review

After all tasks complete:

```
Agent(subagent_type="code-reviewer", prompt="""
  ## Final Implementation Review

  All [N] tasks from the plan have been implemented.
  Review the ENTIRE implementation holistically.

  **Plan:** [plan file path]
  **All files changed:** [aggregate list]

  Check:
  1. All plan tasks are implemented
  2. Tasks integrate correctly with each other
  3. No missing connections between components
  4. Overall architecture is sound

  Verdict: APPROVE / REQUEST CHANGES
""")
```

### Step 4: Complete

```
Plan Execution Complete — [plan name]

Tasks: [N/N] complete
Reviews: All passed
Final review: APPROVED

Next steps:
1. /qx-verify — Run full verification before claiming done
2. /qx-finishing — Merge, PR, or keep branch
```

## --loop Mode (Persistent Execution)

When invoked with `--loop`:

```
/qx-exec --loop "docs/changes/feature/plan.md"
```

**Behavior:**
- Creates a state file at `.claude/qx/state/exec.json`
- Works continuously until all tasks are done or blocked
- If session ends, next session can resume from state file
- Stop with `/qx-stop`

**State file format:**
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

> **Note:** The `--loop` persistent mode requires the `qx-loop-hook` to be configured. Without the hook, `--loop` falls back to single-session execution.

## --worktree Mode (Isolated Execution)

When invoked with `--worktree`:

```
/qx-exec --worktree "docs/changes/feature/plan.md"
```

**Behavior:**
- Before executing tasks, creates an isolated git worktree via `EnterWorktree`
- All implementer sub-agents work in the worktree (no changes to main workspace)
- On completion, user chooses to merge or discard via `/qx-finishing`

**When to use:**
- Large or risky changes that might break the main workspace
- Experimental features you might want to discard
- Parallel development: main workspace stays clean for other work

**When NOT to use:**
- Small, safe changes (worktree overhead not worth it)
- Quick fixes that you want applied immediately

**Agent isolation:** Individual implementer sub-agents can also use `isolation: "worktree"` for per-task isolation. This is heavier but prevents tasks from interfering with each other. Use when tasks modify overlapping files.

## Parallel vs Sequential Execution

**Default: Sequential** — One task at a time, in plan order.

**When to parallelize:**
- Plan explicitly marks tasks as parallelizable
- Tasks have no shared files or dependencies
- Use `Agent` tool with `run_in_background: true` for parallel dispatch
- Consider `isolation: "worktree"` for parallel tasks that might conflict

**When NOT to parallelize:**
- Tasks modify the same files
- Later tasks depend on earlier task output
- Task order matters for integration

## Error Handling

| Situation | Action |
|-----------|--------|
| Implementer fails a task | Dispatch fix agent with error context |
| Spec review fails 3 times | Stop, report to user, ask for plan clarification |
| Quality review finds Critical issues | Must fix before proceeding |
| Build/test fails after task | Investigate before next task |
| Blocked on unclear requirement | Stop, ask user, resume after answer |

## Backward Compatibility

Plans created before this context-injection update may lack `**Context:**` blocks or header `**Rules:**` fields. Handle gracefully:

**If a task lacks `**Context:**` block:**
- `Depends`: Infer from task ordering (assume sequential dependency on the previous task)
- `Reads`: Extract from `**Files:** Modify:` entries (if task modifies existing files, pre-read them)
- `Why`: Use the task title as fallback design intent

**If plan header lacks `**Rules:**`:**
- Always include CLAUDE.md analyzer rules summary (8 rules, ~15 lines — always safe)
- Do NOT bulk-inject all 9 project-rules files (664 lines total — too large)
- If the task text mentions specific patterns (e.g., "Component", "Proto", "async"), try to infer the relevant rules

**If plan header lacks `**Design Ref:**`:**
- In Change Mode: check if `docs/changes/<name>/design.md` exists and read it
- In Quick Mode: omit design context

## Key Principles

- **Pre-load, don't lazy-load** — Orchestrator reads and injects all context; sub-agents never need to "go read X"
- **Fresh agent per task** — No context pollution between tasks
- **Structured output** — Implementer returns parseable output for dependency chain
- **TDD always** — Red → Green → Refactor for every task
- **Two-stage review** — Spec compliance first, then code quality
- **Checkpoint progress** — Update TodoWrite + plan file after each task
- **Stop when blocked** — Don't guess, ask
- **Resume gracefully** — Change Mode checkboxes enable cross-session resume

## Red Flags

**Never:**
- Tell sub-agent to "read CLAUDE.md yourself" — inject rules into prompt instead
- Skip spec review ("looks good enough")
- Skip code quality review ("we're in a hurry")
- Dispatch multiple implementers to the same files in parallel
- Proceed after a failed review without fixing issues
- Start implementation on the main branch without user consent
- Provide only partial task context to implementer (give full text)

**Always:**
- Pre-read and inject all necessary context into sub-agent prompts
- Give implementer the FULL task text from the plan
- Include framework rules inline (from static_context)
- Include dependent task outputs for tasks with Depends
- Run reviews in order: spec first, then quality
- Fix and re-review (don't skip the re-review loop)
- Track progress in both TodoWrite and plan file (Change Mode)
- Parse implementer output into task_outputs for dependency chain
- Offer `/qx-finishing` when all tasks are done

## Related Skills

- **qx-writing-plans** — Creates the plans this skill executes
- **qx-tdd** — TDD discipline that implementer agents follow
- **qx-code-review** — Review methodology used by reviewer agents
- **qx-verification** — Full verification after all tasks complete
- **qx-finishing** — Branch completion after execution is done
- **qx-managing-changes** — Change lifecycle that wraps plan execution
