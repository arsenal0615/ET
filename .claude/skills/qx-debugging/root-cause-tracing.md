# Root Cause Tracing

## Overview

Bugs often manifest deep in the call stack (wrong type used, message sent to wrong target, object created in wrong context). Your instinct is to fix where the error appears, but that's treating a symptom.

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
Error: AddComponent failed — BuffComponent requires parent of type Character but got Scene
```

### 2. Find Immediate Cause
**What code directly causes this?**
```csharp
scene.AddComponent<BuffComponent>(); // Wrong parent type!
```

### 3. Ask: What Called This?
```csharp
CharacterFactory.Create(scene, characterId)
  → called by EnterMapHelper.EnterMap()
  → called by EnterMapHandler.Run()
  → called by message dispatch
```

### 4. Keep Tracing Up
**What value was passed?**
- `scene` is the root Scene, not a Character entity
- `EnterMapHelper` passed `scene` instead of the newly created Character
- The Character was created but not stored before passing to AddComponent

### 5. Find Original Trigger
**Where did wrong reference come from?**
```csharp
// In EnterMapHelper.EnterMap():
var character = scene.CreateChild<Character>(); // Created correctly
// ... several lines later ...
scene.AddComponent<BuffComponent>(); // BUG: should be character.AddComponent
```

**Root cause:** Variable reference error — used `scene` instead of `character`

## Adding Diagnostic Logging

When you can't trace manually, add instrumentation:

```csharp
// Before the problematic operation
public static void AddComponentSafe<T>(this Container parent) where T : class, new()
{
    Log.Debug($"DEBUG AddComponent<{typeof(T).Name}>: " +
              $"parent={parent.GetType().Name}(Id={parent.Id}), " +
              $"parentContext={parent.ContextName}");

    parent.AddComponent<T>();
}
```

**Critical:** Use the project's logging API — check CLAUDE.md for the correct logging methods and debug tools.

**Run and capture:**
```bash
# Run with debug output enabled, filter for diagnostic logs
# (Use project-specific run commands from CLAUDE.md)
```

**Analyze output:**
- Look for unexpected parent types
- Find the exact call that passes wrong data
- Identify the pattern (same handler? same factory?)

## Tracing Patterns for Multi-Component Systems

### Tracing Message/Request Chains
```
Client → Gateway.HandleRequest()
  → Gateway → Backend: ForwardRequest()
    → Backend: ProcessRequest()
      → ??? (error here)
```

Add logging at each boundary:
```
// In Gateway handler
Log("=== Gateway: Request received, SessionId={id} ===");

// In Backend handler
Log("=== Backend: Request received, Payload={data} ===");
```

### Tracing Object Lifecycle
```
// Add to initialization/creation hook
Log("Component created: parent={parentType}, id={id}, context={contextName}");
```

### Tracing Context Boundaries
```
// Before cross-context calls
Log("Sending to target: targetId={id}, currentContext={ctx}, messageType={type}");
```

## Real Example: Wrong Target

**Symptom:** Handler not found for a request type

**Trace chain:**
1. Request sent to Service A
2. Handler registered for Service A's context
3. But request arrives at Service B (wrong context)
4. Client sent to wrong endpoint
5. Client used the wrong session/connection for the call

**Root cause:** Client code used wrong session for server communication

**Fix:** Changed to use the correct routing mechanism for the target service

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

- **Check project's CLAUDE.md** for logging API and debug tools
- **Before operation:** Log before the dangerous operation, not after it fails
- **Include context:** Object type, parent type, context name, thread/fiber ID, message type
- **Capture stack:** Use the language's stack trace API to show complete call chain
