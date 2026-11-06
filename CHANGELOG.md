# Changelog

All notable changes to GDF Starter Kit.

## [1.4.0] — 2026-11-06

GDF packages publish their facts through MessagePipe; every GDF package ships a VContainer installer only,
with `public sealed` implementations for other containers; VContainer 1.19.

### Changed

- `jp.hadashikick.vcontainer` 1.17.0 → 1.19.0 (bug fixes and Unity 6.2+/6.4 deprecation fixes; no API changes).
- GDF package pins: `com.gdf.local-data-storage` v3.0.0; `com.gdf.scene-loader`, `com.gdf.audio`, `com.gdf.analytics`,
  `com.gdf.currency`, `com.gdf.ads`, `com.gdf.timer-service` v2.0.0.
- `IActiveSceneScopeHolder` / `ActiveSceneScopeHolder` moved from `Features/Tutorial/Framework` to
  `Core/Infrastructure/SceneLoading` (`Interfaces` / `Services`), registered by `SceneLoadingInstaller`. Core no
  longer depends on the tutorial feature.
- The holder publishes `SceneActivatedMessage` instead of raising `ActiveSceneChanged`. `TutorialService` and
  `TutorialSceneWaiter` subscribe to the message; `TutorialStepRunner` still reads `ActiveResolver`.
- ADR-0001: "state via queries, facts via messages", the `*Message` naming rule, packages publish natively.
  `technical-preferences.md` and `.aiassistant/rules/package-formation.md` updated to match.

### Added

- `SceneActivatedMessage` (`Core/Infrastructure/SceneLoading/Messages`).

### Removed

- `IActiveSceneScopeHolder.ActiveSceneChanged`.

### Breaking — migrating a clone

1. Bump `jp.hadashikick.vcontainer` to `#1.19.0` and the seven GDF package pins listed above in `Packages/manifest.json`.
2. Currency: add a reason to every `Set` / `Add` / `Spend` / `Reset` call; replace `CurrencyChanged` handlers with
   `ISubscriber<CurrencyChangedMessage>`.
3. Ads: replace `RewardedWatched` with `ISubscriber<RewardedAdFinishedMessage>` filtered on
   `Result == EAdsRewardedResult.Rewarded`, and `AdsDisabledChanged` with `ISubscriber<AdsDisabledChangedMessage>`.
4. Timers: replace `TimerCompleted` / `TimerChanged` / `TimersChanged` with `TimerCompletedMessage` /
   `TimerTickedMessage` / `TimerStartedMessage` subscriptions.
5. Update `using Assets.Project.Features.Tutorial.Framework.Interfaces;` to
   `using Assets.Project.Core.Infrastructure.SceneLoading.Interfaces;` where `IActiveSceneScopeHolder` is used, move
   its registration to `SceneLoadingInstaller`, and replace `ActiveSceneChanged` handlers with
   `ISubscriber<SceneActivatedMessage>`.
6. Clones on Zenject or Reflex (none in the kit itself) copy the bindings from each package README section
   "Using with Zenject / Reflex".

## [1.3.0] — 2026-11-04

MessagePipe for cross-feature domain events; the tutorial waits on typed messages.

### Added

- `com.cysharp.messagepipe` and `com.cysharp.messagepipe.vcontainer` 1.8.2. `RootLifetimeScope` calls `builder.RegisterMessagePipe()`; any message type resolves through VContainer open generics without registration.
- Tutorial: `ITutorialMessageWaiter` / `TutorialMessageWaiter`; `TutorialStepAsset.WaitFor<TMessage>(token, count)` and `WaitFor<TMessage>(predicate, token, count)`.
- ADR-0001 (`docs/architecture/adr-0001-messaging.md`): when to use R3, `event Action` or MessagePipe, with a package-event bridge recipe.

### Removed

