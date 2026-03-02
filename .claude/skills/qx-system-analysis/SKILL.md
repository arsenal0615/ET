---
name: qx-system-analysis
description: "Use when planning changes to understand which systems are affected. Scans codebase relationships (Components, Systems, Messages, Events) to produce an impact analysis report."
---

# 系统影响分析（System Impact Analysis）

## Overview

Analyze the impact of a proposed change by scanning codebase relationships — Components, Systems, Messages, Events, and their connections. Produces a structured impact report that informs planning and reduces surprise breakage.

**Core principle:** Understand the blast radius before you start changing code.

**Announce at start:** "Using qx-system-analysis to analyze the impact of [proposed change]."

## When to Use

- Before implementing a change that touches shared systems
- When `/qx-review` identifies potential cross-system effects
- When planning work on a system you're not fully familiar with
- When a change involves modifying Proto messages, Component structures, or event flows

## Two-Layer Analysis Model

QX uses a two-layer system analysis approach:

### Layer 1: Persistent System Map
**File:** `docs/system-map.md`
**Granularity:** Mid-level (Component/System groups)
**Updated by:** `/qx-compound` after each completed change

The system map provides a quick overview of what exists and how systems connect. It's a living document maintained across the project lifetime.

### Layer 2: On-Demand Impact Analysis (This Skill)
**Granularity:** Fine-grained (specific files, fields, message chains)
**Triggered by:** `/qx-impact` or called internally by other skills
**Output:** One-time analysis report for a specific proposed change

## The Process

### Step 1: Define the Change Scope

Ask the user (or read from change proposal):
- What system or component is being changed?
- What kind of change? (add/modify/remove component, message, event, etc.)
- What's the motivation? (helps identify what we need to protect)

### Step 2: Read the System Map

Read `docs/system-map.md` to understand known system relationships.

If the system map doesn't exist yet, note that and proceed with direct codebase scanning.

### Step 3: Scan Codebase Relationships

Perform targeted scans based on what's being changed:

**If changing a Component:**
1. Find `[ComponentOf]` declaration — which Entity owns it?
2. Find all System classes that use `[FriendOf(typeof(Component))]`
3. Find all `AddComponent<Component>()` call sites
4. Find all `GetComponent<Component>()` call sites
5. Find event handlers that reference this Component

**If changing a Message (Proto):**
1. Find the Proto file definition
2. Find all `Handler` classes for this message
3. Find all `session.Call()` or `session.Send()` call sites
4. Trace the message chain: Client → Gate → Map (or other routing)
5. Find the Response type if it's a Request

**If changing an Event:**
1. Find the event struct definition
2. Find all `[Event(SceneType.X)]` handlers
3. Find all `EventSystem.Instance.Publish()` call sites
4. Check which Fibers/Scenes this event operates in

**If changing Entity structure (parent/child):**
1. Find `[ChildOf]` / `[ComponentOf]` declarations
2. Find `AddChild<T>()` / `AddComponent<T>()` calls
3. Find factory methods that construct this Entity
4. Check serialization implications (MongoDB, Proto)

**If changing a Numeric attribute:**
1. Find NumericType constant definitions
2. Find all `GetAsInt/GetAsFloat` usages for this numeric key
3. Find calculations that produce/consume this value
4. Check client display code that reads this value

### Step 4: Classify Impact

For each affected area, classify:

| Impact Level | Criteria |
|-------------|----------|
| **Direct** | Code that directly references the changed element |
| **Indirect** | Code that depends on direct references (2nd degree) |
| **Potential** | Code that shares the same system boundary (may be affected) |

### Step 5: Generate Impact Report

```markdown
# Impact Analysis: [Change Description]

## Change Summary
- **Target:** [Component/Message/Event being changed]
- **Type:** [Add/Modify/Remove]
- **Motivation:** [Why this change]

## Direct Impact
| File | Element | Impact |
|------|---------|--------|
| [path] | [class/method] | [what changes] |

## Indirect Impact
| File | Element | Impact |
|------|---------|--------|
| [path] | [class/method] | [potential effect] |

## Message Chain Impact
```
[Trace: Client → Gate → Map showing affected messages]
```

## Cross-System Dependencies
- [System A] depends on [changed element] via [mechanism]
- [System B] reads [changed data] for [purpose]

## Risk Assessment
| Risk | Level | Mitigation |
|------|-------|-----------|
| [risk description] | High/Medium/Low | [how to mitigate] |

## Recommended Test Coverage
- [ ] [Test scenario 1]
- [ ] [Test scenario 2]

## Files to Modify
1. [file path] — [what to change]
2. [file path] — [what to change]
```

### Step 6: Present and Recommend

Present the analysis summary:

```
Impact Analysis Complete — [Change Description]

Direct impact: [N] files
Indirect impact: [N] files
Risk level: [High/Medium/Low]

Key risks:
1. [Top risk]
2. [Second risk]

Recommendation: [Proceed / Proceed with caution / Reconsider approach]
```

Offer next steps:
- "Proceed to implementation planning? (`/qx-plan`)"
- "Want me to scan deeper on [specific area]?"
- "Should I update the system map with these findings?"

## Scan Patterns Quick Reference

> **Note:** Read the project's CLAUDE.md and documentation for project-specific patterns, attribute names, and conventions. The patterns below are generic examples.

| Looking for | Search Pattern |
|------------|----------------|
| Component owners | `[ComponentOf(typeof(` |
| Child relationships | `[ChildOf(typeof(` |
| System classes | `[FriendOf(typeof(Target))]` |
| Component usage | `AddComponent<Target>` / `GetComponent<Target>` |
| Message handlers | `class.*Handler.*:.*MessageHandler` + message name |
| Event handlers | `[Event(SceneType.` + event class name |
| Event publishers | `Publish.*EventName` |
| RPC callers | `session.Call.*RequestType` |
| Factory methods | `Factory.*Create` |

## Key Principles

- **Scan before you plan** — Impact analysis informs the implementation plan
- **Breadth first, then depth** — Start with direct references, then trace outward
- **Classify, don't just list** — Direct vs Indirect vs Potential matters
- **Risk drives testing** — Higher impact areas need more test coverage
- **Living knowledge** — Suggest system map updates for new discoveries

## Related Skills

- **qx-exploring** — For open-ended codebase exploration
- **qx-writing-plans** — Uses impact analysis to inform implementation plans
- **qx-compound** — Updates the system map after changes are complete
- **qx-three-party-review** — May trigger impact analysis during review
