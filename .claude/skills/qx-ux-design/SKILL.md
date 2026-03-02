---
name: qx-ux-design
description: "Use when planning UX patterns, UI layouts, interaction flows, or visual direction for a game. Facilitates collaborative UX design through structured discovery."
---

# UX Design

## Overview

Create UX design specifications through collaborative visual exploration and informed decision-making. You act as a UX facilitator working with a product stakeholder — this is a partnership, not a client-vendor relationship.

**Core principle:** Collaborative discovery, not assumption-based design. Never generate content without user input.

**Announce at start:** "Using qx-ux-design to design the UX specification."

## When to Use

- Planning UI/UX for a new game feature
- Redesigning existing UI flows
- Creating a visual direction / style guide
- Planning screen layouts and navigation

## Preflight

Check for existing context:
1. `docs/game-design/` — GDD, feature specs
2. Existing UX specs in `docs/game-design/`
3. Project CLAUDE.md for technical constraints
4. Knowledge base `art-ux-standards.md` for general UX principles

## UX Design Process

### Phase 1: Project Understanding

**Goal:** Understand what we're designing UX for.

**Review existing docs, then ask (one at a time):**
- What feature/screen are we designing UX for?
- Who are the target players? How tech-savvy?
- What platforms? (PC, mobile, console — affects input and layout)
- What's the most important thing players need to DO on this screen?

**Output:**
```markdown
## Project Understanding
### Feature Scope
### Target Players
### Platform Requirements
### Core User Goals
### Key UX Challenges
### Design Opportunities
```

**Present to user for approval before continuing.**

### Phase 2: Core Experience Definition

**Goal:** Define how the interaction should FEEL.

**Questions:**
- What adjectives describe the ideal interaction? (Fast, careful, immersive, casual...)
- What's the primary flow? (What does the player do step by step?)
- What existing games/apps have UX you admire for this type of feature?
- What frustrations should we avoid?

**Output:**
```markdown
## Core Experience
### Interaction Principles
### Primary User Flow
### Reference Inspirations
### Anti-patterns to Avoid
```

### Phase 3: Information Architecture

**Goal:** Organize what information is shown and how.

**Collaborate on:**
- What information does the player need on this screen?
- Priority: What's critical vs nice-to-have?
- Grouping: What information belongs together?
- Navigation: How does the player get here and leave?

**Output:**
```markdown
## Information Architecture
### Information Hierarchy
### Content Grouping
### Navigation Flow
### Screen Inventory
```

### Phase 4: Layout & Wireframes

**Goal:** Define spatial organization.

**Considerations:**
- Safe areas (mobile notch, TV overscan)
- Touch targets (44dp minimum on mobile)
- Aspect ratio adaptations
- Z-ordering (HUD > Popups > Windows > World UI)

**Output:**
```markdown
## Layout
### Screen Layouts (ASCII wireframes or descriptions)
### Responsive Behavior
### Component Placement Rationale
```

**ASCII wireframe example:**
```
┌──────────────────────────┐
│ [Back]    Title    [Menu]│  ← Header
├──────────────────────────┤
│                          │
│    Main Content Area     │  ← Scrollable
│                          │
├──────────────────────────┤
│ [Action1]    [Action2]   │  ← Bottom bar
└──────────────────────────┘
```

### Phase 5: Visual Direction

**Goal:** Define look and feel.

**Questions:**
- What's the overall mood? (Dark/light, playful/serious, minimal/ornate)
- Color palette direction?
- Typography preferences?
- Animation/motion style?

**Output:**
```markdown
## Visual Direction
### Mood & Tone
### Color Palette
### Typography
### Animation & Motion Principles
### Icon Style
```

### Phase 6: Interaction Details

**Goal:** Define micro-interactions and feedback.

**For each interactive element, define:**
- States: normal, hover, pressed, disabled, loading, error
- Feedback: visual + audio + haptic
- Transitions: how it enters/exits
- Edge cases: empty state, error state, overflow

**Output:**
```markdown
## Interaction Details
### Component States
### Feedback Patterns
### Transition Animations
### Edge Case Handling
```

## Document Output

Save to: `docs/game-design/ux-[feature-name].md`

## After the Design

**Transition options:**
1. **Implementation:** Invoke `qx-writing-plans` for UI implementation plan
2. **Review:** Use `/qx-review` for design review with multiple perspectives
3. **Prototype:** If using a design tool, create mockups based on this spec

## Key Principles

- **One question at a time** — Don't overwhelm the user
- **Facilitator, not generator** — Ask, then propose, never assume
- **Player-centered** — Every decision serves the player experience
- **Platform-aware** — Mobile vs PC vs console changes everything
- **Present before proceeding** — Show each phase, get approval, then continue
- **Accessibility always** — Consider colorblind modes, text sizes, screen readers

## Related Skills

- **qx-brainstorm** — For initial feature ideation before UX design
- **qx-game-design** — GDD provides context for UX decisions
- **qx-writing-plans** — For UI implementation planning
