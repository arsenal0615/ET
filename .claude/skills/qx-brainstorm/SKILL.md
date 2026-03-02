---
name: qx-brainstorm
description: "Use before any creative work — game features, system design, new mechanics. Explores ideas through collaborative dialogue, using game design techniques, before committing to implementation."
---

# Game Brainstorming

## Overview

Help turn game ideas into fully formed designs through collaborative dialogue. Understand the vision first, explore approaches using game design techniques, then produce a design document for handoff.

**Core principle:** Design before implementation. Every feature goes through brainstorming, no matter how "simple" it seems.

**Announce at start:** "Using qx-brainstorm to explore this idea before implementation."

<HARD-GATE>
Do NOT invoke any implementation skill, write any code, or take any implementation action until you have presented a design and the user has approved it. This applies to EVERY feature regardless of perceived simplicity.
</HARD-GATE>

## Anti-Pattern: "This Is Too Simple"

Every feature goes through this process. A stat modifier, a single UI panel, a config change — all of them. "Simple" features are where unexamined assumptions cause the most wasted work. The design can be short (a few sentences), but you MUST present it and get approval.

## Checklist

You MUST create a task for each of these items and complete them in order:

1. **Explore project context** — check existing game docs, design docs, recent changes
2. **Ask clarifying questions** — one at a time, understand vision/constraints/player experience
3. **Apply game design techniques** — use appropriate ideation methods (see below)
4. **Propose 2-3 approaches** — with trade-offs and your recommendation
5. **Present design** — in sections scaled to complexity, get user approval after each section
6. **Write design doc** — save to `docs/game-design/` and commit
7. **Transition** — invoke `qx-writing-plans` to create implementation plan, or hand off to `/qx-review` for three-party review

## Process Flow

```
Explore context → Ask questions → Apply techniques → Propose approaches
→ Present design → User approves? → Write doc → Transition
                          ↑ no, revise ↓
```

## The Process

### Understanding the Idea

- Check existing game design documents first (`docs/game-design/`)
- Read CLAUDE.md for project architecture context
- Ask questions ONE AT A TIME to refine the idea
- Prefer multiple choice questions when possible
- Focus on: player experience, constraints, success criteria, system interactions

### Applying Game Design Techniques

Choose techniques based on what you're brainstorming:

**For Core Gameplay:**
| Technique | When to Use | Key Questions |
|-----------|-------------|---------------|
| MDA Framework | Holistic design | Mechanics → Dynamics → Aesthetics: do they align? |
| Core Loop Brainstorming | Gameplay foundation | What does player DO? What's the reward? Why repeat? |
| Player Fantasy Mining | Motivation | What fantasy does the player live? What makes them feel awesome? |
| Verbs Before Nouns | Mechanics-first | What verbs define the game? Build mechanics from actions |

**For Innovation:**
| Technique | When to Use | Key Questions |
|-----------|-------------|---------------|
| Genre Mashup | Fresh gameplay | Take two genres — what unique gameplay emerges? |
| Constraint-Based | Elegant design | Pick a limitation — build everything around it |
| Failure State Design | Tension | How can players fail interestingly? Is failure fair and instructive? |

**For Systems:**
| Technique | When to Use | Key Questions |
|-----------|-------------|---------------|
| Emergence Engineering | Depth/complexity | What simple rules combine to create complex outcomes? |
| Economy Balancing | Resource design | Sources vs sinks — where are the exploits? |
| Progression Curve | Pacing | How does difficulty evolve? When do we introduce concepts? |

**For Narrative:**
| Technique | When to Use | Key Questions |
|-----------|-------------|---------------|
| Ludonarrative Harmony | Story + gameplay | Do mechanics reinforce narrative themes? |
| Environmental Storytelling | World design | What does the space communicate without exposition? |
| Player Agency Moments | Choice design | Where do choices matter? Branch vs flavor? |

### Exploring Approaches

- Propose 2-3 different approaches with trade-offs
- Lead with your recommended option and explain WHY
- Consider: player experience, technical feasibility, scope, system interactions

### Presenting the Design

- Scale each section to its complexity (few sentences if simple, up to 300 words if nuanced)
- Ask after each section whether it looks right so far
- Cover: gameplay mechanics, system interactions, data flow, edge cases, testing considerations
- Be ready to go back and clarify

## Design Document Structure

```markdown
# [Feature Name] Design

## Vision
[One paragraph — what is this and why does it matter to players?]

## Player Experience
[What does it feel like to interact with this feature?]

## Core Mechanics
[How does it work? Rules, interactions, feedback loops]

## System Interactions
[How does this connect to existing game systems?]

## Edge Cases & Constraints
[What could go wrong? Boundaries and limitations]

## Technical Considerations
[Flag items for programmer attention — architecture, networking, data]

## Success Criteria
[How do we know this feature is working as intended?]

## Open Questions
[What still needs to be resolved?]
```

## After the Design

**Save:** Write to `docs/game-design/YYYY-MM-DD-<topic>-design.md`

**Transition options:**
1. **Implementation path:** Invoke `qx-writing-plans` to create a detailed implementation plan
2. **Review path:** If the feature is significant, invoke `/qx-review` for three-party review first
3. **GDD path:** If this feeds into a larger GDD, invoke `qx-game-design` to integrate

## Key Principles

- **One question at a time** — Don't overwhelm with multiple questions
- **Multiple choice preferred** — Easier to answer than open-ended
- **YAGNI ruthlessly** — Remove unnecessary features from all designs
- **Player experience first** — Technical elegance that hurts fun is a failure
- **Systems thinking** — Every new feature affects existing systems
- **Incremental validation** — Present design, get approval before moving on

## Related Skills

- **qx-game-design** — For creating/editing full Game Design Documents
- **qx-writing-plans** — For creating implementation plans from designs
- **qx-exploring** — For open-ended investigation before brainstorming
