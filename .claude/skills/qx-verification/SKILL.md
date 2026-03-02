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

## Verification Commands

> **IMPORTANT:** Read the project's CLAUDE.md for the specific build, test, and verification commands for this project. The commands below are generic patterns — adapt to your project.

### Compilation / Build
```bash
# Build the project (use project-specific command from CLAUDE.md)
# Expected: Build succeeded. 0 Error(s)
```

### Code Generation
```bash
# If project uses code generation (proto, config export, etc.)
# Run the generation command from CLAUDE.md after definition changes
# Then: rebuild to verify generated code compiles
```

### Test Execution
```bash
# Run specific tests
dotnet test --filter "TestName" -v n

# Run all tests
dotnet test -v n

# Expected: Passed! X total, X passed, 0 failed
```

### Smoke Test
```bash
# Quick runtime verification — application starts without crash
# Use project-specific startup command from CLAUDE.md
```

## Common Failures

| Claim | Requires | Not Sufficient |
|-------|----------|----------------|
| Tests pass | Test runner output: 0 failures | Previous run, "should pass" |
| Code compiles | Build output: 0 errors | Linter passing, "looks right" |
| Code generated | Generation output + build passes | "I updated the definition file" |
| Bug fixed | Test original symptom: passes | "Code changed, assumed fixed" |
| Regression test works | Red-green cycle verified | Test passes once |
| Agent completed | VCS diff shows correct changes | Agent reports "success" |
| Requirements met | Line-by-line checklist | "Tests passing" |
| Framework rules followed | Build with analyzers: 0 errors | "I followed the pattern" |

## Framework-Specific Verification

> **IMPORTANT:** Read CLAUDE.md for the complete list of project-specific verification items. Common categories:

### Data Model / Component Changes
- [ ] Build passes (analyzers catch rule violations)
- [ ] Ownership declarations match actual usage
- [ ] Logic module has required annotations and markers
- [ ] No methods in data-only classes (if project enforces this)

### Message / Handler Changes
- [ ] Code generation run after definition changes
- [ ] Build passes after generation
- [ ] Handler annotations match target context
- [ ] Request/Response types properly linked

### Config / Data Changes
- [ ] Export/generation run after source changes
- [ ] Build passes after export
- [ ] Values accessible at runtime

### Cross-Boundary Changes
- [ ] No direct access across isolation boundaries
- [ ] Proper inter-process messaging used
- [ ] Required infrastructure components present

### Hot-Reload / Live-Update Safety
- [ ] No unsafe static state
- [ ] Logic modules follow required patterns
- [ ] Live compilation succeeds

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
| "Build passed" | Build is not test. Run tests too. |
| "Agent said success" | Verify independently |
| "Partial check is enough" | Partial proves nothing |
| "I followed the pattern" | Analyzers catch what eyes miss. Build it. |

## Key Patterns

**Tests:**
```
Correct: [Run test command] [See: 34/34 pass] "All tests pass"
Wrong:   "Should pass now" / "Looks correct"
```

**Regression tests (TDD Red-Green):**
```
Correct: Write -> Run (pass) -> Revert fix -> Run (MUST FAIL) -> Restore -> Run (pass)
Wrong:   "I've written a regression test" (without red-green verification)
```

**Build:**
```
Correct: [Run build command] [See: Build succeeded. 0 Error(s)] "Build passes"
Wrong:   "Code looks correct" (eyes don't catch analyzer errors)
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
