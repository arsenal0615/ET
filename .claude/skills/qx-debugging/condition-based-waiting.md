# Condition-Based Waiting

## Overview

Flaky tests and unreliable async code often guess at timing with arbitrary delays. This creates race conditions where code works on fast machines but fails under load or in CI.

**Core principle:** Wait for the actual condition you care about, not a guess about how long it takes.

## When to Use

**Use when:**
- Code has arbitrary delays (`Thread.Sleep`, `Task.Delay`, framework-specific timers)
- Tests are flaky (pass sometimes, fail under load)
- Waiting for async operations to complete
- Polling for state changes

**Don't use when:**
- Testing actual timing behavior (cooldown timers, animation timing)
- Always document WHY if using arbitrary timeout

## Core Pattern

```csharp
// BAD: Guessing at timing
await Task.Delay(500); // why 500ms?
var result = GetResult();

// GOOD: Waiting for condition
await WaitForCondition(() => GetResult() != null, "result available", 5000);
var result = GetResult();
```

## Quick Patterns

| Scenario | Pattern |
|----------|---------|
| Wait for component | `WaitForCondition(() => entity.GetComponent<T>() != null)` |
| Wait for state | `WaitForCondition(() => component.State == TargetState)` |
| Wait for RPC response | Use built-in RPC timeout mechanisms |
| Wait for event | `WaitForCondition(() => eventFired)` |
| Wait for count | `WaitForCondition(() => collection.Count >= N)` |

## Generic Implementation

A polling function that waits for a condition with timeout:

```csharp
public static async Task WaitForCondition(
    Func<bool> condition,
    string description,
    int timeoutMs = 5000,
    int pollIntervalMs = 50)
{
    var startTime = DateTime.UtcNow;

    while (!condition())
    {
        if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
        {
            throw new TimeoutException(
                $"Timeout waiting for {description} after {timeoutMs}ms");
        }

        await Task.Delay(pollIntervalMs);
    }
}

// Generic version with return value
public static async Task<T> WaitForCondition<T>(
    Func<T> getter,
    Func<T, bool> predicate,
    string description,
    int timeoutMs = 5000,
    int pollIntervalMs = 50)
{
    var startTime = DateTime.UtcNow;

    while (true)
    {
        T value = getter();
        if (predicate(value)) return value;

        if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
        {
            throw new TimeoutException(
                $"Timeout waiting for {description} after {timeoutMs}ms");
        }

        await Task.Delay(pollIntervalMs);
    }
}
```

> **Note:** If your project uses a custom async framework (e.g., custom task types, framework-specific timers), adapt this implementation to use the project's async primitives. Check CLAUDE.md for the correct async patterns.

## Usage Examples

### Wait for component to be added
```csharp
// Wait for a component to appear on an entity
await WaitForCondition(
    () => entity.GetComponent<MoveComponent>() != null,
    $"MoveComponent on entity {entity.Id}",
    timeoutMs: 3000);

var move = entity.GetComponent<MoveComponent>();
// Now safe to use
```

### Wait for state change
```csharp
// Wait for AI to enter Chase state
await WaitForCondition(
    () => aiComponent.CurrentState,
    state => state == AIState.Chase,
    "AI enters Chase state",
    timeoutMs: 5000);
```

### Wait for collection count
```csharp
// Wait for all players to join
await WaitForCondition(
    () => room.Players.Count >= expectedPlayerCount,
    $"{expectedPlayerCount} players in room",
    timeoutMs: 10000);
```

## Common Mistakes

**Bad: Polling too fast**
```csharp
await Task.Delay(1); // wastes CPU cycles
```
**Fix:** Poll every 50ms for most cases, 10ms only for time-critical operations

**Bad: No timeout**
```csharp
while (!condition()) await Task.Delay(50); // infinite loop risk
```
**Fix:** Always include timeout with clear error message

**Bad: Stale data**
```csharp
var comp = entity.GetComponent<T>(); // cached before loop
while (comp == null) { await Task.Delay(50); } // comp is never re-read!
```
**Fix:** Call getter inside loop for fresh data

**Bad: Using wrong async primitive**
```
// If your framework has its own async system, use it instead of standard Task
// Check CLAUDE.md for the correct async patterns
```
**Fix:** Use the project's recommended async/await primitives

## When Arbitrary Timeout IS Correct

```csharp
// Animation plays for exactly 2 seconds, need to wait for it
await WaitForCondition(
    () => animationComponent.IsPlaying,
    "animation started");

// Timing-based wait is justified here — animation has known duration
await Task.Delay(2000);
// 2000ms = animation duration — documented and justified
```

**Requirements:**
1. First wait for triggering condition
2. Based on known timing (not guessing)
3. Comment explaining WHY

## Real-World Impact

Applying condition-based waiting in projects:
- Eliminates flaky tests caused by arbitrary delays
- Reduces test execution time (no unnecessary waiting)
- Makes async behavior deterministic and debuggable
- Clear timeout messages identify exactly what condition wasn't met
