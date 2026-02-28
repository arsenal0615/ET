# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ET is an open-source game framework for Unity (client) and .NET (server), using C# for both sides. It features an Entity-Component architecture (data/logic separation), Fiber-based concurrency (similar to Erlang processes), Actor messaging, KCP networking, and HybridCLR hot-update support. Version: ET9.0, Unity 6000.0.25, .NET 8.

## Development Environment

- **IDE**: Rider 2024.3+ (required). VS is not officially supported.
- **Unity**: 6000.0.25 (use this exact version for initial setup)
- **.NET**: .NET 8 SDK required
- **Proxy**: Global proxy/VPN needed for Unity packages and NuGet downloads

## Key Commands

### Unity Editor Shortcuts
| Shortcut | Menu Path | Function |
|----------|-----------|----------|
| **F6** | ET/Loader/Compile | Compile hot-update DLLs |
| **F7** | ET/Loader/Reload | Hot-reload DLLs (only during Play mode) |

### Project Initialization (first time)
Unity Menu: `ET/StateSync/Init` (or `ET/LockStep/Init`) — runs Excel export, Proto compilation, assembly reference setup, links ET.sln, sets INITED define.

### Code Generation
```bash
# Proto to C# (message classes)
dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./

# Excel config export
dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./
```

### Build & Run
```bash
# Compile the solution (requires proxy for NuGet)
dotnet build ET.sln

# Start server (single process, from ET root directory)
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=StartConfig/Localhost --Console=1
```

### Unity Menu Tools
- `ET/Loader/ServerTools` — Start/stop server from Unity
- `ET/Loader/Build Tool` — Build client package (output: `./Release/`)
- `ET/Loader/Add ENABLE_VIEW` / `Remove ENABLE_VIEW` — Toggle Entity visualization in Hierarchy
- `ET/Proto/Proto2CS` — Compile proto files
- `ET/Excel/ExcelExporter` — Export Excel configs

## Architecture

### Package System (ET9)
All framework code lives in `cn.etetet.*` packages under `Library/PackageCache/`. Key packages:
- **cn.etetet.core** — Entity, Fiber, EventSystem, ObjectPool, World, ETTask
- **cn.etetet.loader** — Code compilation, hot-reload, build tools, CodeMode management
- **cn.etetet.proto** — Proto message definitions and code generation
- **cn.etetet.excel** — Excel config export tools
- **cn.etetet.statesync** — State-sync demo (main demo package)
- **cn.etetet.sourcegenerator** — Roslyn analyzers and source generators
- **cn.etetet.netinner** — Internal network communication (Actor messaging between Fibers)
- **cn.etetet.login** — Login flow
- **cn.etetet.unit** — Unit/character management
- **cn.etetet.move** — Movement system
- **cn.etetet.ai** — AI framework
- **cn.etetet.aoi** — Area of Interest
- **cn.etetet.hybridclr** — Client hot-update support

### Four-Assembly Split
Code is split into 4 assemblies (hot-update DLLs compiled by F6):

| Assembly | Content | Rules |
|----------|---------|-------|
| **ET.Model** | Entity/Component definitions, data structures | Only data fields, no methods on Entity classes |
| **ET.ModelView** | Client-only model (Unity-dependent components) | Same as Model but for View layer |
| **ET.Hotfix** | Logic/Systems, message handlers, event handlers | Only static classes or classes with EnableClassAttribute |
| **ET.HotfixView** | Client-only view logic (UI, rendering) | Same as Hotfix but for View layer |

### Code Visibility (CodeMode)
Each package's code is organized into `Client/`, `Server/`, `Share/` directories. The **CodeMode** setting in `GlobalConfig` controls which code is compiled:
- **ClientServer** — All code visible (recommended for development)
- **Client** — Client + Share only (for standalone client builds)
- **Server** — Server + Share only

### Entity-Component Pattern
Entities are pure data containers. Logic is implemented as **static extension methods** in separate System classes:

