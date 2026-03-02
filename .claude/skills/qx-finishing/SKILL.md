---
name: qx-finishing
description: "Use when implementation is complete and all tests pass. Guides completion of development work by presenting structured options for merge, PR, or cleanup."
---

# Finishing Development Work

## Overview

Guide completion of development work by presenting clear options and handling the chosen workflow.

**Core principle:** Verify tests → Present options → Execute choice → Clean up.

**Announce at start:** "Using qx-finishing to complete this work."

## The Process

### Step 1: Verify Before Finishing

**Before presenting options, verify work is complete:**

Run the project's build and test commands:
```bash
# Build (adapt to your project)
dotnet build   # or the project-specific build command

# Tests
dotnet test    # or the project-specific test command
```

> **Note:** Read project CLAUDE.md for the specific build/test commands.

**If tests fail:**
```
Tests failing (N failures). Must fix before completing:

[Show failures]

Cannot proceed with merge/PR until tests pass.
```

Stop. Don't proceed to Step 2.

**If tests pass:** Continue to Step 2.

### Step 2: Check Change Context

Detect if working within a change lifecycle:
1. Check `docs/changes/` for an active change matching this work
2. If found, suggest running `/qx-change verify` first

### Step 3: Determine Base Branch

```bash
git merge-base HEAD main 2>/dev/null || git merge-base HEAD master 2>/dev/null
```

Or ask: "This branch split from [main] — is that correct?"

### Step 4: Present Options

Present exactly these 4 options:

```
Implementation complete. What would you like to do?

1. Merge back to [base-branch] locally
2. Push and create a Pull Request
3. Keep the branch as-is (I'll handle it later)
4. Discard this work

Which option?
```

**Don't add explanation** — keep options concise.

### Step 5: Execute Choice

#### Option 1: Merge Locally

```bash
git checkout [base-branch]
git pull
git merge [feature-branch]

# Verify tests on merged result
[test command]

# If tests pass
git branch -d [feature-branch]
```

Then: Cleanup (Step 6)

#### Option 2: Push and Create PR

```bash
git push -u origin [feature-branch]

gh pr create --title "[title]" --body "$(cat <<'EOF'
## Summary
- [2-3 bullets of what changed]

## Test Plan
- [ ] [verification steps]
EOF
)"
```

Report PR URL to user. Then: Cleanup (Step 6)

#### Option 3: Keep As-Is

Report: "Keeping branch [name]. You can return to it later."

**Don't cleanup.**

#### Option 4: Discard

**Confirm first:**
```
This will permanently delete:
- Branch [name]
- All commits: [commit-list]

Type 'discard' to confirm.
```

Wait for exact confirmation. If confirmed:
```bash
git checkout [base-branch]
git branch -D [feature-branch]
```

Then: Cleanup (Step 6)

### Step 6: Cleanup

**For Options 1, 2, 4:**

Check if in a worktree:
```bash
git worktree list
```

If in a worktree, offer to remove it.

**For Option 3:** Keep everything as-is.

### Step 7: Update Change Status

If working within a change (`docs/changes/[name]/`):
- Option 1 (merge): Suggest running `/qx-change archive`
- Option 2 (PR): Note that archive should happen after PR merge
- Option 4 (discard): Suggest cleaning up the change directory

## Quick Reference

| Option | Merge | Push | Keep Branch | Cleanup |
|--------|-------|------|-------------|---------|
| 1. Merge locally | Yes | - | Delete | Yes |
| 2. Create PR | - | Yes | Keep | Worktree only |
| 3. Keep as-is | - | - | Keep | No |
| 4. Discard | - | - | Force delete | Yes |

## Red Flags

**Never:**
- Proceed with failing tests
- Merge without verifying tests on result
- Delete work without confirmation
- Force-push without explicit request

**Always:**
- Verify tests before offering options
- Present exactly 4 options
- Get typed confirmation for Option 4
- Update change status if in change lifecycle

## Related Skills

- **qx-verification** — Verify work before finishing
- **qx-code-review** — Review code before merge/PR
- **qx-managing-changes** — Archive change after completion
- **qx-compound** — Extract learnings after completion
