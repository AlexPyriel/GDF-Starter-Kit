# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->
<!-- This is the GDF Starter Kit template baseline. Game-specific values (platforms, -->
<!-- input, performance budgets) are marked "set per game" and filled in each clone. -->

## Engine & Language

- **Engine**: Unity (URP — Universal Render Pipeline 17.3)
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP)
- **Physics**: Unity built-in (PhysX) — 2D/3D set per game

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

> **Source of truth for the target platform is the Unity project itself**, not this
> file. The active build target lives in **Build Settings** (`File → Build Profiles`)
> and `ProjectSettings/`. This template is platform-agnostic; when a clone switches
> platform in Unity, that switch *is* the change — do not maintain a duplicate value
> here that can silently drift. To read the current platform, check Build Settings or
> `EditorUserBuildSettings.activeBuildTarget`, not this doc.

- **Target Platforms**: see Unity Build Settings (authoritative) — template ships platform-agnostic
- **Input Methods**: Unity Input System (`com.unity.inputsystem`); concrete action bindings live in `Assets/InputSystem_Actions.inputactions`, set per game
- **Primary Input**: see the `.inputactions` asset (authoritative); set per game
- **Gamepad Support**: provided by Input System out of the box; enable per game
- **Touch Support**: provided by Input System out of the box; enable per game
- **Platform Notes**: add only game-specific UX constraints here; never the platform name itself

## Naming Conventions

- **Classes**: PascalCase; interfaces prefixed `I`; enums prefixed `E` (e.g. `ESceneType`)
- **Variables**: camelCase locals; private fields `_camelCase`; no `var` (explicit types — enforced by `.editorconfig`)
- **Signals/Events**: R3 (`Observable`/`Subject`) for reactive streams
- **Files**: one entity per file, file name = type name
- **Scenes/Prefabs**: PascalCase
- **Namespaces**: `Assets.Project.*` mirroring folder path (e.g. `Assets.Project.Core.Infrastructure.UI`)

## Performance Budgets

- **Target Framerate**: set per game (default 60 FPS)
- **Frame Budget**: set per game
- **Draw Calls**: set per game
- **Memory Ceiling**: set per game

## Testing

- **Framework**: Unity Test Framework (`com.unity.test-framework`) — EditMode + PlayMode
- **Minimum Coverage**: set per game
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- `var` — always use explicit local types (`.editorconfig` flags it)
- Implicit-public interface members — always declare `public` explicitly
- Multiple top-level types in one file (`1 entity — 1 file`)
- Hand-rolled DI / service locators — use VContainer
- Direct `Resources.Load` for runtime UI/content — use Addressables via `AddressableAssetLoader`
- Alternative tween/async libs (LeanTween, iTween, raw coroutines for timing) — use DOTween + UniTask
- Other DI/reactive frameworks (Zenject, UniRx) — use VContainer + R3

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- **DI**: VContainer (`jp.hadashikick.vcontainer` 1.17)
- **Async / Reactive**: UniTask (`com.cysharp.unitask`), R3 (`com.cysharp.r3` — reactive streams)
- **Tweening / animation**: DOTween (Demigiant, `Assets/Plugins/Demigiant/DOTween`), Timeline (`com.unity.timeline`)
- **Assets / loading**: Unity Addressables (`com.unity.addressables` 2.9) + `com.gdf.addressable-asset-loader`
- **GDF infrastructure packages**: `com.gdf.scene-loader`, `com.gdf.config-repository`, `com.gdf.local-data-storage`, `com.gdf.audio`
- **Inspector / serialization**: NaughtyAttributes, AYellowpaper SerializeInterfaces & SerializedCollections
- **UI**: uGUI (`com.unity.ugui` 2.0); TextMeshPro
- **Input**: Unity Input System (`com.unity.inputsystem` 1.19)
- **AI / navigation**: AI Navigation / NavMesh (`com.unity.ai.navigation`)
- **Scripting (optional)**: Visual Scripting (`com.unity.visualscripting`) — available, prefer C# for systems
- **Tooling**: NuGetForUnity, Graphy (runtime stats), MCP for Unity (`com.coplaydev.unity-mcp`)
- **Editor-only AI**: Unity AI assistant/generators/inference — editor tooling, not runtime deps
- **Transitive (NuGet, via R3)**: Microsoft.Bcl.AsyncInterfaces, Microsoft.Bcl.TimeProvider, System.Threading.Channels — do not reference directly

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: `unity-specialist`
- **Language/Code Specialist**: `unity-specialist` (C#)
- **Shader Specialist**: `unity-shader-specialist`
- **UI Specialist**: `unity-ui-specialist` / `ui-programmer`
- **Additional Specialists**: `unity-addressables-specialist` (Addressables/asset loading), `technical-artist` (URP, VFX), `unity-dots-specialist` (only if DOTS is adopted)
- **Routing Notes**: DI (VContainer) and infrastructure code → `unity-specialist`. UGUI screens/panels → `unity-ui-specialist`. Addressable groups and asset providers → `unity-addressables-specialist`.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (`.cs`) | `unity-specialist` |
| Shader / material (`.shader`, `.shadergraph`, `.mat`) | `unity-shader-specialist` |
| UI / screen files (UGUI prefabs, View/Presenter `.cs`) | `unity-ui-specialist` |
| Scene / prefab / level files (`.unity`, `.prefab`) | `unity-specialist` |
| Addressables (asset groups, providers, loaders) | `unity-addressables-specialist` |
| Native extension / plugin files | `unity-specialist` |
| General architecture review | Primary |