```csharp
// Model assembly — Component definition (data only, no methods)
[ComponentOf(typeof(Unit))]
public class XunLuoPathComponent : Entity, IAwake
{
    public float3[] path;
    public int Index;
}

// Hotfix assembly — System class (logic as extension methods)
[FriendOf(typeof(XunLuoPathComponent))]
public static partial class XunLuoPathComponentSystem
{
    public static float3 GetCurrent(this XunLuoPathComponent self)
    {
        return self.path[self.Index];
    }
}
```

### Key Attributes
- `[ComponentOf(typeof(ParentEntity))]` — Declares which Entity a Component belongs to
- `[ChildOf(typeof(ParentEntity))]` — Declares parent-child Entity relationship
- `[FriendOf(typeof(Entity))]` — Grants access to Entity's private fields from a System class
- `[EntitySystemOf(typeof(Entity))]` — Auto-generates lifecycle System boilerplate
- `[Event(SceneType.X)]` — Event handler registration
- `[MessageHandler(SceneType.X)]` — Network message handler
- `[Invoke(SceneType.X)]` — Invoke handler (function-style call with return value)
- `[EnableMethod]` — Allows methods inside an Entity class (exception to the no-methods rule)

### Fiber (Concurrency Model)
Fiber is the core scheduling unit, similar to Erlang processes. Each Fiber has its own Scene root and runs on one of three schedulers:
- **Main** — Unity main thread
- **Thread** — Dedicated thread per Fiber
- **ThreadPool** — Shared thread pool

Fibers communicate via Actor messages through `MessageQueue`.

### Message System (Proto)
Proto files follow naming convention: `{Name}_{CS}_{StartOpcode}.proto`
- `_C_` = client-only messages, `_S_` = server-only (inner) messages
- Message types declared via comments: `// IMessage`, `// IRequest`, `// IResponse`, `// ISessionRequest`, `// ILocationRequest`, etc.
- `// ResponseType TypeName` comment links Request to its Response

### Networking
- **Session**: Network connection wrapper, handles RPC via `session.Call(request)`
- **KCP**: Default protocol, supports TCP/WebSocket fallback, runtime protocol switching
- **Actor**: Location-transparent messaging. Entities with `MailBoxComponent` can receive messages by ID from any server

## Analyzer Rules (Enforced at Compile Time)

The source generator package enforces strict coding rules:
1. **Entity classes cannot declare methods** — All logic must be in external static System classes
2. **Entity fields are private** — Access only via `[FriendOf]` attribute or the Entity's own System class
3. **Hotfix assemblies: only static classes or EnableClassAttribute classes** — No regular class declarations
4. **No static fields** (except `[StaticField]` annotated) — Prevents state leaks across hot-reloads
5. **Async methods must return ETTask** — Not `Task` or `void`
6. **AddChild/AddComponent type checking** — Must match `[ComponentOf]`/`[ChildOf]` declarations
7. **Client classes cannot appear in Server assemblies** — Namespace separation enforced
8. **No `new` on Entity types** — Must use `AddComponent`/`AddChild` (object pool managed)

## Namespaces
- `ET` — Shared code (both client and server)
- `ET.Client` — Client-specific code
- `ET.Server` — Server-specific code

## Entry Point
- Unity scene: `Packages/cn.etetet.loader/Scenes/Init.unity`
- Code entry: `Entry.StartAsync()` in cn.etetet.core — initializes World singletons (ObjectPool, EventSystem, MessageQueue, NetServices), loads config, creates main Fiber

## Configuration
- **GlobalConfig**: `Packages/cn.etetet.loader/Resources/GlobalConfig` — CodeMode, SceneName, Address
- **YooConfig**: `Packages/cn.etetet.yooassets/Resources/YooConfig` — Asset loading mode (EditorSimulateMode for dev)
- **StartConfig**: Excel files in demo package (`Excel/StartConfig/Localhost/` or `Release/`) — Machine, Process, Scene, Zone configs