- Tutorial action bus: `ETutorialActionType`, `ITutorialActionBus`, `ITutorialActionPublisher`, `ITutorialActionTracker`, `TutorialActionBus`, `TutorialRunState`, `ITutorialStateProvider`.
- Unused overlay focus channel: `IOverlayUIService.TutorialFocusRequested` / `TutorialFocusCleared` / `OverlayAlphaRequested`, the matching internal methods on `SceneUIService` / `GlobalOverlayService`, and the `TutorialDimmer` / `FocusFrame` objects and fields on `UIRoot` (dim and highlight are owned by `TutorialOverlayView`).

### Breaking — migrating a clone

1. Add both MessagePipe packages to `Packages/manifest.json`, the `MessagePipe` and `MessagePipe.VContainer` references to `Project.asmdef`, and `builder.RegisterMessagePipe();` to `RootLifetimeScope.Configure`.
2. For each `ETutorialActionType` value, declare a `readonly struct` message in the publishing feature (e.g. `DemonFightStarted`).
3. Replace `ITutorialActionPublisher` injection and `Publish(ETutorialActionType.X)` with `IPublisher<X>` and `Publish(new X())`.
4. Replace `WaitForAction(ETutorialActionType.X, token, count)` with `WaitFor<X>(token, count)`. Counting now starts at the call instead of at the run start.
5. Delete the removed tutorial types and update `TutorialContext` construction in tests (`ITutorialMessageWaiter` instead of the action tracker).

## [1.2.3] — 2026-11-02

### Changed

- Manifest pins for 5 new GDF packages bumped `v1.0.0 → v1.0.1`: `com.gdf.analytics`, `com.gdf.ads`, `com.gdf.currency`, `com.gdf.timer-service`, `com.gdf.localization-sheets`. v1.0.1 is a clean release of the GDF.* namespace rename + `using VContainer.Unity;` installer fix, replacing the force-moved v1.0.0 tag.

### Added

- `.claude/skills/unity-cli` and `.claude/skills/new-unity-project` — Unity CLI agent skills installed project-locally via `unity skill install claude-code --local`. Available to Claude Code immediately after cloning the kit, no user-side install required.
- README `AI-agent setup` section documents how to refresh the full Unity-authored skill catalog (33 skills) with `unity skill install claude-code --local`.

## [1.2.2] — 2026-11-02

### Changed

- `com.unity.pipeline` bumped `0.5.0-exp.1` → `0.9.1-exp.1`.
- CLAUDE.md: добавлена заметка, что цикл Unity CLI + Pipeline не зависит от фокуса Editor — вызовы `unity command` можно безопасно отдавать субагенту.

## [1.2.1] — 2026-11-02

### Fixed

- New GDF packages (`com.gdf.analytics`, `com.gdf.ads`, `com.gdf.currency`, `com.gdf.timer-service`, `com.gdf.localization-sheets`) now use `GDF.*` root namespace to avoid conflicts with Unity built-in `UnityEngine.Analytics` / `UnityEngine.Monetization`.
- VContainer installers in those packages add missing `using VContainer.Unity;` so `IInstaller` resolves.
- `com.gdf.addressable-asset-loader` bumped to v1.0.2 (adds `LICENSE.md.meta`, removes Unity import warning).

### Changed

- `.aiassistant/rules/package-formation.md`, `CLAUDE.md`, `.claude/docs/technical-preferences.md` updated with new package authoring rules: GDF.* namespace discipline, VContainer installer gotcha, `defineConstraints` + `versionDefines` pattern for optional DI installers, `set_active_instance` for multi-instance UnityMCP, LICENSE.md meta requirement, force-move tag protocol.

## [1.2.0] — 2026-11-02

Prototype-to-playtest acceleration. Four new GDF packages wired into the composition
root by default, two existing packages upgraded, Lean Localization retired, Google
Sheets authoring path for both balance data and localization.

### New GDF packages (added as default dependencies)

- **`com.gdf.analytics` v1.0.0** — vendor-agnostic analytics facade with composite backend
  dispatch. Replaces the thin in-SK `IAnalyticsService` / `NullAnalyticsService` pair.
  Default binding = `DebugAnalyticsBackend` (logs to console). Swap through
  `AnalyticsVContainerInstaller().WithBackend(...)` to plug GameAnalytics / Firebase.
