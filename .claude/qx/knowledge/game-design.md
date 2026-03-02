# Game Design Knowledge Base

> Injected into game-designer agent context. Contains game design theory and best practices applicable to any Unity game project.

## Core Design Principles

### Player Experience First
- Every system, mechanic, and UI element serves the player experience
- Technical elegance that hurts player experience is a failure
- Measure success by player behavior, not system complexity

### Iterative Design
- Paper prototype -> Digital prototype -> Playtest -> Iterate
- Kill your darlings: if playtesting says it's not fun, cut it
- "Fun" is discovered, not designed — create conditions for emergent fun

### Systems Thinking
- Games are interconnected systems, not isolated features
- Every new system affects existing systems (economy, difficulty, progression)
- Map dependencies before implementing: input → process → output → feedback

## Game Design Document (GDD) Structure

### Essential Sections
1. **Vision Statement** — One paragraph that captures the game's core identity
2. **Core Loop** — The primary activity cycle players repeat
3. **Pillars** — 3-5 design pillars that guide all decisions
4. **Systems Overview** — Each game system with inputs, outputs, and interactions
5. **Progression** — How the player grows (power, knowledge, access)
6. **Economy** — Resource flow: sources, sinks, exchange rates
7. **Content Matrix** — What content exists at each progression stage

### Technical Considerations for GDD
When writing a GDD, always consider the technical implications that need programmer input:
- **Authority Model** — Which systems need server validation vs client-only?
- **Synchronization** — Which sync model fits each system? (state sync, lockstep, etc.)
- **Concurrency** — Which systems need dedicated threads/processes?
- **Hot-Update Scope** — Which systems need frequent iteration without full rebuilds?

> **Note:** Read the project's CLAUDE.md and framework documentation for specific technical patterns. The GDD should flag technical concerns, not prescribe framework-specific solutions.

## System Architecture Patterns

### Component-Based Design
- Each game system maps to data containers + behavior processors
- Data and behavior are separate — enables hot-reload and parallel development
- Prefer composition over inheritance for game entities

### Event-Driven Design
- Systems communicate via events, not direct coupling
- Reduces dependencies, enables parallel development
- Decoupled systems are easier to test, iterate, and remove

### Config-Driven Design
- Balance values, content definitions, and progression curves in data files
- Designers modify data, not code
- Enables rapid iteration without programmer involvement

## Narrative Design

### Story Structure
- **Three-Act Structure**: Setup → Confrontation → Resolution
- **Hero's Journey**: Applicable to player progression arc
- **Environmental Storytelling**: Let the world tell stories through placement and design

### Dialogue Systems
- Branching dialogue: choices → consequences → player agency
- Bark system: ambient NPC reactions to player state
- Quest text: brief, actionable, personality-appropriate

## Balance & Economy

### Resource Economy
- **Sources**: Where resources enter the system (quests, drops, purchases)
- **Sinks**: Where resources leave (crafting, upgrades, durability loss)
- **Equilibrium**: Sources and sinks balance at target play rate
- **Inflation control**: Time-gating, diminishing returns, caps

### Difficulty Curves
- Introduce one mechanic at a time
- Challenge should match growing player skill
- Provide difficulty options OR adaptive difficulty
- "Easy to learn, hard to master" — separate skill floor from skill ceiling

### Stat System Design
- Define the stat formula BEFORE implementing: base value + modifiers = final value
- Common modifier types: flat add, percentage, final flat add, final percentage
- Document the calculation order clearly for programmer handoff
- Use spreadsheets to simulate balance across progression stages

## Sprint Planning for Game Features

### Story Sizing
- **1 point**: Single data container + logic, no network messages, no config
- **2 points**: Data + logic + message handler, may need new protocol
- **3 points**: Multiple interconnected pieces, protocol + config, cross-system
- **5 points**: New subsystem, significant protocol + config changes
- **8+ points**: Epic — must be broken down further

### Acceptance Criteria Template
```
GIVEN [initial state/context]
WHEN [player action / system event]
THEN [observable outcome]
AND [secondary effects if any]
```

### Technical Acceptance Criteria
Always include these for programmer clarity:
- Which server process/scene handles the feature
- Server authority requirements (what must be validated server-side)
- Network sync requirements (what data is sent, how often)
- Data lifecycle expectations (when created, when cleaned up)

> **Note:** Use the project's specific terminology (scene types, message types, etc.) from project documentation.

## Playtest Methodology

### Internal Playtesting
1. Define hypothesis: "Players will discover X mechanic within Y minutes"
2. Observe without interfering
3. Record: what confused them, what delighted them, what they skipped
4. Prioritize findings by frequency and severity

### Metrics to Track
- Time-to-first-action (onboarding quality)
- Session length and return rate (engagement)
- Feature discovery rate (discoverability)
- Failure/success ratio (difficulty calibration)
- Resource accumulation rate (economy balance)

## Common Design Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Feature creep | Scope grows every sprint | Enforce design pillars as filter |
| Kitchen sink design | Too many unrelated systems | Core loop test: does it serve the loop? |
| Designer's blindness | "It's obvious" (but only to you) | Playtest with fresh players |
| Balance by feel | "Seems right" | Use spreadsheets, simulate 1000 players |
| Copy without understanding | "Game X does it" | Understand WHY it works in Game X's context |
| Ignoring server cost | "Cool system!" (but 100ms per frame) | Always estimate server tick budget |
