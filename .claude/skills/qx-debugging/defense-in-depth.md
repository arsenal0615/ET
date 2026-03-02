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
// Factory method (entry point for creating Units)
public static Unit CreateUnit(Scene scene, int configId)
{
    if (scene == null)
        throw new ArgumentNullException(nameof(scene));
    if (scene.SceneType != SceneType.Map)
        throw new ArgumentException($"Unit must be created in Map scene, got: {scene.SceneType}");
    if (configId <= 0)
        throw new ArgumentException($"Invalid configId: {configId}");

    // ... proceed
}
```

### Layer 2: Business Logic Validation
**Purpose:** Ensure data makes sense for this operation

```csharp
// System class — validate Component relationships
public static void AddBuff(this BuffComponent self, int buffConfigId)
{
    if (self.Parent is not Unit)
        throw new InvalidOperationException("BuffComponent must be on a Unit");
    if (self.HasBuff(buffConfigId))
    {
        Log.Warning($"Duplicate buff: {buffConfigId} already exists on Unit {self.Parent.Id}");
        return;
    }
    // ... proceed
}
```

### Layer 3: Environment Guards
**Purpose:** Prevent dangerous operations in specific contexts

```csharp
// Prevent cross-Fiber access
public static void ValidateSameFiber(this Entity self, Entity other)
{
    if (self.Fiber().Id != other.Fiber().Id)
    {
        throw new InvalidOperationException(
            $"Cross-Fiber access detected: {self.GetType().Name}(Fiber={self.Fiber().Id}) " +
            $"accessing {other.GetType().Name}(Fiber={other.Fiber().Id}). " +
            $"Use Actor messages for cross-Fiber communication.");
    }
}

// Prevent static field abuse in hot-reload context
#if DEBUG
public static void ValidateNoStaticState<T>()
{
    var fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
    foreach (var field in fields)
    {
        if (field.GetCustomAttribute<StaticFieldAttribute>() == null)
        {
            Log.Error($"Static field {typeof(T).Name}.{field.Name} without [StaticField] — unsafe for hot-reload");
        }
    }
}
#endif
```

### Layer 4: Debug Instrumentation
**Purpose:** Capture context for forensics

```csharp
// Before risky operations
public static Entity AddComponentWithTrace<T>(this Entity parent) where T : Entity, IAwake, new()
{
    Log.Debug($"AddComponent<{typeof(T).Name}>: " +
              $"parent={parent.GetType().Name}(Id={parent.Id}), " +
              $"scene={parent.IScene.SceneType}, " +
              $"fiber={parent.Fiber().Id}");

    return parent.AddComponent<T>();
}
```

## Applying the Pattern

When you find a bug:

1. **Trace the data flow** - Where does bad value originate? Where used?
2. **Map all checkpoints** - List every point data passes through
3. **Add validation at each layer** - Entry, business, environment, debug
4. **Test each layer** - Try to bypass layer 1, verify layer 2 catches it

## ET-Specific Example

Bug: Wrong Entity type used as parent for Component

**Data flow:**
1. Handler receives message → extracts entityId
2. EntityHelper.Get(scene, entityId) → returns Entity
3. entity.AddComponent<BuffComponent>() → fails because entity is not a Unit

**Four layers added:**
- Layer 1: Handler validates entityId refers to a Unit: `if (entity is not Unit) return error`
- Layer 2: BuffComponent's [ComponentOf(typeof(Unit))] — Analyzer enforces at compile time
- Layer 3: AddComponent runtime check validates parent type matches ComponentOf declaration
- Layer 4: Debug logging before AddComponent shows parent type and id

**Result:** Bug caught at compile time (Layer 2), and at runtime if bypassed (Layer 3)

## Key Insight

All four layers are often necessary. During development, each layer catches bugs the others miss:
- Different code paths bypass entry validation
- Hot-reload can reset state that business logic depends on
- Cross-Fiber scenarios need environment guards
- Debug logging identifies misuse patterns in production

**Don't stop at one validation point.** Add checks at every layer.
