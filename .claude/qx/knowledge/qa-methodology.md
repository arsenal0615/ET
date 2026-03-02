# QA Methodology Knowledge Base

> Injected into qa-tester agent context. Contains testing methodology and quality assurance best practices for Unity game projects.

## Testing Philosophy

### Core Principles
1. **Test what matters** — Focus on player-facing behavior, not implementation details
2. **Automate the repetitive** — Manual testing for exploration, automation for regression
3. **Shift left** — Find bugs earlier (design review > code review > testing > production)
4. **Risk-based priority** — Test high-impact, high-probability scenarios first

### Testing Pyramid (adapted for games)

```
        /  Playtest  \          (Manual, exploratory)
       / Integration  \         (Multi-system, network)
      /   Component    \        (Single system behavior)
     /     Unit         \       (Individual functions)
    /   Static Analysis  \      (Analyzers, build checks)
```

> **Note:** Check the project's build system for available static analysis tools (Roslyn Analyzers, linters, etc.). Many bugs can be caught at compile time — know what your toolchain already covers so you don't duplicate effort in tests.

## Test Categories

### Unit Tests
- Test individual functions and methods in isolation
- Test pure calculation logic (damage formulas, pathfinding, stat calculations)
- Test data structure state transitions
- **Fast**, run in milliseconds

### Component Tests
- Test a single component + its logic together
- Test lifecycle (initialization → operations → cleanup)
- Test edge cases (empty, null, overflow, boundary values)

### Integration Tests
- Test multiple components interacting
- Test network message flow (client → server → response)
- Test cross-process or cross-thread communication
- **Slower**, may need server startup

### Playtest / Exploration Tests
- Manual testing with specific goals
- "Can a player complete the tutorial in 10 minutes?"
- "What happens if player disconnects during trade?"
- Document findings as bug reports or test cases

## Test Strategy

### What to Test
Focus automated testing on areas that static analysis and the compiler can't catch:
- Business logic correctness (damage calculations, buff stacking, economy)
- Network handler behavior (correct response for given request)
- State machine transitions (AI states, game phase states)
- Race conditions in async code
- Config-driven behavior (do data values produce expected results?)
- Cross-process communication paths
- Client-server round-trip scenarios

### What NOT to Test (if your toolchain catches it)
Check your project's static analysis capabilities. Common things that compilers/analyzers catch:
- Type mismatches and missing references
- Structural rule violations (framework-specific)
- Namespace separation issues
- API misuse patterns

> **Note:** Read the project's CLAUDE.md to understand which rules are enforced at compile time.

## Test Design Patterns

### Arrange-Act-Assert (AAA)
```csharp
[Test]
public void DamageSystem_ApplyDamage_ReducesHealth()
{
    // Arrange
    int initialHealth = 100;
    int damage = 30;

    // Act
    int result = DamageCalculation.Apply(initialHealth, damage);

    // Assert
    Assert.AreEqual(70, result);
}
```

### Given-When-Then (BDD style)
```csharp
[Test]
public void GivenPlayerInMap_WhenDisconnects_ThenUnitRemovedAfterTimeout()
{
    // Given: player is in map with a unit
    // When: session disconnects
    // Then: unit is removed after timeout period
}
```

### Test Data Patterns
| Pattern | Use When |
|---------|----------|
| Minimal valid | Happy path tests |
| Boundary values | Edge case tests (0, max, min+1, max-1) |
| Invalid input | Error handling tests (null, empty, negative) |
| Realistic data | Integration tests |
| Worst case | Performance tests |

## Bug Report Template

```markdown
## Bug: [Short description]

**Severity:** Critical / Major / Minor / Cosmetic
**Priority:** P0 / P1 / P2 / P3
**Found in:** [Version/Build/Branch]

### Steps to Reproduce
1. [Exact step]
2. [Exact step]
3. [Exact step]

### Expected Result
[What should happen]

### Actual Result
[What actually happens]

### Evidence
- Screenshot/Video: [link]
- Log excerpt: [relevant lines]
- Server/Client: [which side]

### Environment
- Build type: [Editor/Standalone/Mobile]
- Server config: [if applicable]
- Platform: [Windows/iOS/Android/etc]

### Additional Context
- Reproducibility: [Always / Sometimes / Rare]
- Regression: [Was this working before? Which commit?]
- Workaround: [Any temporary fix?]
```

## Test Plan Structure

### For Each Feature/Sprint
```markdown
# Test Plan: [Feature Name]

## Scope
- What's being tested
- What's NOT being tested (out of scope)

## Test Matrix

| Test Case | Type | Priority | Automated? | Status |
|-----------|------|----------|-----------|--------|
| Happy path login | Integration | P0 | Yes | Pass |
| Invalid password | Component | P1 | Yes | Pass |
| Server down | Integration | P1 | No | Not tested |

## Risk Areas
- [High-risk areas requiring extra attention]

## Test Environment
- Server config: [configuration used]
- Client config: [build type, settings]
- Test data: [config files needed]

## Exit Criteria
- [ ] All P0 tests pass
- [ ] All P1 tests pass or have documented workarounds
- [ ] No Critical bugs open
- [ ] Performance within budget
```

## Performance Testing

### Key Metrics for Game Servers
| Metric | Target | Measurement |
|--------|--------|-------------|
| Server tick time | <50ms per frame | Log tick duration |
| Message round-trip | <100ms (LAN), <300ms (WAN) | Client timestamp diff |
| Entity creation | <1ms per entity | Profile factory method |
| Scene load time | <3s (client) | Measure from request to ready |
| Memory per process | Monitor growth | Track over extended play sessions |

### Load Testing Approach
1. Define player capacity target (e.g., 1000 concurrent per process)
2. Simulate bot clients sending realistic message patterns
3. Monitor: CPU, memory, message queue depth, tick time
4. Find breaking point, then optimize

## Regression Testing Strategy

### When to Run Full Regression
- Before release builds
- After major refactors
- After merging long-lived branches

### When to Run Targeted Regression
- After bug fix (test the fix + related areas)
- After feature completion (test the feature + integration points)
- After config/protocol changes (test affected systems)

### Automated Regression Suite
> **Note:** Use the project's specific test runner commands. Common patterns:
```bash
# Full test suite (adapt command to your project)
dotnet test -v n        # .NET projects
# or: Unity Test Runner  # Unity-native tests

# Targeted by category
dotnet test --filter "Category=Login" -v n

# Smoke test (critical path only)
dotnet test --filter "Priority=P0" -v n
```

## Quality Gates

### Before Code Review
- [ ] Project builds with 0 errors
- [ ] New code has tests
- [ ] Existing tests still pass

### Before Sprint Demo
- [ ] All P0/P1 test cases pass
- [ ] No Critical bugs open
- [ ] Performance within budget
- [ ] Basic playtest completed

### Before Release
- [ ] Full regression suite passes
- [ ] Load test at target capacity
- [ ] Security review completed
- [ ] Playtest signoff from game designer
- [ ] No known Critical or Major bugs
