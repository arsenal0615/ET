---
name: qx-test-design
description: "Use when creating test scenarios, test plans, or test design documents for game features. Produces prioritized test coverage based on risk assessment and player impact."
---

# Game Test Design

## Overview

Create comprehensive test scenarios for game features, covering gameplay mechanics, progression systems, multiplayer functionality, and platform requirements. Produces a prioritized test plan based on risk assessment and player impact.

**Announce at start:** "Using qx-test-design to create the test design for [feature/sprint]."

## When to Use

- New feature needs test coverage design
- Sprint test planning
- Creating test scenarios for a game system
- Establishing test strategy for a major release

## Preflight

- Game design documentation available (GDD, feature specs)
- Understanding of target platforms
- Knowledge of core gameplay loop

## Process

### Step 1: Gather Context

1. **Read Game Design Documentation**
   - Locate GDD or feature specs in `docs/game-design/`
   - Identify core mechanics and features being tested
   - Note target platforms

2. **Identify Critical Systems**
   - Core gameplay loop
   - Progression / save systems
   - Multiplayer (if applicable)
   - Monetization (if applicable)

3. **Assess Risk Areas**
   - Player-facing features (highest priority)
   - Data persistence (save/load)
   - Platform requirements
   - Performance-critical paths

### Step 2: Define Test Categories

#### Core Gameplay Testing

| Category | Focus | Priority |
|----------|-------|----------|
| Core Loop | Primary mechanic execution | P0 |
| Combat/Interaction | Hit detection, feedback | P0 |
| Movement | Physics, collision, feel | P0 |
| UI/UX | Menu navigation, HUD | P1 |
| Audio | Sound triggers, music | P2 |

#### Progression Testing

| Category | Focus | Priority |
|----------|-------|----------|
| Save/Load | Data persistence | P0 |
| Unlocks | Content gating | P1 |
| Economy | Currency, rewards | P1 |
| Achievements | Trigger conditions | P2 |

#### Multiplayer Testing (if applicable)

| Category | Focus | Priority |
|----------|-------|----------|
| Connectivity | Join/leave handling | P0 |
| Synchronization | State consistency | P0 |
| Latency | Degraded network | P1 |
| Matchmaking | Player grouping | P1 |

#### Platform Testing

| Category | Focus | Priority |
|----------|-------|----------|
| Input | Controller/touch support | P0 |
| Performance | FPS, loading times | P1 |
| Accessibility | Assist features | P1 |

### Step 3: Create Test Scenarios

**Scenario Format:**
```
SCENARIO: [Descriptive Name]
  GIVEN [Initial state/preconditions]
  WHEN [Action taken]
  THEN [Expected outcome]
  PRIORITY: P0/P1/P2/P3
  CATEGORY: [gameplay/progression/multiplayer/platform]
```

**Example — Gameplay:**
```
SCENARIO: Basic Attack Hits Enemy
  GIVEN player is within attack range of enemy
  AND enemy has 100 health
  WHEN player performs basic attack
  THEN enemy receives damage
  AND damage feedback plays (visual + audio)
  AND enemy health decreases
  PRIORITY: P0
  CATEGORY: gameplay
```

**Example — Progression:**
```
SCENARIO: Save Preserves Player Progress
  GIVEN player has 500 gold and 3 items
  WHEN game saves and reloads
  THEN player has 500 gold and same 3 items
  AND player is at same position
  PRIORITY: P0
  CATEGORY: progression
```

**Example — Multiplayer:**
```
SCENARIO: Gameplay Under High Latency
  GIVEN 2 players in session with 200ms latency
  WHEN Player 1 attacks Player 2
  THEN damage is applied correctly
  AND positions remain synchronized
  PRIORITY: P1
  CATEGORY: multiplayer
```

**E2E Journey Format:**
```
E2E SCENARIO: [Player Journey Name]
  GIVEN [Initial game state]
  WHEN [Sequence of player actions]
  THEN [Observable outcomes]
  TIMEOUT: [Expected max duration in seconds]
  PRIORITY: P0/P1
  CATEGORY: e2e
```

### Step 4: Prioritize Coverage

**Risk Priority Matrix:**
```
                    IMPACT
                Low      High
            ┌─────────┬─────────┐
      High  │   P2    │   P0    │
LIKELIHOOD  ├─────────┼─────────┤
      Low   │   P3    │   P1    │
            └─────────┴─────────┘
```

**Coverage Targets by Priority:**

| Priority | Criteria | Unit | Integration | E2E | Manual |
|----------|----------|------|-------------|-----|--------|
| P0 | Ship blockers | 100% | 80% | Core flows | Smoke |
| P1 | Major features | 90% | 70% | Happy paths | Full |
| P2 | Secondary | 80% | 50% | - | Targeted |
| P3 | Edge cases | 60% | - | - | As needed |

### Step 5: Generate Test Design Document

Save to: `docs/test-plans/[feature-name]-test-design.md`

```markdown
# Game Test Design: [Feature/Sprint Name]

## Overview
- Feature description and core mechanics
- Target platforms
- Test scope (in scope / out of scope)

## Risk Assessment
| Area | Risk | Mitigation |
|------|------|-----------|
| [area] | [potential issue] | [test strategy] |

## Test Scenarios

### Core Gameplay Tests
[SCENARIO blocks...]

### Progression Tests
[SCENARIO blocks...]

### Multiplayer Tests (if applicable)
[SCENARIO blocks...]

### Platform Tests
[SCENARIO blocks...]

### E2E Journey Tests
[SCENARIO blocks...]

## Coverage Matrix
| Feature | P0 | P1 | P2 | P3 | Total |
|---------|----|----|----|----|-------|
| [feature] | N | N | N | N | N |

## Automation Strategy
### Recommended for Automation
- [scenario] — Reason

### Manual Testing Required
- [scenario] — Reason (e.g., requires human judgment on "feel")

## Playtesting Recommendations
- Internal: [focus, participants, duration]
- External: [focus, target audience, duration]

## Next Steps
1. [ ] Review test design with team
2. [ ] Implement P0 automated tests
3. [ ] Plan playtesting sessions
```

### Step 6: Report Summary

```
Test Design Complete — [Feature Name]

Scenarios Created: [count]
  P0 (Critical): [count]
  P1 (High): [count]
  P2 (Medium): [count]
  P3 (Low): [count]

Focus Areas: Core Gameplay, Progression, [Multiplayer], Platform

Next: Review with team → Implement P0 tests → Plan playtests
```

## Key Principles

- **Risk-based priority** — Test high-impact, high-probability scenarios first
- **Player-facing focus** — Prioritize what players actually experience
- **Automate the repetitive** — Manual for exploration, automation for regression
- **Don't duplicate analyzers** — Know what your build system already catches

> **Note:** Read the project's CLAUDE.md to understand what static analysis and compile-time checks already exist. Don't write tests for things the compiler catches.

## Related Skills

- **qx-tdd** — For test-driven implementation of the scenarios
- **qx-verification** — For verifying test results before claiming completion
- **qx-game-design** — GDD provides the features to test
- **qx-sprint** — Sprint context determines test scope
