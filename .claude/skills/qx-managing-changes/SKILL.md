---
name: qx-managing-changes
description: "Use when creating a new feature or change, continuing work on a change, defining requirements, writing proposals, designing solutions, checking change status, verifying implementation completeness, archiving completed work, or listing active changes. Triggers on: create change, continue, new feature, proposal, requirements, specs, design, verify, archive, status, list changes, /qx-change"
---

# Managing Changes

Manage the full lifecycle of a "change" — from proposal to archive. A change is a directory-based container at `docs/changes/<name>/` that tracks all artifacts for a feature, fix, or modification.

**Announce at start:** "I'm using the qx-managing-changes skill to [create/propose/design/verify/archive] this change."

## When to Use This vs Quick Mode

| Situation | Use This (Change Mode) | Use Quick Mode (qx-brainstorm) |
|-----------|----------------------|-------------------------------|
| New game system or feature | ✓ | |
| Cross-role handoff (designer → engineer) | ✓ | |
| Requirements need to be tracked long-term | ✓ | |
| Technical investigation with formal design | ✓ | |
| Small bug fix | | ✓ |
| Simple refactoring | | ✓ |
| Config change or tweak | | ✓ |

## Change Directory Structure

```
docs/changes/<name>/
├── .change.yaml       # Metadata (created date, status)
├── proposal.md        # Why and what (required)
├── design.md          # Technical approach (optional)
├── specs/             # Delta specs (optional)
│   └── <capability>/
│       └── spec.md    # ADDED/MODIFIED/REMOVED requirements
└── plan.md            # Implementation tasks with checkboxes
```

## Operations

Infer the operation from conversation context. If ambiguous, ask the user.

---

### CREATE — Start a New Change

**Triggers:** "start a new feature", "create change", "new change", "/qx-change create"

**Process:**
1. Get a name from the user (or derive from description)
2. Convert to kebab-case (e.g., "combo attack system" → `combo-attack-system`)
3. Check `docs/changes/` for duplicates
4. Create directory and metadata:

```
docs/changes/<name>/
└── .change.yaml
```

`.change.yaml` content:
```yaml
created: YYYY-MM-DD
status: active
```

5. Announce what was created and suggest next step: "Change created. Want to write the proposal?"

---

### CONTINUE — Automatically Advance to Next Step

**Triggers:** "continue", "next step", "keep going", "/qx-change continue"

**Process:**
1. If no change specified, infer from context or ask
2. Scan `docs/changes/<name>/` to determine what exists:

```
Check in order:
  proposal.md exists?  → NO  → create proposal (run PROPOSE)
  proposal.md exists?  → YES → check capabilities
    Capabilities listed but no specs/?  → create specs (run SPEC)
  design.md exists?    → NO  → create design (run DESIGN)
  plan.md exists?      → NO  → create plan (invoke qx-writing-plans skill)
  plan.md has - [ ]?   → YES → resume execution (invoke qx-exec skill)
  all tasks [x]?       → YES → suggest verify/archive
```

3. Announce what was detected and what will be created next:
   ```
   Change: add-combo-system
   Completed: proposal ✓, specs ✓
   Next: Creating design.md
   ```

4. Execute the appropriate operation or invoke the appropriate skill

**Notes:**
- This is the "just keep saying continue" workflow — user doesn't need to know which operation comes next
- If capabilities are listed in proposal but specs are optional (pure technical change), skip to design
- When reaching plan creation, hand off to `qx-writing-plans` skill
- When reaching execution, hand off to `qx-exec` skill

---

### PROPOSE — Define What and Why

**Triggers:** "write proposal", "define the change", "/qx-change propose"

**Requires:** Change must exist (run CREATE first if not)

**Process:**
1. If no change is active, ask which change or create one
2. Read CLAUDE.md for project-specific architecture and conventions
3. Draft `proposal.md` using this template:

