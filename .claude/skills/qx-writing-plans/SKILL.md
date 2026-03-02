---
name: qx-writing-plans
description: "Use when you have a spec, design, or requirements for a multi-step task, before touching code. Creates bite-sized TDD implementation plans with ET framework awareness."
---

# Writing Plans

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for our codebase and questionable taste. Document everything they need to know: which files to touch for each task, code, testing, docs they might need to check, how to test it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

Assume they are a skilled developer, but know almost nothing about ET framework or the problem domain. Assume they don't know good test design very well.

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

**Tech Stack:** [Key technologies/assemblies affected]

**ET Impact:**
- Assemblies: [Model / ModelView / Hotfix / HotfixView]
- Proto changes: [Yes/No — if yes, run Proto2CS after]
- Excel changes: [Yes/No — if yes, run ExcelExporter after]
- New Components: [List ComponentOf declarations]
- New Messages: [List proto message types]

---
```

## Task Structure (Quick Mode)

````markdown
### Task N: [Component Name]

**Files:**
- Create: `Packages/cn.etetet.{pkg}/Scripts/Hotfix/Share/{File}.cs`
- Modify: `Packages/cn.etetet.{pkg}/Scripts/Model/Share/{File}.cs:123-145`
- Test: `path/to/test/file.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void TestSpecificBehavior()
{
    // Arrange
    var result = MyComponentSystem.Calculate(input);

    // Assert
    Assert.AreEqual(expected, result);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: FAIL with "method not found" or similar

**Step 3: Write minimal implementation**

```csharp
[FriendOf(typeof(MyComponent))]
public static partial class MyComponentSystem
{
    public static int Calculate(this MyComponent self, int input)
    {
        return input * self.Multiplier;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: PASS

**Step 5: Compile check**

Run: `dotnet build ET.sln`
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
- Create: `Packages/cn.etetet.{pkg}/Scripts/Hotfix/Share/{File}.cs`
- Test: `path/to/test/file.cs`

**Steps:**
1. Write failing test
2. Run to verify failure
3. Implement minimal code
4. Run to verify pass
5. Compile check (`dotnet build ET.sln`)
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

## ET Framework Plan Awareness

When writing plans for ET projects, ensure every plan addresses:

### Assembly Placement
For each new file, specify which assembly it belongs to:
- **ET.Model** — Component/Entity data definitions (fields only, no methods)
- **ET.ModelView** — Client-only data (Unity-dependent)
- **ET.Hotfix** — System classes, Handlers, logic (static classes only)
- **ET.HotfixView** — Client-only view logic

### Package Path Convention
Always use full paths:
```
Packages/cn.etetet.{package}/Scripts/{Assembly}/{Visibility}/{File}.cs
```
Where:
- `{Assembly}` = `Model` or `Hotfix`
- `{Visibility}` = `Client`, `Server`, or `Share`

### Proto & Config Steps
If plan involves new messages:
```markdown
### Task 0: Proto Definition (before any code)

**Step 1: Define proto messages**
- File: `Packages/cn.etetet.{pkg}/Proto/{Name}_{Direction}_{Opcode}.proto`

**Step 2: Generate C# code**
Run: `dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./`
Expected: Generated files in Proto output directory

**Step 3: Verify compilation**
Run: `dotnet build ET.sln`
Expected: 0 errors
```

If plan involves new Excel configs:
```markdown
### Task 0: Excel Config (before dependent code)

**Step 1: Add Excel rows**
- File: `Packages/cn.etetet.{pkg}/Excel/{ConfigName}.xlsx`

**Step 2: Export configs**
Run: `dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./`

**Step 3: Verify compilation**
Run: `dotnet build ET.sln`
Expected: 0 errors
```

### Component Declaration Tasks
When plan creates new Components:
```markdown
- [ ] N.1 Create Component data class

**File:** `Packages/cn.etetet.{pkg}/Scripts/Model/Share/{Name}Component.cs`

```csharp
[ComponentOf(typeof(ParentEntity))]
public class MyComponent : Entity, IAwake, IDestroy
{
    public int MyField;
}
```

- [ ] N.2 Create System class

**File:** `Packages/cn.etetet.{pkg}/Scripts/Hotfix/Share/{Name}ComponentSystem.cs`

```csharp
[EntitySystemOf(typeof(MyComponent))]
[FriendOf(typeof(MyComponent))]
public static partial class MyComponentSystem
{
    [EntitySystem]
    private static void Awake(this MyComponent self) { }

    [EntitySystem]
    private static void Destroy(this MyComponent self) { }
}
```
```

## Remember

- Exact file paths always (use ET package path convention)
- Complete code in plan (not "add validation")
- Exact commands with expected output
- DRY, YAGNI, TDD, frequent commits
- Every new Component needs both Model + Hotfix files
- Every proto change needs Proto2CS step
- Every Excel change needs ExcelExporter step
- Always include `dotnet build ET.sln` verification step

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
