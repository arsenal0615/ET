---
name: qx-three-party-review
description: "Use when a design document, GDD, proposal, or plan needs multi-perspective review before proceeding. Orchestrates a structured review with 3 role-based reviewers providing diverse expert feedback."
---

# 开三方评审（Three-Party Review）

## Overview

Orchestrate a structured multi-perspective review of design documents, proposals, or plans. Three role-based reviewers each bring a different lens — strategic, technical, and adversarial — to surface blind spots before committing to implementation.

**Core principle:** Three lenses catch what one misses. Strategic + Technical + Adversarial = comprehensive review.

**Announce at start:** "Using qx-three-party-review to conduct a three-party review of [document]."

## When to Use

- GDD or feature design is complete and needs validation before story breakdown
- Technical proposal or spec needs cross-role review
- UX design needs feedback from non-UX perspectives
- Any significant document before it becomes the basis for implementation work
- Sprint plan needs validation before commitment

## Preflight

1. Identify the document to review (must exist as a file)
2. Read the document fully
3. Determine the document type to select appropriate reviewer perspectives

## The Process

### Step 1: Select Three Reviewers

Based on document type, select 3 reviewers from the QX role pool:

**For Game Design Documents (GDD):**

| Reviewer | Role | Lens |
|----------|------|------|
| **Strategic** | game-designer | Player experience, market fit, fun factor |
| **Technical** | programmer | Feasibility, performance, architecture implications |
| **Adversarial** | qa-tester | Edge cases, exploits, what could go wrong |

**For Technical Proposals / Specs:**

| Reviewer | Role | Lens |
|----------|------|------|
| **Strategic** | game-designer | Does this serve the game vision? |
| **Technical** | programmer | Architecture, patterns, maintainability |
| **Adversarial** | qa-tester | Testability, failure modes, missing requirements |

**For UX Design:**

| Reviewer | Role | Lens |
|----------|------|------|
| **Strategic** | game-designer | Player needs, game flow impact |
| **Technical** | programmer | Implementation feasibility, platform constraints |
| **User Advocate** | artist-ux | Accessibility, consistency, interaction quality |

**For Sprint Plans:**

| Reviewer | Role | Lens |
|----------|------|------|
| **Strategic** | game-designer | Priority alignment with game vision |
| **Technical** | programmer | Estimate accuracy, dependency risks |
| **Quality** | qa-tester | Test coverage gaps, risk areas |

### Step 2: Dispatch Reviewers

Dispatch 3 reviewer sub-agents in parallel using the Agent tool:

```
For each reviewer:
  Agent(subagent_type="[role-agent]", prompt="""
    You are reviewing [document path] as the [lens] reviewer.

    Your review lens: [lens description]

    Read the document, then provide:

    ## [Lens] Review

    ### Strengths
    - [What this document does well from your perspective]

    ### Concerns
    For each concern, classify severity:
    - **Critical** — Must fix before proceeding
    - **Important** — Should address, but not blocking
    - **Suggestion** — Nice to have improvement

    ### Questions
    - [Anything unclear that needs author clarification]

    ### Verdict
    APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES
  """)
```

> **Note:** Use the QX role Agents (game-designer, programmer, artist-ux, qa-tester) as reviewer types. These agents have their respective domain knowledge loaded.

### Step 3: Synthesize Reviews

After all 3 reviewers return, synthesize their findings:

```markdown
## Three-Party Review Summary

**Document:** [name]
**Date:** [date]
**Reviewers:** [3 names with roles]

### Consensus Verdict
[APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES]

### Critical Issues (must fix)
1. [Issue] — raised by [reviewer]
2. ...

### Important Issues (should address)
1. [Issue] — raised by [reviewer]
2. ...

### Suggestions
1. [Suggestion] — raised by [reviewer]
2. ...

### Conflicting Opinions
[Where reviewers disagree, note both positions]

### Questions Requiring Author Response
1. [Question] — from [reviewer]
2. ...
```

### Step 4: Present to Author

Present the synthesized review to the user:

```
Three-Party Review Complete — [Document Name]

Verdict: [APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES]
  Critical: [N] issues
  Important: [N] issues
  Suggestions: [N]

[If REQUEST CHANGES]: Must address [N] critical issues before proceeding.
[If APPROVE WITH CONDITIONS]: Can proceed, but should address [N] important issues.
[If APPROVE]: Ready to proceed to next phase.
```

### Step 5: Save Review Record

Save to: `docs/reviews/[document-name]-review-[date].md`

### Step 6: Offer Next Steps

Based on verdict:

**If APPROVE:**
- "Ready for next phase. What would you like to do?"
- Suggest appropriate next skill (e.g., `/qx-stories` for GDD, `/qx-plan` for specs)

**If APPROVE WITH CONDITIONS:**
- "Can proceed, but recommend addressing [N] important issues first."
- Offer: "Address issues now, or proceed and track as follow-up?"

**If REQUEST CHANGES:**
- "Must address [N] critical issues before proceeding."
- List the critical issues
- "Would you like to revise the document now?"

## Review Quality Standards

**Good reviews:**
- Cite specific sections of the document
- Explain WHY something is a concern, not just WHAT
- Propose alternatives when raising issues
- Acknowledge strengths, not just problems

**Bad reviews:**
- Vague ("this needs work")
- Only negative (no strengths noted)
- Off-lens (technical reviewer critiquing art direction)
- Rubber-stamping (APPROVE with no substance)

## Key Principles

- **Three perspectives always** — Never skip to fewer reviewers
- **Parallel dispatch** — Run all 3 reviews concurrently for speed
- **Severity classification** — Every concern gets Critical/Important/Suggestion
- **Conflict is valuable** — Disagreements between reviewers surface important trade-offs
- **Author decides** — Reviews inform, author makes final call
- **Evidence-based** — Reviews cite specific document sections

## Related Skills

- **qx-game-design** — Creates GDDs that get reviewed
- **qx-writing-plans** — Creates plans that get reviewed
- **qx-ux-design** — Creates UX specs that get reviewed
- **qx-code-review** — Code-level review (post-implementation)
