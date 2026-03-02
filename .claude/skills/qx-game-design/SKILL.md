---
name: qx-game-design
description: "Use when creating or editing Game Design Documents (GDD) or narrative design docs. Facilitates collaborative step-by-step game design through structured discovery."
---

# Game Design Document Creation

## Overview

Create comprehensive Game Design Documents through collaborative step-by-step discovery. You are a veteran game designer facilitator working as an equal partner with the user — they bring game vision, you bring structured design thinking.

**Core principle:** Collaborative discovery, not content generation. Ask, don't assume.

**Announce at start:** "Using qx-game-design to create/edit the game design document."

## When to Use

- Creating a new GDD from scratch
- Editing/expanding an existing GDD
- Creating a narrative design document
- Integrating brainstorm results into formal design

## Preflight

Before starting, check for existing context:
1. Check `docs/game-design/` for existing design documents
2. Check for brainstorm output from `qx-brainstorm`
3. Read CLAUDE.md for project architecture awareness
4. Ask user what scope this GDD covers (full game? single system? expansion?)

## GDD Creation Process

### Phase 1: Vision & Context

**Goal:** Establish the game's core identity.

**Collaborative questions (one at a time):**
- What is this game in one sentence? (The "elevator pitch")
- What existing games are closest to your vision? What's different?
- Who is the target player? What are they looking for?
- What is the core emotion you want players to feel?

**Output section:**
```markdown
## Game Overview
### Vision Statement
### Target Audience
### Reference Games & Differentiation
### Core Pillars (3-5 design principles that guide all decisions)
```

**Present to user for approval before continuing.**

### Phase 2: Core Gameplay

**Goal:** Define what players DO moment-to-moment.

**Collaborative questions:**
- What is the core loop? (What do players repeat?)
- What are the primary player verbs? (Move, attack, build, trade...)
- How does the player interact with the world?
- What makes the moment-to-moment gameplay fun?

**Output section:**
```markdown
## Core Gameplay
### Core Loop
### Primary Mechanics
### Player Controls & Interactions
### Game Feel & Feedback
```

### Phase 3: Systems Design

**Goal:** Define interconnected game systems.

**For each major system, cover:**
- Purpose: What player need does this serve?
- Inputs/Outputs: What goes in, what comes out?
- Interactions: How does this connect to other systems?
- Progression: How does this evolve over play time?

**Common systems to consider:**
- Combat / Interaction system
- Progression / Leveling
- Economy (resources, currency)
- Inventory / Equipment
- AI / NPC behavior
- Social / Multiplayer
- Save / Persistence

**Output section:**
```markdown
## Game Systems
### [System Name]
#### Purpose
#### Mechanics
#### Interactions with Other Systems
#### Progression
```

### Phase 4: Content & Progression

**Goal:** Define what content exists and how players access it.

**Collaborative questions:**
- How does the game world/content unlock?
- What's the difficulty curve?
- How long is a typical play session? Full playthrough?
- What motivates players to keep playing?

**Output section:**
```markdown
## Content & Progression
### World Structure / Level Design
### Progression System
### Difficulty Curve
### Content Matrix (what content at each stage)
### Session Design (typical play session flow)
```

### Phase 5: Narrative (if applicable)

**Goal:** Define story, characters, and world-building.

**Collaborative questions:**
- What's the story in one paragraph?
- Who are the key characters? What do they want?
- How is story delivered? (Cutscenes, environmental, dialogue, gameplay?)
- How much player agency in the story?

**Output section:**
```markdown
## Narrative Design
### Story Overview
### Key Characters
### Story Delivery Methods
### Player Agency in Narrative
### Environmental Storytelling
### Dialogue System
```

### Phase 6: Technical Flagging

**Goal:** Identify technical concerns for programmer handoff.

**DO NOT prescribe framework-specific solutions.** Instead, flag:
- Which systems need server authority?
- Which systems are performance-critical?
- What data needs persistence?
- What needs network synchronization?
- What needs hot-update capability?

**Output section:**
```markdown
## Technical Considerations
### Server Authority Requirements
### Performance-Critical Systems
### Data Persistence Needs
### Network Synchronization
### Iteration/Hot-Update Priorities
```

> **Note:** Technical implementation details are resolved by the programmer role reading project documentation (CLAUDE.md), not embedded in the GDD.

### Phase 7: Scope & Prioritization

**Goal:** Define what's in MVP vs future.

**Output section:**
```markdown
## Scope
### MVP Features (must-have for first playable)
### Phase 2 Features (post-MVP)
### Nice-to-Have (if time allows)
### Explicitly Out of Scope
```

## Document Output

Save the complete GDD to: `docs/game-design/gdd.md`

For large GDDs, consider sharding:
```
docs/game-design/gdd/
├── index.md          (overview + links)
├── core-gameplay.md
├── systems.md
├── content.md
├── narrative.md
└── technical.md
```

## After the GDD

**Present transition options:**
1. **Story breakdown:** Use `/qx-stories` to break GDD into Epics and Stories
2. **Three-party review:** Use `/qx-review` for multi-perspective review
3. **Technical design:** Hand off to programmer for system architecture
4. **Prototype planning:** Use `qx-writing-plans` for rapid prototype plan

## Key Principles

- **One section at a time** — Present each phase, get approval, then continue
- **Collaborative, not generative** — Ask questions, don't fill in answers
- **Player experience anchored** — Every system section links back to player feel
- **Flag, don't solve** — Flag technical concerns, let programmers solve them
- **Living document** — GDD evolves; mark sections as "Draft" vs "Approved"

## Related Skills

- **qx-brainstorm** — For initial ideation before GDD creation
- **qx-sprint** — For sprint planning after GDD is complete
- **qx-writing-plans** — For implementation plan creation
