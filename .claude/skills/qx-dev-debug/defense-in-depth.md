# Defense-in-Depth Validation

## Overview

When you fix a bug caused by invalid data, adding validation at one place feels sufficient. But that single check can be bypassed by different code paths, refactoring, or mocks.

**Core principle:** Validate at EVERY layer data passes through. Make the bug structurally impossible.

## Why Multiple Layers

Single validation: "We fixed the bug"
Multiple layers: "We made the bug impossible"

Different layers catch different cases:
- Entry validation catches most bugs
- Business logic catches edge cases
- Environment guards prevent context-specific dangers
- Debug logging helps when other layers fail

## The Four Layers

### Layer 1: Entry Point Validation
**Purpose:** Reject obviously invalid input at API boundary

```csharp
// Factory method (entry point for creating objects)
public static Player CreatePlayer(GameWorld world, int configId)
{
    if (world == null)
        throw new ArgumentNullException(nameof(world));
    if (!world.IsActive)
        throw new ArgumentException($"Cannot create player in inactive world");
    if (configId <= 0)
        throw new ArgumentException($"Invalid configId: {configId}");

    // ... proceed
}
```

### Layer 2: Business Logic Validation
**Purpose:** Ensure data makes sense for this operation

```csharp
// Validate component relationships
public static void AddBuff(this BuffManager self, int buffConfigId)
{
    if (self.Owner == null)
        throw new InvalidOperationException("BuffManager must have an owner");
    if (self.HasBuff(buffConfigId))
    {
        Log.Warning($"Duplicate buff: {buffConfigId} already exists on {self.Owner.Id}");
        return;
    }
    // ... proceed
}
```

### Layer 3: Environment Guards
**Purpose:** Prevent dangerous operations in specific contexts

```csharp
// Prevent cross-context access
public static void ValidateSameContext(object self, object other)
{
    if (GetContextId(self) != GetContextId(other))
    {
        throw new InvalidOperationException(
            $"Cross-context access detected: {self.GetType().Name} " +
            $"accessing {other.GetType().Name}. " +
            $"Use message passing for cross-context communication.");
    }
}

// Prevent unsafe state in hot-reload context
#if DEBUG
public static void ValidateNoUnsafeStaticState<T>()
{
    var fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
    foreach (var field in fields)
    {
        if (!IsMarkedSafe(field))
        {
            Log.Error($"Static field {typeof(T).Name}.{field.Name} — unsafe for hot-reload");
        }
    }
}
#endif
```

### Layer 4: Debug Instrumentation
**Purpose:** Capture context for forensics

```csharp
// Before risky operations
public static T AddComponentWithTrace<T>(this Container parent) where T : class, new()
{
    Log.Debug($"AddComponent<{typeof(T).Name}>: " +
              $"parent={parent.GetType().Name}(Id={parent.Id}), " +
              $"context={parent.ContextName}");

    return parent.AddComponent<T>();
}
```

## Applying the Pattern

When you find a bug:

1. **Trace the data flow** - Where does bad value originate? Where used?
2. **Map all checkpoints** - List every point data passes through
3. **Add validation at each layer** - Entry, business, environment, debug
4. **Test each layer** - Try to bypass layer 1, verify layer 2 catches it

## Example: Wrong Parent Type

Bug: Wrong type used as parent for a component

**Data flow:**
1. Handler receives message → extracts entityId
2. Looks up entity by ID → returns an object
3. Adds component to entity → fails because entity is wrong type

**Four layers added:**
- Layer 1: Handler validates entity is the correct type before proceeding
- Layer 2: Component declaration enforces parent type (compile-time if framework supports it)
- Layer 3: AddComponent runtime check validates parent type matches declaration
- Layer 4: Debug logging before AddComponent shows parent type and id

**Result:** Bug caught at compile time (Layer 2), and at runtime if bypassed (Layer 3)

## Key Insight

All four layers are often necessary. During development, each layer catches bugs the others miss:
- Different code paths bypass entry validation
- State resets can break business logic assumptions
- Cross-context scenarios need environment guards
- Debug logging identifies misuse patterns in production

**Don't stop at one validation point.** Add checks at every layer.