```markdown
## Why

<!-- What problem does this solve? Why now? -->

## What Changes

<!-- Bullet list of changes. Be specific. -->

## Capabilities

### New Capabilities
<!-- Each becomes a spec file. Use kebab-case names. -->
- `<name>`: <brief description>

### Modified Capabilities
<!-- Existing capabilities whose requirements change. -->
<!-- Check docs/specs/ for existing spec names. -->

## Impact

<!-- Affected code, APIs, dependencies, systems -->

### Framework Impact Checklist
<!-- Read CLAUDE.md for project-specific items. Common checks: -->
- [ ] **Modules/assemblies affected**: Which ones?
- [ ] **New protocol/message definitions needed?** If yes, list them
- [ ] **New config/data files needed?** If yes, list them
- [ ] **Cross-process/cross-thread communication?** If yes, identify boundaries
- [ ] **New data model types?** List with their ownership declarations
- [ ] **Affects system-map.md?** If yes, which systems are impacted
```

**Notes:**
- Capabilities section is optional for pure technical changes
- Each capability listed will need a corresponding spec file
- Keep it concise (1-2 pages). Focus on "why" not "how".

---

### SPEC — Define Requirements

**Triggers:** "write spec", "define requirements", "/qx-change spec"

**Requires:** `proposal.md` must exist

**Process:**
1. Read `proposal.md` to identify capabilities
2. For each capability, create `docs/changes/<name>/specs/<capability>/spec.md`
3. If modifying an existing capability, read `docs/specs/<capability>/spec.md` first

**Delta spec template:**
```markdown
## ADDED Requirements

### Requirement: <name>
<description using SHALL/MUST>

#### Scenario: <scenario name>
- **WHEN** <condition>
- **THEN** <expected outcome>

## MODIFIED Requirements

### Requirement: <existing requirement name>
<!-- Copy full requirement text from main spec, then modify -->

#### Scenario: <new or changed scenario>
- **WHEN** <condition>
- **THEN** <new expected outcome>

## REMOVED Requirements

### Requirement: <name>
**Reason**: <why it's being removed>
**Migration**: <what replaces it>

## RENAMED Requirements
- FROM: `### Requirement: Old Name`
- TO: `### Requirement: New Name`
```

**Rules:**
- Every requirement MUST have at least one scenario
- Scenarios MUST use `#### Scenario:` (4 hashtags)
- Use SHALL/MUST for normative requirements
- MODIFIED requirements must include the full updated content, not just the diff
- Skip this operation entirely for pure technical changes

---

### DESIGN — Define How

**Triggers:** "design the solution", "technical approach", "/qx-change design"

**Requires:** `proposal.md` must exist. Specs recommended but not required.

**Process:**
1. Read `proposal.md` and any specs for context
2. Read `docs/system-map.md` for existing system relationships
3. Read CLAUDE.md for project-specific architecture patterns
4. Draft `design.md` using this template:

```markdown
## Context

<!-- Background and current state -->

## Goals / Non-Goals

**Goals:**
<!-- What this design aims to achieve -->

**Non-Goals:**
<!-- What is explicitly out of scope -->

## Decisions

### Decision 1: <title>

**Choice:** <what was decided>

**Alternatives considered:**
- A) <option> → <why not>
- B) <option> → <why not>

**Rationale:** <why this choice>

## Framework-Specific Decisions

<!-- Read CLAUDE.md for project-specific architectural patterns. -->
<!-- Document decisions about: -->
<!-- - Data model design (ownership, lifecycle, relationships) -->
<!-- - Message/protocol type selection -->
<!-- - Concurrency/threading model choices -->
<!-- - Module/assembly placement -->

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| <risk> | <mitigation> |
```

**Notes:**
- Optional for simple changes — skip if proposal is sufficient
- Focus on architecture and "why X over Y", not line-by-line code
- Good design docs explain the reasoning behind technical decisions
- Framework-Specific Decisions section is optional — include only the subsections that apply

---

### STATUS — Check Progress

**Triggers:** "what's the status", "progress", "/qx-change status"

**Process:**
1. If no change specified, ask or infer from context
2. Read the change directory and report:

```
Change: <name>
Created: <date>
Status: <active/archived>

Artifacts:
  [x] proposal.md
  [x] design.md
  [ ] specs/
  [x] plan.md

Tasks: 5/12 complete (42%)
  Completed: 1.1, 1.2, 2.1, 2.2, 2.3
  Next: 3.1 — <description>
```

3. Count checkboxes in `plan.md`: `- [x]` = complete, `- [ ]` = pending

---

### LIST — Show All Changes

**Triggers:** "list changes", "what changes exist", "/qx-change list"

