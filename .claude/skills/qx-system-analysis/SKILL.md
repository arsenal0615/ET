---
name: qx-system-analysis
description: "Use when planning changes to understand which systems are affected. Scans codebase relationships to produce an impact analysis report."
---

# 系统影响分析（System Impact Analysis）

## Overview

Analyze the impact of a proposed change by scanning codebase relationships — data models, logic modules, messages, events, and their connections. Produces a structured impact report that informs planning and reduces surprise breakage.

**Core principle:** Understand the blast radius before you start changing code.

**Announce at start:** "Using qx-system-analysis to analyze the impact of [proposed change]."

## When to Use

- Before implementing a change that touches shared systems
- When `/qx-review` identifies potential cross-system effects
- When planning work on a system you're not fully familiar with
- When a change involves modifying protocol messages, data model structures, or event flows

## Two-Layer Analysis Model

QX uses a two-layer system analysis approach:

### Layer 1: Persistent System Map
**File:** `docs/system-map.md`
**Granularity:** Mid-level (component/system groups)
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
- What kind of change? (add/modify/remove data model, message, event, etc.)
- What's the motivation? (helps identify what we need to protect)

### Step 2: Read the System Map

Read `docs/system-map.md` to understand known system relationships.

If the system map doesn't exist yet, note that and proceed with direct codebase scanning.

### Step 3: Read Project Rules

Read the project's CLAUDE.md and MEMORY.md to understand:
- Framework-specific patterns and conventions
- Key attributes/annotations to search for
- Module/assembly organization
- Code generation steps that may be affected

### Step 4: Scan Codebase Relationships

Perform targeted scans based on what's being changed. Adapt the scan patterns to the project's framework (read CLAUDE.md for specifics):

**If changing a data model/component:**
1. Find ownership declarations — which parent entity owns it?
2. Find all logic modules that reference this data model
3. Find all creation/instantiation call sites
4. Find all read/access call sites
5. Find event handlers that reference this data model

**If changing a message/protocol:**
1. Find the protocol definition
2. Find all handler classes for this message
3. Find all send/call sites
4. Trace the message chain through system layers
5. Find the response type if it's a request/response pair

**If changing an event:**
1. Find the event definition
2. Find all event handlers/listeners
3. Find all event publish/dispatch sites
4. Check which processes/threads this event operates in

**If changing entity/data structure (parent/child relationships):**
1. Find ownership declarations
2. Find creation/instantiation calls
3. Find factory methods that construct this entity
4. Check serialization implications

**If changing a numeric/config attribute:**
1. Find constant/type definitions
2. Find all read/write usages for this attribute
3. Find calculations that produce/consume this value
4. Check display code that presents this value

### Step 5: Classify Impact

For each affected area, classify:

| Impact Level | Criteria |
|-------------|----------|
| **Direct** | Code that directly references the changed element |
| **Indirect** | Code that depends on direct references (2nd degree) |
| **Potential** | Code that shares the same system boundary (may be affected) |

### Step 6: Generate Impact Report

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
[Trace: layer-by-layer showing affected messages]
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

### Step 7: Present and Recommend

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

> **Note:** Read the project's CLAUDE.md for project-specific patterns, attribute names, and conventions. The patterns below are generic examples — adapt to your project's framework.

| Looking for | Generic Approach |
|------------|-----------------|
| Data model owners | Search for ownership/parent declaration attributes |
| Child relationships | Search for child/containment declaration attributes |
| Logic modules | Search for classes that reference the target type |
| Data model usage | Search for creation and access calls on the target type |
| Message handlers | Search for handler classes matching the message name |
| Event handlers | Search for event listener/subscriber registrations |
| Event publishers | Search for event publish/dispatch calls |
| RPC callers | Search for remote call invocations with the request type |
| Factory methods | Search for factory/creation patterns |

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
