---
name: qx-verification
description: "Use when about to claim work is complete, fixed, or passing, before committing or creating PRs. Requires running verification commands and confirming output before making any success claims. Evidence before assertions, always."
---

# Verification Before Completion

## Overview

Claiming work is complete without verification is dishonesty, not efficiency.

**Core principle:** Evidence before claims, always.

**Violating the letter of this rule is violating the spirit of this rule.**

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

If you haven't run the verification command in this message, you cannot claim it passes.

## The Gate Function

```
BEFORE claiming any status or expressing satisfaction:

1. IDENTIFY: What command proves this claim?
2. RUN: Execute the FULL command (fresh, complete)
3. READ: Full output, check exit code, count failures
4. VERIFY: Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
5. ONLY THEN: Make the claim

Skip any step = lying, not verifying
```

## ET Framework Verification Commands

### Compilation
```bash
# Full solution build (catches Analyzer errors, type mismatches, assembly violations)
dotnet build ET.sln

# Expected: Build succeeded. 0 Error(s)
```

### Unity Hot-Update Compilation
```
# In Unity Editor:
# F6 — Compile hot-update DLLs (Model + ModelView + Hotfix + HotfixView)
# Must show: Compile success in Unity Console
```

### Proto Code Generation
```bash
# After any .proto file changes
dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./

# Verify: Generated C# files match proto definitions
# Then: dotnet build ET.sln (must still pass)
```

### Excel Config Export
```bash
# After any Excel config changes
dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./

# Verify: Config files generated without errors
# Then: dotnet build ET.sln (must still pass)
```

### Test Execution
```bash
# Run specific tests
dotnet test --filter "TestName" -v n

# Run all tests
dotnet test -v n

# Expected: Passed! X total, X passed, 0 failed
```

### Server Startup Verification
```bash
# Quick smoke test — server starts without crash
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=StartConfig/Localhost --Console=1

# Expected: Server running, no exceptions in first 5 seconds
# Ctrl+C to stop
```

## Common Failures

| Claim | Requires | Not Sufficient |
|-------|----------|----------------|
| Tests pass | `dotnet test` output: 0 failures | Previous run, "should pass" |
| Code compiles | `dotnet build ET.sln`: 0 errors | Linter passing, "looks right" |
| F6 compiles | Unity Console: Compile success | dotnet build (different pipeline) |
| Proto generated | Proto2CS output + build passes | "I updated the proto file" |
| Config exported | ExcelExporter output + build passes | "I updated the Excel" |
| Bug fixed | Test original symptom: passes | "Code changed, assumed fixed" |
| Regression test works | Red-green cycle verified | Test passes once |
| Agent completed | VCS diff shows correct changes | Agent reports "success" |
| Requirements met | Line-by-line checklist | "Tests passing" |
| ET rules followed | `dotnet build` with Analyzers: 0 errors | "I followed the pattern" |

## ET-Specific Verification Checklist

When verifying ET framework code, check ALL applicable items:

### Component/Entity Changes
- [ ] `dotnet build ET.sln` passes (Analyzer catches rule violations)
- [ ] `[ComponentOf]` / `[ChildOf]` matches actual parent usage
- [ ] System class has `[EntitySystemOf]` + `[FriendOf]` + `partial`
- [ ] No methods in Entity classes
- [ ] No `new` on Entity types

### Message/Handler Changes
- [ ] Proto2CS run after `.proto` changes
- [ ] `dotnet build ET.sln` passes after generation
- [ ] `[MessageHandler(SceneType.X)]` matches receiver scene
- [ ] Request/Response types linked with `// ResponseType`

### Config Changes
- [ ] ExcelExporter run after Excel changes
- [ ] `dotnet build ET.sln` passes after export
- [ ] Config values accessible at runtime

### Cross-Fiber Changes
- [ ] No direct Entity access across Fibers
- [ ] Actor messages used for cross-Fiber communication
- [ ] MailBoxComponent added to entities that receive Actor messages

### Hot-Reload Safety
- [ ] No static fields without `[StaticField]`
- [ ] Hotfix assembly has only static classes (or `[EnableClass]`)
- [ ] F6 compile succeeds in Unity Editor

## Red Flags - STOP

- Using "should", "probably", "seems to"
- Expressing satisfaction before verification ("Great!", "Perfect!", "Done!")
- About to commit/push/PR without verification
- Trusting agent success reports
- Relying on partial verification
- Thinking "just this once"
- **ANY wording implying success without having run verification**

## Rationalization Prevention

| Excuse | Reality |
|--------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence is not evidence |
| "Just this once" | No exceptions |
| "dotnet build passed" | Build is not test. Run tests too. |
| "Agent said success" | Verify independently |
| "Partial check is enough" | Partial proves nothing |
| "I followed the ET pattern" | Analyzers catch what eyes miss. Build it. |
| "F6 compiled" | F6 is not `dotnet build`. Run both if applicable. |

## Key Patterns

**Tests:**
```
Correct: [Run dotnet test] [See: 34/34 pass] "All tests pass"
Wrong:   "Should pass now" / "Looks correct"
```

**Regression tests (TDD Red-Green):**
```
Correct: Write -> Run (pass) -> Revert fix -> Run (MUST FAIL) -> Restore -> Run (pass)
Wrong:   "I've written a regression test" (without red-green verification)
```

**Build:**
```
Correct: [Run dotnet build ET.sln] [See: Build succeeded. 0 Error(s)] "Build passes"
Wrong:   "Code looks correct" (eyes don't catch Analyzer errors)
```

**ET Analyzers:**
```
Correct: [Run dotnet build] [See 0 warnings from ET Analyzers] "ET rules compliant"
Wrong:   "I followed the ComponentOf pattern" (without building)
```

**Requirements:**
```
Correct: Re-read plan -> Create checklist -> Verify each item -> Report gaps or completion
Wrong:   "Tests pass, phase complete" (tests don't verify all requirements)
```

**Agent delegation:**
```
Correct: Agent reports success -> Check git diff -> Verify changes -> Build -> Test -> Report
Wrong:   Trust agent report
```

## When To Apply

**ALWAYS before:**
- ANY variation of success/completion claims
- ANY expression of satisfaction
- ANY positive statement about work state
- Committing, PR creation, task completion
- Moving to next task
- Delegating to agents

**Rule applies to:**
- Exact phrases
- Paraphrases and synonyms
- Implications of success
- ANY communication suggesting completion/correctness

## The Bottom Line

**No shortcuts for verification.**

Run the command. Read the output. THEN claim the result.

This is non-negotiable.

## Related Skills

- **qx-code-review** — Multi-dimensional review (triggered during verification)
- **qx-tdd** — Red-green cycle is a form of verification
- **qx-debugging** — When verification reveals failures
- **qx-managing-changes** — Verify phase in change lifecycle
