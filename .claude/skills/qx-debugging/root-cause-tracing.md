# Root Cause Tracing

## Overview

Bugs often manifest deep in the call stack (wrong Component added, message sent to wrong Fiber, Entity created in wrong Scene). Your instinct is to fix where the error appears, but that's treating a symptom.

**Core principle:** Trace backward through the call chain until you find the original trigger, then fix at the source.

## When to Use

**Use when:**
- Error happens deep in execution (not at entry point)
- Stack trace shows long call chain
- Unclear where invalid data originated
- Need to find which code path triggers the problem

## The Tracing Process

### 1. Observe the Symptom
```
Error: AddComponent failed — NumericComponent requires [ComponentOf(typeof(Unit))] but parent is Scene
```

### 2. Find Immediate Cause
**What code directly causes this?**
```csharp
scene.AddComponent<NumericComponent>(); // Wrong parent type!
```

### 3. Ask: What Called This?
```csharp
UnitFactory.Create(scene, unitId)
  → called by EnterMapHelper.EnterMap()
  → called by G2C_EnterMapHandler.Run()
  → called by message dispatch
```

### 4. Keep Tracing Up
**What value was passed?**
- `scene` is the root Scene, not a Unit entity
- `EnterMapHelper` passed `scene` instead of the newly created Unit
- The Unit was created but not stored before passing to AddComponent

### 5. Find Original Trigger
**Where did wrong reference come from?**
```csharp
// In EnterMapHelper.EnterMap():
var unit = scene.AddChild<Unit>(); // Created correctly
// ... several lines later ...
scene.AddComponent<NumericComponent>(); // BUG: should be unit.AddComponent
```

**Root cause:** Variable reference error — used `scene` instead of `unit`

## Adding Diagnostic Logging

When you can't trace manually, add instrumentation:

```csharp
// Before the problematic operation
public static void AddComponentSafe<T>(this Entity parent) where T : Entity, IAwake, new()
{
    Log.Debug($"DEBUG AddComponent<{typeof(T).Name}>: " +
              $"parent={parent.GetType().Name}(Id={parent.Id}), " +
              $"parentScene={parent.IScene.SceneType}");

    parent.AddComponent<T>();
}
```

**Critical:** Use `Log.Debug()` for ET logging, check Unity Console or server console output

**Run and capture:**
```bash
# Server with console output
dotnet Bin/ET.App.dll --Console=1 2>&1 | grep 'DEBUG AddComponent'
```

**Analyze output:**
- Look for unexpected parent types
- Find the exact call that passes wrong data
- Identify the pattern (same handler? same factory?)

## ET-Specific Tracing Patterns

### Tracing Message Chains
```
Client → Session.Call(C2G_Login)
  → Gate: C2G_LoginHandler.Run()
    → Gate → Realm: ActorMessage(R2G_GetLoginKey)
      → Realm: R2G_GetLoginKeyHandler.Run()
        → ??? (error here)
```

Add logging at each boundary:
```csharp
// In C2G_LoginHandler
Log.Debug($"=== Gate: C2G_Login received, Account={request.Account} ===");

// In R2G_GetLoginKeyHandler
Log.Debug($"=== Realm: R2G_GetLoginKey received, Account={request.Account} ===");
```

### Tracing Entity Lifecycle
```csharp
// Add to Awake System
[EntitySystem]
private static void Awake(this MyComponent self)
{
    Log.Debug($"MyComponent Awake: parent={self.Parent?.GetType().Name}, " +
              $"id={self.Id}, scene={self.IScene.SceneType}");
}
```

### Tracing Fiber Boundaries
```csharp
// Before cross-Fiber Actor call
Log.Debug($"Sending to Fiber: targetActorId={actorId}, " +
          $"currentFiber={self.Fiber().Id}, message={message.GetType().Name}");
```

## Real Example: Wrong Scene Type

**Symptom:** MessageHandler not found for C2M_PathfindingResult

**Trace chain:**
1. `C2M_PathfindingResult` sent to Map scene
2. Handler registered with `[MessageHandler(SceneType.Map)]`
3. But message arrives at Gate scene (SceneType.Gate)
4. Client sent to wrong Session (Gate session instead of Map session)
5. Client used `gateSession.Call()` instead of map actor message

**Root cause:** Client code used wrong session for server communication

**Fix:** Changed to location-based Actor message: `MessageHelper.CallLocationActor()`

## Key Principle

```
Found immediate cause
  → Can trace one level up? → Trace backwards
    → Is this the source? → NO → Keep tracing
    → Is this the source? → YES → Fix at source
      → Add validation at each layer (see defense-in-depth.md)
```

**NEVER fix just where the error appears.** Trace back to find the original trigger.

## Stack Trace Tips

- **In server:** Use `Log.Debug()` — check server console output
- **In client:** Use `Log.Debug()` — check Unity Console
- **Before operation:** Log before the dangerous operation, not after it fails
- **Include context:** Entity type, parent type, SceneType, Fiber ID, message type
- **Capture stack:** `Environment.StackTrace` shows complete call chain in C#
