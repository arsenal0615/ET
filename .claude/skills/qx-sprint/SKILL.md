---
name: qx-sprint
description: "Use for sprint planning, sprint status tracking, and sprint retrospectives. Manages the development cadence from epic/story breakdown through execution tracking."
---

# Sprint Management

## Overview

Manage development sprints: plan work, track status, and run retrospectives. Works with epics and stories from game design to organize implementation work.

**Announce at start:** "Using qx-sprint to [plan/track/retrospect] the sprint."

## Modes

This skill operates in three modes based on context:

| Mode | Trigger | Output |
|------|---------|--------|
| **Plan** | "plan sprint", "sprint planning", new sprint | `docs/sprints/sprint-N-plan.md` |
| **Status** | "sprint status", "where are we" | Updated status in sprint plan |
| **Retro** | "retrospective", "sprint retro", sprint end | `docs/sprints/sprint-N-retro.md` |

## Sprint Planning Mode

### Prerequisites
- Game design docs exist (`docs/game-design/`)
- Stories have been broken down (via `/qx-stories` or manually)
- Team capacity is understood (how many story points per sprint?)

### Process

**Step 1: Gather Context**
1. Read existing game design docs and story backlog
2. Check previous sprint retro (if not first sprint) for carryover items
3. Identify current project priorities

**Step 2: Select Stories**

Collaborate with user to select stories for this sprint:
- Present backlog ordered by priority
- For each story, show: ID, title, estimate, dependencies
- Respect capacity limits
- Flag dependency chains (Story B requires Story A complete)

**Step 3: Generate Sprint Plan**

```markdown
# Sprint N Plan

**Duration:** [start date] → [end date]
**Goal:** [One sentence sprint goal]
**Capacity:** [X story points]

## Stories

| # | Story | Points | Owner | Dependencies | Status |
|---|-------|--------|-------|-------------|--------|
| 1.1 | [Title] | 3 | - | None | backlog |
| 1.2 | [Title] | 2 | - | 1.1 | backlog |
| 2.1 | [Title] | 5 | - | None | backlog |

## Sprint Backlog (not selected for this sprint)
[Remaining stories ordered by priority]

## Risks & Notes
- [Any risks or blockers to flag]
```

Save to: `docs/sprints/sprint-N-plan.md`

## Sprint Status Mode

### Status State Machine

**Story Status Flow:**
```
backlog → ready → in-progress → review → done
```

| Status | Meaning |
|--------|---------|
| `backlog` | Story exists but work not started |
| `ready` | Story refined, ready for development |
| `in-progress` | Developer actively working |
| `review` | Code complete, in review |
| `done` | Reviewed, tested, merged |

### Status Update Process

1. Read current sprint plan file
2. Check git history and active changes (`docs/changes/`) for progress signals
3. Present current status to user
4. Ask for corrections/updates
5. Update sprint plan file with new statuses

### Status Report Format

```
Sprint N Status Update — [date]

Progress: [X/Y] stories done ([Z] points / [total] points)

  [done]        1.1 Story Title (3 pts)
  [in-progress] 1.2 Story Title (2 pts) — active change: feature-name
  [backlog]     2.1 Story Title (5 pts)

Blockers:
  - [Any blockers identified]

Velocity: [X] points completed this sprint so far
```

## Sprint Retrospective Mode

### Process

**Step 1: Gather Data**
- What was planned vs completed
- Sprint velocity (points completed)
- Carryover stories (not completed)
- Notable events during the sprint

**Step 2: Facilitate Discussion**

Ask these questions one at a time:
1. **What went well?** — What should we keep doing?
2. **What didn't go well?** — What caused friction or problems?
3. **What did we learn?** — New insights about the game, code, or process?
4. **What should we change?** — Concrete action items for next sprint

**Step 3: Generate Retro Document**

```markdown
# Sprint N Retrospective

**Date:** [date]
**Sprint Goal:** [was it met?]
**Velocity:** [X points planned, Y completed]

## Completed
- [stories completed]

## Carried Over
- [stories not completed, with reason]

## What Went Well
- [items]

## What Didn't Go Well
- [items]

## Learnings
- [items]

## Action Items for Next Sprint
- [ ] [concrete action]
- [ ] [concrete action]
```

Save to: `docs/sprints/sprint-N-retro.md`

### Compound Accumulation

After retro, suggest running `/qx-compound` to extract knowledge from this sprint into the knowledge base.

## Integration with QX Workflow

```
GDD → /qx-stories → Stories → /qx-sprint plan → Sprint Plan
→ /qx-change create (per story) → Development → /qx-sprint status
→ Sprint complete → /qx-sprint retro → /qx-compound
→ Next sprint → /qx-sprint plan
```

## Key Principles

- **One sprint goal** — Each sprint has a single, clear goal
- **Respect capacity** — Don't overcommit; leave buffer for bugs
- **Dependencies first** — Plan dependency chains carefully
- **Carry over honestly** — Don't hide incomplete work
- **Retro drives improvement** — Action items must be concrete and actionable

## Related Skills

- **qx-brainstorm** — For ideating features before sprint planning
- **qx-game-design** — GDD provides the story backlog source
- **qx-managing-changes** — Each story becomes a change during execution
- **qx-writing-plans** — For creating implementation plans per story
