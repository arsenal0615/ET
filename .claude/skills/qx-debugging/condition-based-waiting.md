# Condition-Based Waiting

## Overview

Flaky tests and unreliable async code often guess at timing with arbitrary delays. This creates race conditions where code works on fast machines but fails under load or in CI.

**Core principle:** Wait for the actual condition you care about, not a guess about how long it takes.

## When to Use

**Use when:**
- Code has arbitrary delays (`await ETTask.CompletedTask` with timer, `Thread.Sleep`)
- Tests are flaky (pass sometimes, fail under load)
- Waiting for async operations to complete
- Polling for state changes

**Don't use when:**
- Testing actual timing behavior (cooldown timers, animation timing)
- Always document WHY if using arbitrary timeout

## Core Pattern

```csharp
// BAD: Guessing at timing
await TimerComponent.Instance.WaitAsync(500); // why 500ms?
var result = GetResult();

// GOOD: Waiting for condition
await WaitForCondition(() => GetResult() != null, "result available", 5000);
var result = GetResult();
```

## Quick Patterns

| Scenario | Pattern |
|----------|---------|
| Wait for Component | `WaitForCondition(() => entity.GetComponent<T>() != null)` |
| Wait for state | `WaitForCondition(() => component.State == TargetState)` |
| Wait for Actor response | Use `session.Call()` which has built-in timeout |
| Wait for event | `WaitForCondition(() => eventFired)` |
| Wait for entity count | `WaitForCondition(() => scene.Children.Count >= N)` |

## Implementation with ETTask

Generic polling function using ET's async model:

```csharp
public static async ETTask WaitForCondition(
    Func<bool> condition,
    string description,
    int timeoutMs = 5000,
    int pollIntervalMs = 50)
{
    long startTime = TimeInfo.Instance.ClientNow();

    while (!condition())
    {
        if (TimeInfo.Instance.ClientNow() - startTime > timeoutMs)
        {
            throw new TimeoutException(
                $"Timeout waiting for {description} after {timeoutMs}ms");
        }

        await TimerComponent.Instance.WaitAsync(pollIntervalMs);
    }
}

// Generic version with return value
public static async ETTask<T> WaitForCondition<T>(
    Func<T> getter,
    Func<T, bool> predicate,
    string description,
    int timeoutMs = 5000,
    int pollIntervalMs = 50)
{
    long startTime = TimeInfo.Instance.ClientNow();

    while (true)
    {
        T value = getter();
        if (predicate(value)) return value;

        if (TimeInfo.Instance.ClientNow() - startTime > timeoutMs)
        {
            throw new TimeoutException(
                $"Timeout waiting for {description} after {timeoutMs}ms");
        }

        await TimerComponent.Instance.WaitAsync(pollIntervalMs);
    }
}
```

## Usage Examples

### Wait for Component to be added
```csharp
// Wait for MoveComponent to appear on Unit
await WaitForCondition(
    () => unit.GetComponent<MoveComponent>() != null,
    $"MoveComponent on Unit {unit.Id}",
    timeoutMs: 3000);

var move = unit.GetComponent<MoveComponent>();
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

### Wait for entity count
```csharp
// Wait for all players to enter the map
await WaitForCondition(
    () => mapScene.Children.Count >= expectedPlayerCount,
    $"{expectedPlayerCount} players in map",
    timeoutMs: 10000);
```

## Common Mistakes

**Bad: Polling too fast**
```csharp
await TimerComponent.Instance.WaitAsync(1); // wastes CPU cycles
```
**Fix:** Poll every 50ms for most cases, 10ms only for time-critical operations

**Bad: No timeout**
```csharp
while (!condition()) await TimerComponent.Instance.WaitAsync(50); // infinite loop risk
```
**Fix:** Always include timeout with clear error message

**Bad: Stale data**
```csharp
var comp = entity.GetComponent<T>(); // cached before loop
while (comp == null) { await ...; } // comp is never re-read!
```
**Fix:** Call getter inside loop for fresh data

**Bad: Using Task instead of ETTask**
```csharp
await Task.Delay(500); // WRONG for ET framework
```
**Fix:** Use `TimerComponent.Instance.WaitAsync()` or custom ETTask-based waiting

## When Arbitrary Timeout IS Correct

```csharp
// Animation plays for exactly 2 seconds, need to wait for it
await WaitForCondition(
    () => animationComponent.IsPlaying,
    "animation started");

// Timing-based wait is justified here — animation has known duration
await TimerComponent.Instance.WaitAsync(2000);
// 2000ms = animation duration — documented and justified
```

**Requirements:**
1. First wait for triggering condition
2. Based on known timing (not guessing)
3. Comment explaining WHY

## Real-World Impact

Applying condition-based waiting in ET projects:
- Eliminates flaky tests caused by arbitrary `WaitAsync` calls
- Reduces test execution time (no unnecessary waiting)
- Makes async behavior deterministic and debuggable
- Clear timeout messages identify exactly what condition wasn't met
