# Changelog

All notable changes to GDF Starter Kit.

## [1.1.0] — 2026-10-18

Platform-agnostic enrichment harvested from `narrative_poker`: production-tested
subsystems promoted into the template without pulling in any game-specific or
PC-only dependencies.

### Added

- **Tutorial framework** under `Assets/Project/Features/Tutorial/` — single-tutorial
  MVP: `TutorialDefinition` (ScriptableObject) with ordered `TutorialStepEntry`
  rows, `TutorialStepAsset` base with granular `Highlight` / `SpawnPanel` /
  `WaitForAction` / `WaitForAdvanceClick` / `WaitForButtonClick` / `WaitForEvent`
  mechanisms, scene-safe `TutorialStepRunner` (per-step injection with the active
  scene resolver, resume cursor, per-step and scope cancellation), overlay with
  shaped and rectangular cutouts, raise-above-dim support, scene waiter and
  highlight registry. Enum keys (`ETutorialHighlightKey`, `ETutorialActionType`)
  ship empty — each game fills them with its own values.
- **Analytics abstraction** (`IAnalyticsService`, `NullAnalyticsService`,
  `EAnalyticsProgressionStatus`) under `Assets/Project/Core/Infrastructure/Analytics/`
  — vendor-neutral facade with a no-op default binding; the tutorial funnel
  publishes `tutorial:step:<index>_<name>` and `tutorial:complete` through it.
- **Startup warmup pipeline** (`StartupWarmupService`, `IStartupWarmupStep`,
  `IStartupWarmupAwaiter`) under `Assets/Project/Core/Bootstrap/Warmup/` — fire-
  and-forget background initialization overlapping the first scene transition;
  ships with `AddressablesInitializeStep` and `PreloadLoadingSceneViewStep`.
- **Reusable confirmation dialog** `ConfirmDialogPanel` under
  `Assets/Project/Core/Infrastructure/UI/Views/` — generic yes/no modal with
  configurable labels, Confirm / Cancel / Dismiss callbacks and click-outside
  dismiss support.
- **`UIRoot.SetTutorialPanelsVisible`** on the shared UI host — keeps the
  tutorial root active while the tutorial feature holds visible panels,
  independent of overlay-service dim state.
- **Active-scene signal** on `SceneUILifetimeScope` — each scene announces
  itself and its resolver through `IActiveSceneScopeHolder` on `Awake`, giving
  the tutorial runner a scene-safe injection source.
- **Internals exposure** via `Assets/Project/AssemblyInfo.cs` —
  `[InternalsVisibleTo("Project.Tests.EditMode")]` enables white-box tests
  without widening public API.
- **Tutorial framework EditMode test suite** — `TestInfrastructureSmokeTest`
  plus `TutorialServiceTests`, `TutorialStepRunnerTests`, `TutorialStepBaseTests`,
  `TutorialProgressServiceTests`, `TutorialSceneWaiterTests`, `TutorialOverlayViewTests`,
  `TutorialAnalyticsTrackerTests`, with shared `FrameworkFakes` and
  `RecordingAnalyticsService`.

### Dependencies

- **Added** `com.unity.localization@1.5.5` — primary localization framework.
- **Added** `com.unity.pipeline@0.5.0-exp.1` — Unity CLI pipeline for scripted
  recompile and test runs.
- **Added** `com.coffee.ui-particle@4.13.0` (git) — Particle Effect for UGUI.

### Changed

- `RootLifetimeScope` now accepts an optional `TutorialInstaller` asset
  reference; when assigned, the installer registers the full tutorial system
  into the root scope.
- `GameRunnerService.RunAsync` kicks off `StartupWarmupService.RunAsync` in the
  background so warmup work overlaps the first scene transition; consumers that
  need warmup completion wait on `IStartupWarmupAwaiter`.
- Scene UI scopes (`HomeSceneLifetimeScope`, `GameSceneLifetimeScope`,
  `LoadingSceneLifetimeScope`) now expose their `ESceneType` through the shared
  base and publish it to `IActiveSceneScopeHolder` at scope `Awake`.
- README documents the tutorial framework, the Unity Localization setup path,
  and the newly added UPM dependencies. Lean Localization is now described as
  legacy / optional.
- MCP configuration bumps `mcpforunityserver` to 9.7.3 and drops the obsolete
  `--offline` argument.

### Scope boundaries

Explicitly NOT ported from `narrative_poker` to keep the starter kit
platform-agnostic:

- Full custom audio stack (voice picker, catalog, rules) — superseded by the
  cue-driven `com.gdf.audio` v1.0.0 already in the kit.
- Game-save gateway (`IGameSaveGateway`) — kept as low-level
  `com.gdf.local-data-storage`; game-specific save contracts belong in the clone.
- PC-leaning settings screen (resolution, window mode, brightness, software
  cursor).
- Narrative scroll cutscene (poker-specific presentation).
- Analytics vendor binding — only the abstract facade ships; concrete vendors
  are per-game.
- All poker/castle game content, catalogs and installers.

## [1.0.0] — 2026-10-16

Initial release.
