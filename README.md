# GDF Starter Kit

A Unity 6 base project skeleton wiring up the GDF package ecosystem and a curated
set of third-party tools into a working `Bootstrap → Loading → Home → Game`
scene flow.

Use this as the seed for a new Unity 6 project. The wiring, DI composition, UI
scaffolding, scene-load orchestration, and AI-agent conventions are already in
place — drop in your gameplay features and ship.

---

## What's wired up

### GDF packages (UPM, git-installed)

| Package | Repo | Role |
|---|---|---|
| `com.gdf.scene-loader` | [SceneLoader](https://github.com/AlexPyriel/SceneLoader) | Addressables-backed scene loading with exclusive single-scene transitions, parallel additive loads, and explicit `LoadResult`. |
| `com.gdf.addressable-asset-loader` | [AddressableAssetLoader](https://github.com/AlexPyriel/AddressableAssetLoader) | Generic typed loader wrapper around Unity Addressables. |
| `com.gdf.config-repository` | [ConfigRepository](https://github.com/AlexPyriel/ConfigRepository) | ScriptableObject-backed key→config repositories with identity synchronization and provider contracts. |
| `com.gdf.config-import` | [ConfigImport](https://github.com/AlexPyriel/ConfigImport) | Editor-time Google Sheets / local CSV import pipeline that writes into any `ConfigRepository` through the `IConfigImportTarget` bridge. |
| `com.gdf.local-data-storage` | [LocalDataStorage](https://github.com/AlexPyriel/LocalDataStorage) | Typed local persistence with pluggable backends (PlayerPrefs / crash-safe atomic file), slots, schema migrations, change events, sync + async APIs. |
| `com.gdf.audio` | [Audio](https://github.com/AlexPyriel/Audio) | Cue-driven audio service — SFX pooling, music crossfade, per-bus persisted settings. |
| `com.gdf.analytics` | [Analytics](https://github.com/AlexPyriel/Analytics) | Vendor-agnostic analytics facade (Info / Error / Progression / PurchaseRevenue / AdsRevenue) with composite backend dispatch and a Debug backend default. |
| `com.gdf.timer-service` | [TimerService](https://github.com/AlexPyriel/TimerService) | Named countdown timers with offline progression and auto-save. Dailies, cooldowns, stamina systems. |
| `com.gdf.currency` | [Currency](https://github.com/AlexPyriel/Currency) | Multi-currency wallet (Get / Set / Add / Spend / Reset) with earned-total tracking. Empty by default; assign a `CurrencyRepository` asset when a game needs it. |
| `com.gdf.ads` | [Ads](https://github.com/AlexPyriel/Ads) | Vendor-agnostic ad orchestration (banner / interstitial / rewarded) with daily caps and IAP-driven remove-ads. Vendor SDKs plug in through adapter interfaces. |
| `com.gdf.localization-sheets` | [LocalizationSheets](https://github.com/AlexPyriel/LocalizationSheets) | Bridge that pulls public Google Sheets (zero OAuth) through `com.gdf.config-import` and writes into Unity Localization `StringTableCollection`s. |

### Third-party UPM packages

- `jp.hadashikick.vcontainer` — DI container (used by every system in the kit).
- `com.cysharp.unitask` — allocation-free async/await for Unity.
- `com.cysharp.r3` — reactive streams (used by presenters).
- `com.github-glitchenzo.nugetforunity` — NuGet integration.
- `com.coplaydev.unity-mcp` — Unity MCP server for AI assistants (Claude Code, Codex, JetBrains AI, Cursor).
- `com.unity.localization` — primary localization framework (strings, assets, system-locale selection via `SystemLocaleSelector`).
- `com.unity.pipeline` — Unity CLI pipeline for scripted recompile and test runs from the shell.
- `com.coffee.ui-particle` — Particle Effect for UGUI (particle systems rendered through the UI canvas).
- Unity registry: Addressables, Input System, TextMesh Pro, Universal Render Pipeline, Test Framework, AI suite.

### Imported assets

- **DOTween** (Demigiant) — tween library.
- **IngameDebugConsole** — in-game debug console.
- **NaughtyAttributes** — inspector authoring attributes.
- **SerializeInterfaces** & **SerializedCollections** (AYellowpaper) — inspector-serialized interfaces and dictionaries.
- **Graphy** — runtime stats monitor.

### NuGet (via NuGetForUnity)

- `R3` — primary reactive library.
- `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.TimeProvider`, `System.Threading.Channels`, `System.ComponentModel.Annotations` — modern BCL polyfills.

---

## Architecture

### Bootstrap chain

```
BootstrapScene         RootLifetimeScope spawns; BootstrapEntryPoint starts.
      ↓                GameRunnerService.RunAsync()
LoadingScene           Transitional progress screen.
      ↓
HomeScene              Main menu / home screen.
      ↓                Player clicks "Play".
LoadingScene           Same transitional screen used for every flow transition.
      ↓
GameScene              Gameplay placeholder.
```

Every cross-scene transition is routed through `LoadingScene` by
`GameRunnerService.TransitionToSceneAsync`. The loading scene is the single place
where load progress is presented; target scenes are pre-loaded additively and
activated only after the loader is ready to be unloaded.

Each scene has its own `SceneUILifetimeScope` as a child of the root scope. Scene
transitions go through `ISceneLoaderService` (Addressables-driven) with progress
reporting via `ISceneLoadingProgress`.

### Composition root

`Assets/Project/Core/DI/RootLifetimeScope.cs` is the single composition root. It
installs:

- `SceneLoadingInstaller` — registers `SceneConfigRepository` (Addressables-keyed scenes).
- `LocalDataStorageVContainerInstaller`
- `SceneLoaderVContainerInstaller`
- `AudioVContainerInstaller`
- `AddressableUIViewLoaderService`
- `IUIAssetProvider` — Addressables-driven, backed by `UI*PrefabConfigRepository`.
- `GlobalOverlayService` — app-level overlay host (survives scene changes).
- `GameRunnerService` — flow service for scene transitions.
- `BootstrapEntryPoint` — async startup entry point.

### UI system (`Assets/Project/Core/Infrastructure/UI/`)

> The UI framework **code** lives in `Core/Infrastructure/UI/`; UI **assets**
> (fonts, prefabs) stay in `Shared/UI/`.

- **View ↔ Presenter** pattern with `Presenter<TView>` async-startable base.
- Three view kinds: `View` (base), `ScreenView` (one active per scene), `PanelView` (modal stack).
- Three panel modes: `Inline`, `Modal`, `Overlay` with automatic backdrop.
- `UIRoot` MonoBehaviour hosts the authored canvas with separate transforms for screens, panels, overlays, and tutorial dimming.
- `SceneUIService` owns scene-local UI; `GlobalOverlayService` owns app-level panels.
- Prefab resolution via `AddressableUIAssetProvider` with type-keyed `UIScreenPrefabConfigRepository` and `UIPanelPrefabConfigRepository`.
- Editor `Sync Keys` tool generates repository keys from `MonoScript` references — type-safe binding without hand-typed keys.
- Tutorial focus + dimmer system built into `UIRoot` (opt-in via `IOverlayUIService`).

### Scenes

| Scene | Purpose |
|---|---|
| `BootstrapScene.unity` | Spawns `RootLifetimeScope`; the only scene that must be in Build Settings as index 0. |
| `LoadingScene.unity` | Transitional scene shown during loads; displays `LoadingSceneView` with progress. |
| `HomeScene.unity` | Main menu / home. `HomeScenePresenter` wires the primary button to `GameRunnerService.LoadGameSceneAsync`. |
| `GameScene.unity` | Gameplay placeholder with `GameScenePresenter`. |

---

## Project layout

```
Assets/
├── Project/                           Project-specific code and assets
│   ├── Core/
│   │   ├── Bootstrap/                 GameRunnerService
│   │   ├── DI/                        RootLifetimeScope, EntryPoints, prefabs
│   │   └── Infrastructure/
│   │       ├── SceneLoading/          SceneConfigRepository, ESceneType, Installer
│   │       └── UI/                    MVP UI framework code (View/Presenter, scopes, services)
│   ├── Features/
│   │   └── Scenes/                    One folder per scene: Presenter + View
│   │       ├── Home/
│   │       ├── Game/
│   │       └── Loading/
│   ├── Scenes/                        BootstrapScene, LoadingScene, HomeScene, GameScene
│   ├── Settings/                      Authored ScriptableObject assets
│   │   ├── SceneLoading/              Installers, Repositories
│   │   ├── UI/                        Repositories (screen + panel prefab configs)
│   │   └── VContainer/                VContainer settings
│   ├── Shared/
│   │   └── UI/                        UI assets only — fonts, prefabs (framework code moved to Core/Infrastructure/UI)
│   └── Tests/                         EditMode + PlayMode test assemblies
├── Plugins/                           Imported third-party assets
├── Resources/                         DOTween settings asset
└── Settings/                          URP render pipeline settings
```

---

## Requirements

- **Unity 6.x (6000.0+)** with Universal Render Pipeline.
- macOS / Windows / Linux Editor.
- Unity CLI (`unity --version`) + `com.unity.pipeline` package — pinned by this kit; AI agents drive compile and tests through it.

---

## AI-agent setup (optional)

The kit ships with project-level AI-agent configuration under `.claude/` for Claude Code:

- `.claude/docs/` + `.aiassistant/rules/` — coding/package/UI/architecture rules.
- `.claude/skills/` — CCGS game-studio skill set + the Unity CLI agent skill (`unity-cli`) + `new-unity-project`. Project-level skills are available to Claude Code immediately after cloning without any user-side install.

To refresh the full Unity-authored skill catalog from Unity's registry (33 skills: `ui-ugui`, `ui-uitk`, `optimize-text-mesh-pro`, `shader-graph-create-custom-node`, `migrate-birp-to-urp`, `levelplay-unity-integration`, `implement-in-app-purchases`, etc.), run from the project root:

```bash
unity skill install claude-code --local
```

Install is additive and idempotent: it skips skills that already exist and never overwrites user-scope versions. Repeat when offline resolution fails or when a new Unity skill ships.

---

## Getting started

1. Clone the repo.
2. Set the product identity for your new game in **Edit → Project Settings → Player**: update `Company Name`, `Product Name`, and the bundle identifier so the kit no longer ships as "GDF Starter Kit".
3. Open in **Unity 6.x**. First import will resolve all UPM dependencies and Addressables data.
4. Confirm `Assets/Project/Scenes/BootstrapScene` is index 0 in **File → Build Settings → Scenes In Build**.
5. Press **Play** from any scene; bootstrap chain takes over from there.

Expected flow: BootstrapScene composes the root container → loads LoadingScene → loads HomeScene → click "Play" → loads GameScene.

---

## Extending the kit

Typical first-day workflow for a new feature:

1. **New scene:** add the `.unity` file under `Assets/Project/Scenes/`. Add a new `ESceneType` enum value and an entry in `SceneConfigRepository.asset` with the Addressables reference.
2. **Scene scope:** create `MySceneLifetimeScope : SceneUILifetimeScope`, register your `Presenter` as an entry point.
3. **Screen view:** create a `ScreenView`-derived prefab. Register it in `UIScreenPrefabConfigRepository.asset` with the `Sync Keys` button.
4. **Presenter:** derive from `Presenter<TView>`, inject services through the constructor.
5. **Configs:** model game data as `Config<TKey>` types and store them in a `ConfigRepository<TKey, TConfig>` ScriptableObject asset.
6. **Save data:** derive from `LocalData` and use `ILocalDataStorage.Save<T>` / `TryLoad<T>` for persistence. The kit's default binding writes crash-safe files under `Application.persistentDataPath/saves/` (atomic `tmp -> move + .bak`). Add `[DataVersion(N)]` + `[MigrationFor(…)]` migrations as the schema evolves.
7. **Audio:** inject `ISfxService` / `IMusicService` / `IAudioSettingsService`; build cues from your own catalog.
8. **Analytics:** inject `IAnalyticsService` and call `TrackInfo` / `TrackProgression` / `TrackPurchaseRevenue` / `TrackAdsRevenue`. The default binding is `DebugAnalyticsBackend` — swap `AnalyticsVContainerInstaller().WithBackend(vendor)` in `RootLifetimeScope` to add GameAnalytics / Firebase / mixpanel vendor adapters.
9. **Timers:** inject `ITimerService` and call `CreateOrReset("daily_reward", 86400)`. The service applies the offline delta on `Initialize` so timers finish while the game is closed.
10. **Currency:** author a `CurrencyRepository` asset (`Create -> GDF -> Currency -> Currency Repository`), assign it to the `_currencyRepository` field on `RootLifetimeScope`. Inject `ICurrencyService` and call `Add` / `Spend` / `GetEarnedTotal`.
11. **Ads:** implement `IAdsInitializer` / `IAdsBanner` / `IAdsInterstitial` / `IAdsRewarded` once per vendor (AppLovin / UnityAds / IronSource / AdMob) and chain `AdsVContainerInstaller().WithRewarded(…)` in `RootLifetimeScope`. The facade enforces per-placement daily caps and the IAP remove-ads flag.
12. **Tutorial:** build steps as `TutorialStepAsset` subclasses, drop them into a `TutorialDefinition` list, assign the `TutorialInstaller` asset to the `RootLifetimeScope` field. The runner follows active scenes through `IActiveSceneScopeHolder`; each per-game step composes `Highlight` / `SpawnPanel` / `WaitForAction` from `TutorialStepAsset`. Fill the empty `ETutorialHighlightKey` and `ETutorialActionType` enums with per-game values; `TutorialAnalyticsTracker` emits `tutorial:step:<index>_<name>` and `tutorial:complete` through `IAnalyticsService`.
13. **Localization:** open **Edit → Project Settings → Localization → Create** to generate `LocalizationSettings.asset`, then **Window → Asset Management → Localization Tables** to author a `StringTableCollection`. For designers who prefer Google Sheets, author a `LocalizationSheetsImporter` asset — paste a **public** sheet URL into `GoogleSheetsDataSource`, map `(LocaleCode -> SheetName)` entries, click **Reload**. The default locale selector is `SystemLocaleSelector` — the game boots in the player's system locale with the project's default locale as fallback.
14. **Config import (balance / loot / shops):** author a `ConfigImporter` asset — paste a **public** Google Sheets URL, point `Target` at any `ConfigRepository<TKey, TConfig>`, click **Reload**. Supports `Color`, `Sprite`, `AssetReference`, `List<T>` through plug-in `ConfigValueConverter` SOs. OAuth2 data source available for private spreadsheets.

---

## License

MIT — both the kit itself and the GDF packages it depends on.

Imported third-party assets retain their original licenses; see each plugin folder for details.
