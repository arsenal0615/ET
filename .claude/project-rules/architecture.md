# ET 架构参考

## Package System (ET9)

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

## Four-Assembly Split

Code is split into 4 assemblies (hot-update DLLs compiled by F6):

| Assembly | Content | Rules |
|----------|---------|-------|
| **ET.Model** | Entity/Component definitions, data structures | Only data fields, no methods on Entity classes |
| **ET.ModelView** | Client-only model (Unity-dependent components) | Same as Model but for View layer |
| **ET.Hotfix** | Logic/Systems, message handlers, event handlers | Only static classes or classes with EnableClassAttribute |
| **ET.HotfixView** | Client-only view logic (UI, rendering) | Same as Hotfix but for View layer |

## Code Visibility (CodeMode)

Each package's code is organized into `Client/`, `Server/`, `Share/` directories. The **CodeMode** setting in `GlobalConfig` controls which code is compiled:
- **ClientServer** — All code visible (recommended for development)
- **Client** — Client + Share only (for standalone client builds)
- **Server** — Server + Share only

## Directory Structure

每个包 `Packages/cn.etetet.*/Scripts/` 下分 `Model/` 和 `Hotfix/`，每层再分 `Client/` `Server/` `Share/`。

第三方库包（如 FairyGUI、YooAssets）用 `Runtime/` + `Editor/` 模式，不参与四程序集分离。

## Entry Point

- Unity scene: `Packages/cn.etetet.loader/Scenes/Init.unity`
- Code entry: `Entry.StartAsync()` in cn.etetet.core — initializes World singletons (ObjectPool, EventSystem, MessageQueue, NetServices), loads config, creates main Fiber

## Configuration

- **GlobalConfig**: `Packages/cn.etetet.loader/Resources/GlobalConfig` — CodeMode, SceneName, Address
- **YooConfig**: `Packages/cn.etetet.yooassets/Resources/YooConfig` — Asset loading mode (EditorSimulateMode for dev)
- **StartConfig**: Excel files in demo package (`Excel/StartConfig/Localhost/` or `Release/`) — Machine, Process, Scene, Zone configs

## Unity Menu Tools

- `ET/Loader/ServerTools` — Start/stop server from Unity
- `ET/Loader/Build Tool` — Build client package (output: `./Release/`)
- `ET/Loader/Add ENABLE_VIEW` / `Remove ENABLE_VIEW` — Toggle Entity visualization in Hierarchy
- `ET/Proto/Proto2CS` — Compile proto files
- `ET/Excel/ExcelExporter` — Export Excel configs

## Build & Run Commands

```bash
# Proto to C# (message classes)
dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./

# Excel config export
dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./

# Compile the solution (requires proxy for NuGet)
dotnet build ET.sln

# Start server (single process, from ET root directory)
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=StartConfig/Localhost --Console=1
```