- **`com.gdf.timer-service` v1.0.0** — named countdown timers with offline progression
  and auto-save. Default autosave 60 s. Backed by `com.gdf.local-data-storage`.
- **`com.gdf.currency` v1.0.0** — multi-currency wallet with earned-total tracking.
  Empty by default — assign a `CurrencyRepository` asset to `RootLifetimeScope._currencyRepository`
  to enable. Backed by `com.gdf.config-repository` + `com.gdf.local-data-storage`.
- **`com.gdf.ads` v1.0.0** — vendor-agnostic ad orchestration: banner, interstitial,
  rewarded video, per-placement daily caps, IAP-driven remove-ads flag. Facade without
  vendor SDKs — clones plug AppLovin / UnityAds / IronSource / AdMob adapters.
- **`com.gdf.config-import` v1.0.0** — editor-time Google Sheets / CSV importer that
  writes into any `ConfigRepository<TKey, TConfig>` through the new `IConfigImportTarget`
  bridge. Public spreadsheets through `gviz/csv` (no OAuth), OAuth2 for private, local
  `TextAsset` data source for fixtures. Pluggable `ConfigValueConverter` SOs for Color,
  Sprite, AssetReference, List types.
- **`com.gdf.localization-sheets` v1.0.0** — bridge that pulls public Google Sheets
  (zero OAuth) via `com.gdf.config-import` and writes them into Unity Localization
  `StringTableCollection`s. Custom backends plug through `ILocalizationBackend`.

### Updated GDF packages

- **`com.gdf.local-data-storage` v1.0.0 → v2.0.0** — pluggable backends (PlayerPrefs
  kept for compatibility, new crash-safe `FileStorageBackend` with atomic
  `tmp -> move + .bak` writes and WebGL IDBFS auto-flush); schema migrations
  (`[DataVersion(N)]` + `[MigrationFor(...)]`); slot overloads; async API via UniTask;
  change events (`DataSaved`, `DataDeleted`, `AllDeleted`); R3 adapter in a separate
  assembly. Backward-compatible with v1 PlayerPrefs saves.
- **`com.gdf.audio` v1.0.0 → v1.1.0** — depends on `com.gdf.local-data-storage@v2.0.0`;
  no public API change.
- **`com.gdf.config-repository` v1.0.0 → v1.1.0** — new `IConfigImportTarget` non-generic
  bridge (explicit implementation on `ConfigRepository<TKey, TConfig>`) so external
  importers can write into any repository without reflecting on its generic arguments.
- **`com.gdf.addressable-asset-loader` v1.0.0 → v1.0.1** — package metadata cleanup
  (`LICENSE.md`, `Assembly Definition Notes` section, removed stale empty folders).

### Changed in SK

- `RootLifetimeScope` installs every GDF package through its DI installer by default:
  `LocalDataStorageVContainerInstaller.WithFileBackend()`, `AnalyticsVContainerInstaller`,
  `TimerServiceVContainerInstaller`, `AdsVContainerInstaller`, optional
  `CurrencyVContainerInstaller(_currencyRepository)`. Save data lives in files out of the box.
- `TutorialAnalyticsTracker` now talks to the external `IAnalyticsService` from
  `com.gdf.analytics` through `TrackInfo` (replaces the removed in-SK `TrackDesign`).

### Removed

- `Assets/Project/Core/Infrastructure/Analytics/` — in-SK thin `IAnalyticsService` /
  `NullAnalyticsService` / `EAnalyticsProgressionStatus`. Replaced by `com.gdf.analytics`.
- `Assets/Plugins/CW/LeanLocalization/` + `Assets/Plugins/CW/LeanCommon/` — Lean Localization
  removed. New work uses Unity Localization (optionally fed by `com.gdf.localization-sheets`).

### Verification

- Compile verification is **not** run in this release — SK's Unity Editor instance was not
  reachable from the Unity CLI pipeline during cut. Open the project in Unity and let the
  package manager resolve all GDF git dependencies; the test suite from v1.1.0 is still in
  place and should pass as before.

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
