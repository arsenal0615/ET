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

| Shortcut / Command | Function |
|--------------------|----------|
| **F6** | Compile hot-update DLLs |
| **F7** | Hot-reload DLLs (Play mode only) |
| `ET/StateSync/Init` | Project initialization (first time) |
| `dotnet build ET.sln` | Compile solution |
| `dotnet .../ET.Proto2CS.dll ./` | Proto to C# code generation |
| `dotnet .../ET.ExcelExporter.dll ./` | Excel config export |
| `dotnet Bin/ET.App.dll --Console=1` | Start server |

## Analyzer Rules (Enforced at Compile Time)

1. **Entity classes cannot declare methods** — Logic in static System classes
2. **No `new` on Entity types** — Use `AddComponent`/`AddChild` (object pool)
3. **Async must return ETTask** — Not `Task` or `void`
4. **No static fields** — Unless `[StaticField]` annotated
5. **Hotfix: only static classes** — Or `[EnableClass]` annotated
6. **AddComponent/AddChild type checking** — Must match `[ComponentOf]`/`[ChildOf]`
7. **Namespace separation** — `ET`(shared), `ET.Client`, `ET.Server`
8. **No Client classes in Server assemblies**

## Namespaces

- `ET` — Shared code (both client and server)
- `ET.Client` — Client-specific code
- `ET.Server` — Server-specific code

## Detailed Rules

For detailed architecture, coding patterns, and reference material, see `.claude/project-rules/`:

| File | Content |
|------|---------|
| `architecture.md` | Package system, four-assembly split, CodeMode, entry point, config, build commands |
| `coding-patterns.md` | Entity-Component pattern, code templates, attributes, file naming |
| `messaging-network.md` | Proto conventions, message types, Fiber, Actor, networking |
| `code-review-checklist.md` | Framework compliance checklist for code review |