**Process:**
1. Scan `docs/changes/` (exclude `archive/`)
2. For each change directory:
   - Read `.change.yaml` for created date
   - Count checkboxes in `plan.md` if it exists
3. Display:

```
Active Changes:
  add-combo-system    created: 2026-02-20    tasks: 3/8
  optimize-ai         created: 2026-02-25    tasks: 0/0 (no plan yet)

Recent Archives:
  2026-02-15-fix-inventory-ui
  2026-02-10-add-quest-system
```

---

### VERIFY — Check Implementation Completeness

**Triggers:** "verify", "check completeness", "/qx-change verify"

**Requires:** `plan.md` must exist

**Process:**

Three-dimensional verification with graceful degradation:

**1. Completeness (always checked)**
- Count `- [ ]` vs `- [x]` in `plan.md`
- Report: "N/M tasks complete"
- CRITICAL if incomplete tasks remain

**2. Correctness (if specs exist)**
- For each requirement in `specs/`, search codebase for implementation evidence
- For each scenario, check if corresponding test or logic exists
- WARNING if requirement has no implementation evidence

**3. Coherence (if design.md exists)**
- For each decision in `design.md`, verify implementation follows the chosen approach
- SUGGESTION if implementation diverges from design

**4. Framework Compliance (always checked)**
- Read CLAUDE.md for project-specific rules and verification commands
- Run the project's build/compile commands
- Verify framework-specific declarations and annotations are correct
- Check code generation steps were run if applicable
- Verify no framework rule violations exist

**Output format:**
```
## Verification: <change-name>

| Dimension      | Status  | Score |
|---------------|---------|-------|
| Completeness   | ✓ / ✗  | N/M   |
| Correctness    | ✓ / ✗  | N/M   |
| Coherence      | ✓ / △  | N/M   |
| Framework Compliance | ✓ / ✗ | N/M |

### CRITICAL
- [ ] Task 3.2 is incomplete
- [ ] Requirement "Combo Chain" has no implementation

### WARNING
- Spec scenario "damage overflow" has no test coverage
- Code generation step not run after definition changes

### SUGGESTION
- Design says approach A but implementation uses approach B
```

---

### ARCHIVE — Close Out a Change

**Triggers:** "archive", "done with this", "/qx-change archive"

**Requires:** Change must exist

**Process:**

1. **Check completion**
   - Count incomplete tasks in `plan.md`
   - If incomplete: warn and ask for confirmation
   - "3 tasks remain incomplete. Archive anyway?"

2. **Check delta specs**
   - Look for `docs/changes/<name>/specs/` directory
   - If delta specs exist:

   ```
   Delta specs found:
     specs/combat-system/spec.md
       ADDED: Combo Chain requirement (2 scenarios)
       MODIFIED: Damage Calculation (1 new scenario)

   Options:
   1. Sync to main specs (recommended) — merge into docs/specs/
   2. Archive without syncing
   ```

3. **Sync specs (if chosen)**
   - For each delta spec:
     - Read `docs/specs/<capability>/spec.md` (create if doesn't exist)
     - Apply ADDED: add new requirement blocks
     - Apply MODIFIED: merge changes into existing requirements, preserving unmodified content
     - Apply REMOVED: remove requirement blocks
     - Apply RENAMED: rename requirement headers
   - This is AI-driven intelligent merge, not programmatic — preserve existing content not mentioned in delta

4. **Move to archive**
   ```
   docs/changes/<name>/ → docs/changes/archive/YYYY-MM-DD-<name>/
   ```

5. **Display summary**
   ```
   ## Archived: <name>

   Location: docs/changes/archive/YYYY-MM-DD-<name>/
   Specs synced: combat-system (2 added, 1 modified)
   Tasks completed: 12/12
   ```

---

## Key Principles

- **Artifacts are optional except proposal** — Simple changes can go proposal → plan → execute
- **Specs are for functional requirements** — Skip for pure technical work (performance, refactoring)
- **Don't force the full workflow** — If user just wants to create and plan, let them
- **Change directories are the handoff** — Designer creates proposal + specs, engineer picks up from design
- **One change, one session at a time** — Avoid concurrent modification of the same change across sessions
- **Check system-map.md** — Reference the persistent system relationship map during proposal and design
