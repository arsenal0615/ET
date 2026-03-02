---
name: qx-code-review
description: "Use when completing tasks, implementing major features, or before merging to verify work meets requirements. Dispatches multi-dimensional code review with project framework awareness."
---

# Code Review

## Overview

Dispatch code-reviewer subagent (and optionally security-reviewer) to catch issues before they cascade.

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
| **Code Quality + Plan Alignment + Framework Compliance** | `code-reviewer` | Always (mandatory) |
| **Security** | `security-reviewer` | When network/auth/input handling changed |

### Auto-Detection Rules

**Framework Rule Review triggered when changes touch:**
- Core framework patterns (read CLAUDE.md for project-specific patterns)
- Data model definitions or component structures
- System/logic classes
- Message handlers or protocol files
- Files in framework-specific directories

**Security Review triggered when changes touch:**
- Network message handlers
- Session management code
- Inter-process or inter-service messaging
- Input validation or serialization
- Cross-boundary communication

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

**3. Optionally dispatch security reviewer in parallel**

Use Agent tool with `subagent_type: "security-reviewer"` when security-sensitive code was changed

**4. Act on feedback:**
- Fix **Critical** issues immediately
- Fix **Important** issues before proceeding
- Note **Suggestion** issues for later
- Push back if reviewer is wrong (with reasoning)

## Code Review Template

When dispatching the code-reviewer agent, provide this context:

```
You are reviewing code changes for production readiness.

**What was implemented:** {DESCRIPTION}
**Requirements/Plan:** {PLAN_REFERENCE}
**Git range:** {BASE_SHA}..{HEAD_SHA}

IMPORTANT: Read the project's CLAUDE.md for framework-specific rules and conventions.

Review the git diff and check:

## Review Checklist

### Code Quality
- Clean separation of concerns?
- Proper error handling?
- Type safety?
- DRY principle followed?
- Edge cases handled?

### Framework Compliance
- Read CLAUDE.md for project-specific rules
- All framework coding conventions followed?
- Correct file/module placement per project architecture?
- Required attributes/annotations present?
- Build/compilation verification commands run?

### Architecture
- Sound design decisions?
- Scalability considerations?
- Performance implications?
- Concurrency model appropriate?

### Testing
- Tests actually test logic (not mocks)?
- Edge cases covered?
- All tests passing?

### Requirements
- All plan requirements met?
- Implementation matches spec?
- No scope creep?
- Code generation steps run if applicable?

## Output Format

### Strengths
[What's well done? Be specific with file:line references.]

### Issues

#### Critical (Must Fix)
[Bugs, security issues, data loss risks, broken functionality, framework rule violations]

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
| **Critical** | Framework rule violations, security holes, data loss risks, broken functionality | Must fix before proceeding |
| **Important** | Missing error handling, test gaps, wrong module placement, architectural issues | Should fix before merge |
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

[code-reviewer returns]:
  Strengths: Clean separation of data and logic
  Framework Compliance: All framework rules compliant
  Issues:
    Important: Destroy handler doesn't cancel active timers
    Suggestion: Consider using existing numeric system for stat modifications
    Suggestion: Consider adding lifecycle method annotations
  Assessment: Ready with fixes

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
- Skip framework rule review when core framework code changed

**If reviewer is wrong:**
- Push back with technical reasoning
- Show code/tests that prove it works
- Request clarification

## Related Skills

- **qx-verification** — Verification before claiming completion
- **qx-managing-changes** — Change lifecycle (verify phase triggers review)
- **qx-debugging** — When review reveals bugs needing investigation
- **qx-tdd** — For writing tests to cover gaps found in review
