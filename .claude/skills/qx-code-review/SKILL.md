---
name: qx-code-review
description: "Use when completing tasks, implementing major features, or before merging to verify work meets requirements. Dispatches multi-dimensional code review with ET framework awareness."
---

# Code Review

## Overview

Dispatch code-reviewer subagent (and optionally et-rule-reviewer + security-reviewer) to catch issues before they cascade.

**Core principle:** Review early, review often.

**Announce at start:** "Using qx-code-review to review the implementation."

## When to Request Review

**Mandatory:**
- After completing a major feature or plan task group
- Before merge to main branch
- After completing a change (`/qx-change verify` triggers this)

**Optional but valuable:**
- When stuck (fresh perspective)
- Before refactoring (baseline check)
- After fixing complex bug
- After each task in multi-agent execution

## Review Dimensions

QX code review supports up to three review dimensions:

| Dimension | Agent | When |
|-----------|-------|------|
| **Code Quality + Plan Alignment** | `code-reviewer` | Always (mandatory) |
| **ET Framework Compliance** | `et-rule-reviewer` | When ET code changed (auto-detect) |
| **Security** | `security-reviewer` | When network/auth/input handling changed |

### Auto-Detection Rules

**ET Rule Review triggered when changes touch:**
- Component or Entity definitions
- System classes (`*System.cs`)
- Message handlers (`*Handler.cs`)
- Proto files (`.proto`)
- Files in `Model/` or `Hotfix/` directories

**Security Review triggered when changes touch:**
- Network message handlers
- Session management code
- Actor message routing
- Input validation or serialization
- Cross-Fiber communication

## How to Request Review

**1. Get git SHAs:**
```bash
BASE_SHA=$(git rev-parse HEAD~1)  # or merge-base with main
HEAD_SHA=$(git rev-parse HEAD)
```

**2. Dispatch code-reviewer agent:**

Use Agent tool with `subagent_type: "code-reviewer"`, providing:
- What was implemented (description)
- Plan or requirements reference
- Git range (BASE_SHA..HEAD_SHA)

**3. Optionally dispatch ET rule reviewer and security reviewer in parallel**

Use Agent tool with `subagent_type: "et-rule-reviewer"` and/or `subagent_type: "security-reviewer"`

**4. Act on feedback:**
- Fix **Critical** issues immediately
- Fix **Important** issues before proceeding
- Note **Suggestion** issues for later
- Push back if reviewer is wrong (with reasoning)

## Code Review Template

When dispatching the code-reviewer agent, provide this context:

```
You are reviewing code changes for production readiness in an ET framework Unity/Server project.

**What was implemented:** {DESCRIPTION}
**Requirements/Plan:** {PLAN_REFERENCE}
**Git range:** {BASE_SHA}..{HEAD_SHA}

Review the git diff and check:

## Review Checklist

### Code Quality
- Clean separation of concerns?
- Proper error handling?
- Type safety?
- DRY principle followed?
- Edge cases handled?

### ET Framework Compliance
- Entity classes have no methods (logic in System classes)?
- Components have correct [ComponentOf] / [ChildOf]?
- System classes have [EntitySystemOf] + [FriendOf] + partial?
- No static fields without [StaticField]?
- Async methods return ETTask (not Task)?
- No `new` on Entity types (use AddComponent/AddChild)?
- Hotfix assembly has only static classes?
- Correct assembly placement (Model vs Hotfix)?
- Cross-Fiber communication uses Actor messages?

### Architecture
- Sound design decisions?
- Scalability considerations?
- Performance implications?
- Fiber scheduling appropriate?

### Testing
- Tests actually test logic (not mocks)?
- Edge cases covered?
- All tests passing?

### Requirements
- All plan requirements met?
- Implementation matches spec?
- No scope creep?
- Proto2CS run if proto changed?
- ExcelExporter run if config changed?

## Output Format

### Strengths
[What's well done? Be specific with file:line references.]

### Issues

#### Critical (Must Fix)
[Bugs, security issues, data loss risks, broken functionality, ET rule violations]

#### Important (Should Fix)
[Architecture problems, missing features, poor error handling, test gaps]

#### Suggestion (Nice to Have)
[Code style, optimization, documentation improvements]

**For each issue:**
- File:line reference
- What's wrong
- Why it matters
- How to fix (if not obvious)

### Assessment
**Ready to merge?** [Yes / No / With fixes]
**Reasoning:** [1-2 sentences]
```

## Issue Severity Guide

| Severity | Examples | Action |
|----------|----------|--------|
| **Critical** | Entity has methods, missing ComponentOf, cross-Fiber direct access, security hole, data loss | Must fix before proceeding |
| **Important** | Missing error handling, test gaps, wrong assembly placement, missing FriendOf | Should fix before merge |
| **Suggestion** | Naming convention, code organization, documentation | Track for later |

## Example Workflow

```
[Just completed Task 2: Add BuffComponent System]

You: Let me request code review.

BASE_SHA=$(git merge-base HEAD main)
HEAD_SHA=$(git rev-parse HEAD)

[Dispatch code-reviewer agent]
  Description: BuffComponent with add/remove/query and duration timer
  Plan: docs/changes/buff-system/plan.md, Task 2
  Range: a7981ec..3df7661

[Dispatch et-rule-reviewer agent in parallel]
  Range: a7981ec..3df7661
  Focus: Component/System declarations, attribute compliance

[code-reviewer returns]:
  Strengths: Clean System class separation, proper FriendOf
  Issues:
    Important: BuffComponent.Destroy doesn't cancel active timers
    Suggestion: Consider using NumericComponent for buff stat modifications
  Assessment: Ready with fixes

[et-rule-reviewer returns]:
  Pass: All ET rules compliant
  Note: Consider adding [EntitySystem] to Update method

You: [Fix timer cleanup in Destroy]
[Continue to Task 3]
```

## Integration with Workflows

**Multi-Agent Execution (`/qx-exec`):**
- Review after EACH task group
- Catch issues before they compound
- Fix before moving to next group

**Change Lifecycle (`/qx-change verify`):**
- Full three-dimension review
- Part of verification phase

**Ad-Hoc Development:**
- Review before merge
- Review when stuck

## Red Flags

**Never:**
- Skip review because "it's simple"
- Ignore Critical issues
- Proceed with unfixed Important issues
- Argue with valid technical feedback
- Skip ET rule review when Entity/Component code changed

**If reviewer is wrong:**
- Push back with technical reasoning
- Show code/tests that prove it works
- Request clarification

## Related Skills

- **qx-verification** — Verification before claiming completion
- **qx-managing-changes** — Change lifecycle (verify phase triggers review)
- **qx-debugging** — When review reveals bugs needing investigation
- **qx-tdd** — For writing tests to cover gaps found in review
