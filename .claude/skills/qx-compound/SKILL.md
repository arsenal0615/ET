---
name: qx-compound
description: "Use after completing a feature, fixing a bug, or finishing a sprint to extract learnings and update knowledge bases. Implements compound accumulation — each piece of work makes the next one easier."
---

# 复合积累（Compound Accumulation）

## Overview

After completing a piece of work, extract the learnings and feed them back into the project's knowledge systems. This creates a compound effect — each feature, bugfix, or sprint makes the next one faster and higher quality.

**Core principle:** Every completed task is a learning opportunity. Capture it, or lose it.

**Announce at start:** "Using qx-compound to extract learnings from [completed work]."

## When to Use

- After a change is verified and archived (`/qx-change archive`)
- After a sprint retrospective
- After debugging a tricky issue
- After discovering a new pattern or anti-pattern
- When `/qx-finishing` or `/qx-retro` suggests it

## What Gets Updated

| Knowledge Store | What Goes There | How to Update |
|----------------|-----------------|---------------|
| **System Map** (`docs/system-map.md`) | System relationships, Component/Message/Event connections | Add new systems, update changed connections |
| **MEMORY.md** (`~/.claude/projects/.../memory/MEMORY.md`) | Code patterns, common pitfalls, debugging insights | Add verified patterns, update outdated entries |
| **Memory topic files** (`~/.claude/projects/.../memory/et-*.md`) | Detailed per-topic knowledge | Add deep-dive findings |
| **QX Knowledge Base** (`qx/knowledge/*.md`) | Generic methodology improvements | Update process insights (rare) |
| **Sprint data** (`docs/sprints/`) | Velocity, estimation accuracy | Updated by `/qx-sprint retro` |

## The Process

### Step 1: Identify the Completed Work

Determine what was just completed:
- Read the change directory (`docs/changes/[name]/`) if in change lifecycle
- Read recent git history for the relevant commits
- Read the sprint plan if in sprint context
- Ask the user if context is unclear

### Step 2: Extract Learnings

For each category, ask: "Did this work teach us anything new?"

#### 2a. System Relationships

**Questions:**
- Did we add a new system or Component?
- Did we discover connections between systems we didn't know about?
- Did we modify message flows or event chains?
- Did any existing system map entries turn out to be wrong?

**If yes:** Prepare system map updates.

#### 2b. Code Patterns

**Questions:**
- Did we find a pattern that worked well? (Reusable for future work)
- Did we hit a pitfall that others should avoid?
- Did we discover something about the framework that isn't documented?
- Did any existing MEMORY.md entries need correction?

**If yes:** Prepare MEMORY.md updates.

#### 2c. Process Insights

**Questions:**
- Did our estimation match reality? (For sprint velocity calibration)
- Did the implementation plan work well, or did we deviate?
- Did a specific QX skill help or hinder? How could it improve?
- Did we discover a better workflow pattern?

**If yes:** Note for retrospective or QX knowledge base.

#### 2d. Test Insights

**Questions:**
- Did we discover new test strategies that worked?
- Did we find edge cases we should always test for?
- Did a specific test approach save us from a bug?

**If yes:** Prepare QA knowledge base updates.

### Step 3: Validate Before Writing

**For each proposed update, verify:**
- Is this a confirmed pattern (not a one-off observation)?
- Does it contradict any existing knowledge? If so, which is correct?
- Is this specific enough to be actionable?
- Is this the right knowledge store for this information?

**Validation rules:**
- System map updates: Only after code is merged and tested
- MEMORY.md updates: Only for patterns confirmed across 2+ interactions
- QX knowledge base: Only for generic methodology (not project-specific)
- Exception: User explicitly asks to remember something → save immediately

### Step 4: Apply Updates

#### Update System Map

If system relationships changed:

```markdown
## [System Name]
- **Components**: [list]
- **Systems**: [list]
- **Messages**: [list with direction: C2G_, G2C_, etc.]
- **Dependencies**: [other systems this depends on]
- **Events**: [events published/consumed]
```

Read `docs/system-map.md`, find the relevant section, update or add entries.

#### Update MEMORY.md

If code patterns or pitfalls discovered:

Read current MEMORY.md. Find the appropriate section. Add the new entry, keeping the file concise (< 200 lines for the main file; use topic files for details).

Format for new entries:
```markdown
- **[Pattern/Pitfall name]** — [One-line description]
```

For detailed entries, add to the appropriate `et-*.md` topic file and link from MEMORY.md.

#### Update QX Knowledge Base

Only for generic methodology improvements (not project-specific):

Read the relevant `qx/knowledge/*.md` file. Add the process insight.

### Step 5: Report

```
Compound Accumulation Complete — [Work Name]

Updates applied:
  System Map: [N changes / no changes]
  MEMORY.md: [N entries added/updated / no changes]
  Knowledge Base: [N updates / no changes]

Key learnings captured:
1. [Learning 1]
2. [Learning 2]
```

## What NOT to Store

- **Session-specific context** — Current task details, temporary state
- **Unverified observations** — One-off behavior that might not reproduce
- **Project-agnostic methodology in MEMORY.md** — Goes in QX knowledge base
- **Project-specific tech in QX knowledge base** — Goes in MEMORY.md
- **Duplicates** — Check existing entries first

## Integration with QX Workflow

```
Feature complete → /qx-change verify → /qx-change archive → /qx-compound
                                                              ↓
Sprint complete → /qx-sprint retro ─────────────────────→ /qx-compound
                                                              ↓
Bug fixed → /qx-debug (solution found) ────────────────→ /qx-compound
                                                              ↓
                                                     Updates knowledge stores
                                                     Next work starts faster
```

## Key Principles

- **Verified only** — Don't store unconfirmed patterns
- **Specific and actionable** — "ETTask cancelation needs ETCancelToken" not "async is tricky"
- **Right store, right level** — Project-specific → MEMORY.md; Generic → QX knowledge
- **Concise** — One line where possible, detail files for deep content
- **Living documents** — Update or remove outdated entries, don't just append
- **Compound effect** — Each update makes the next developer (human or AI) more effective

## Related Skills

- **qx-managing-changes** — Archive phase triggers compound accumulation
- **qx-sprint** — Retro phase suggests compound accumulation
- **qx-debugging** — Debugging insights feed into compound accumulation
- **qx-system-analysis** — System map is a key compound accumulation target
