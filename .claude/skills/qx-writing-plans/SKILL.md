---
name: qx-writing-plans
description: "Use when you have a spec, design, or requirements for a multi-step task, before touching code. Creates bite-sized TDD implementation plans with project framework awareness."
---

# Writing Plans

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for our codebase and questionable taste. Document everything they need to know: which files to touch for each task, code, testing, docs they might need to check, how to test it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

Assume they are a skilled developer, but know almost nothing about the project's framework or the problem domain. Assume they don't know good test design very well.

**Announce at start:** "Using qx-writing-plans to create the implementation plan."

## Change Context Detection

Before writing the plan, detect whether you're working within a change:

1. **Check conversation context** — Has the user mentioned a change name? Have you been working on a change in `docs/changes/<name>/`?
2. **Check for explicit path** — Did the user reference a file inside `docs/changes/`?

**If change context detected (Change Mode):**
- Read `docs/changes/<name>/proposal.md`, `design.md`, and `specs/` as input context
- Save plan to: `docs/changes/<name>/plan.md`
- Use checkbox format for tasks (see Change Mode Task Structure below)

**If no change context (Quick Mode):**
- Save plans to: `docs/plans/YYYY-MM-DD-<feature-name>.md`
- Use original task format (see Task Structure below)

## Project Context Loading

**Before writing any plan, read the project's CLAUDE.md and MEMORY.md to understand:**
- Framework rules and coding conventions
- Module/assembly organization and placement rules
- Code generation steps (proto compilation, config export, etc.)
- Build and verification commands
- File naming conventions
- Key patterns and anti-patterns

## Bite-Sized Task Granularity

**Each step is one action (2-5 minutes):**
- "Write the failing test" - step
- "Run it to make sure it fails" - step
- "Implement the minimal code to make the test pass" - step
- "Run the tests and make sure they pass" - step
- "Commit" - step

## Plan Document Header

**Every plan MUST start with this header:**

```markdown
# [Feature Name] Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-exec` to implement this plan task-by-task.

**Goal:** [One sentence describing what this builds]

**Architecture:** [2-3 sentences about approach]

**Tech Stack:** [Key technologies/modules affected]

**Impact:**
- Modules/Assemblies: [list affected modules]
- Code generation changes: [Yes/No — if yes, which generation steps needed]
- New data models: [list with ownership declarations]
- New messages/protocols: [list types]

---
```

## Task Structure (Quick Mode)

````markdown
### Task N: [Component Name]

**Files:**
- Create: `path/to/new/file.cs`
- Modify: `path/to/existing/file.cs:123-145`
- Test: `path/to/test/file.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void TestSpecificBehavior()
{
    // Arrange
    var result = MySystem.Calculate(input);

    // Assert
    Assert.AreEqual(expected, result);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: FAIL with "method not found" or similar

**Step 3: Write minimal implementation**

```csharp
public static class MySystem
{
    public static int Calculate(int input)
    {
        return input * multiplier;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: PASS

**Step 5: Compile check**

Run: [project build command from CLAUDE.md]
Expected: Build succeeded, 0 errors

**Step 6: Commit**

```bash
git add <specific files>
git commit -m "feat: add specific feature"
```
````

## Change Mode Task Structure

When writing a plan for a change (`docs/changes/<name>/plan.md`), use checkbox format for persistent tracking:

````markdown
## 1. [Group Name]

- [ ] 1.1 [Task description]

**Files:**
- Create: `path/to/new/file.cs`
- Test: `path/to/test/file.cs`

**Steps:**
1. Write failing test
2. Run to verify failure
3. Implement minimal code
4. Run to verify pass
5. Compile check (project build command)
6. Commit

- [ ] 1.2 [Next task description]
...

## 2. [Next Group]

- [ ] 2.1 [Task description]
...
````

**Key differences from quick mode:**
- Tasks use `- [ ]` checkbox format (updated to `- [x]` during execution)
- Tasks are numbered as `N.M` (group.task)
- Groups are numbered `## N. Group Name`
- Progress persists across sessions via checkbox state

## Framework-Aware Planning

> **IMPORTANT:** Read CLAUDE.md for project-specific rules. Adapt the patterns below to your project's framework.

When writing plans, ensure every plan addresses project-specific concerns:

### Module/Assembly Placement
For each new file, specify which module or assembly it belongs to based on the project's architecture (read CLAUDE.md for the specific module organization).

### File Path Convention
Always use full paths following the project's directory structure conventions.

### Code Generation Steps
If plan involves new protocol/message definitions or config data:
- Include a task for defining the source (proto files, Excel, etc.)
- Include a task for running the code generation command
- Include a verification step to confirm generated code compiles

### Data Model Declaration Tasks
When plan creates new data models, include tasks for:
- Creating the data definition with proper ownership annotations
- Creating the associated logic module with required markers
- Verifying compilation passes

## Remember

- Exact file paths always (follow project path conventions)
- Complete code in plan (not "add validation")
- Exact commands with expected output
- DRY, YAGNI, TDD, frequent commits
- Every new data model needs both definition + logic files
- Every code generation change needs a generation step
- Always include build verification step

## Execution Handoff

After saving the plan, offer execution choice:

**Change Mode:**

**"Plan complete and saved to `docs/changes/<name>/plan.md`. Two execution options:**

**1. Multi-Agent Execution (this session)** — Use `/qx-exec` to dispatch agents per task, review between tasks, fast iteration

**2. New Session** — Open new session, load plan, execute with checkpoints. Progress tracked via checkboxes — resume anytime.

**Which approach?"**

**Quick Mode:**

**"Plan complete and saved to `docs/plans/<filename>.md`. Two execution options:**

**1. Multi-Agent Execution (this session)** — Use `/qx-exec` to dispatch agents per task with review

**2. New Session** — Open new session, load plan, execute with checkpoints

**Which approach?"**

## Related Skills

- **qx-tdd** — For test-driven discipline during plan execution
- **qx-code-review** — For reviewing completed tasks
- **qx-exec** — For executing plans with multi-agent orchestration
- **qx-managing-changes** — For change lifecycle (plan is one phase)
