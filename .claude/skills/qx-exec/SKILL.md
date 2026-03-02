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
│  1. Read plan, extract all tasks                   │
│  2. Detect mode (change vs quick)                  │
│  3. Create TodoWrite with all tasks                │
│  4. For each task:                                 │
│     a. Dispatch implementer sub-agent              │
│        - Loads project rules (CLAUDE.md, MEMORY)   │
│        - Follows TDD discipline                    │
│        - Implements, tests, commits                │
│     b. Dispatch spec reviewer sub-agent            │
│        - Verifies code matches plan spec           │
│     c. Dispatch code quality reviewer sub-agent    │
│        - Checks quality, patterns, maintainability │
│     d. Mark task complete (TodoWrite + checkbox)   │
│  5. After all tasks: dispatch final review         │
│  6. Offer /qx-finishing                            │
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

### Step 2: Execute Tasks (Per Task)

#### 2a. Dispatch Implementer

Dispatch a sub-agent for each task:

```
Agent(subagent_type="programmer", prompt="""
  ## Task: [task title]

  [Full task text from plan, verbatim]

  ## Context
  - Plan: [plan file path]
  - This is task [N] of [total]
  - Previous tasks completed: [list]

  ## Rules
  1. Read project CLAUDE.md for framework rules
  2. Follow TDD discipline:
     - RED: Write a failing test first
     - GREEN: Write minimum code to pass
     - REFACTOR: Clean up while tests pass
  3. Run verification commands after implementation
  4. Commit your work with a descriptive message

  ## Output
  Return:
  - What you implemented
  - Test results (pass/fail counts)
  - Any concerns or questions
  - Files changed
""")
```

**If implementer asks questions:**
- Answer clearly before letting them proceed
- Provide additional context if needed

**If implementer fails:**
- Dispatch a fix agent with specific error context
- Don't try to fix manually (context pollution)

#### 2b. Dispatch Spec Reviewer

After implementer completes:

```
Agent(subagent_type="code-reviewer", prompt="""
  ## Spec Compliance Review

  Review whether the implementation matches the plan specification.

  **Plan task spec:**
  [Full task text from plan]

  **Files changed:**
  [List from implementer output]

  Check:
  1. All requirements in the spec are implemented
  2. Nothing extra was added beyond the spec
  3. The implementation approach matches what was planned

  Verdict: PASS / FAIL
  If FAIL: List specific gaps or extras
""")
```

**If spec review fails:**
- Same implementer agent fixes the gaps
- Re-run spec review
- Loop until PASS

#### 2c. Dispatch Code Quality Reviewer

After spec review passes:

```
Agent(subagent_type="code-reviewer", prompt="""
  ## Code Quality Review

  Review the implementation for code quality.

  **Files changed:**
  [List from implementer output]

  Check:
  1. Code follows project conventions (read CLAUDE.md)
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
- Implementer fixes the issues
- Re-run quality review
- Loop until APPROVE

#### 2d. Mark Task Complete

```
1. Update TodoWrite: mark task as completed
2. If Change Mode: update plan file checkbox (- [ ] → - [x])
3. Log: "Task N complete. [brief summary]"
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

## Key Principles

- **Fresh agent per task** — No context pollution between tasks
- **TDD always** — Red → Green → Refactor for every task
- **Two-stage review** — Spec compliance first, then code quality
- **Checkpoint progress** — Update TodoWrite + plan file after each task
- **Stop when blocked** — Don't guess, ask
- **Resume gracefully** — Change Mode checkboxes enable cross-session resume

## Red Flags

**Never:**
- Skip spec review ("looks good enough")
- Skip code quality review ("we're in a hurry")
- Dispatch multiple implementers to the same files in parallel
- Proceed after a failed review without fixing issues
- Start implementation on the main branch without user consent
- Provide only partial task context to implementer (give full text)

**Always:**
- Give implementer the FULL task text from the plan
- Run reviews in order: spec first, then quality
- Fix and re-review (don't skip the re-review loop)
- Track progress in both TodoWrite and plan file (Change Mode)
- Offer `/qx-finishing` when all tasks are done

## Related Skills

- **qx-writing-plans** — Creates the plans this skill executes
- **qx-tdd** — TDD discipline that implementer agents follow
- **qx-code-review** — Review methodology used by reviewer agents
- **qx-verification** — Full verification after all tasks complete
- **qx-finishing** — Branch completion after execution is done
- **qx-managing-changes** — Change lifecycle that wraps plan execution
