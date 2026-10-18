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
| `com.gdf.local-data-storage` | [LocalDataStorage](https://github.com/AlexPyriel/LocalDataStorage) | Typed local persistence for serializable save payloads. |
| `com.gdf.audio` | [Audio](https://github.com/AlexPyriel/Audio) | Cue-driven audio service — SFX pooling, music crossfade, per-bus persisted settings. |

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
- **Lean Localization** (CW) — legacy localization framework kept for compatibility; new work should prefer `com.unity.localization`.
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
6. **Save data:** derive from `LocalData` and use `ILocalDataStorage.Save<T>` / `TryLoad<T>` for persistence.
7. **Audio:** inject `ISfxService` / `IMusicService` / `IAudioSettingsService`; build cues from your own catalog. See the [com.gdf.audio README](https://github.com/AlexPyriel/Audio).
8. **Tutorial:** build steps as `TutorialStepAsset` subclasses, drop them into a `TutorialDefinition` list, assign the `TutorialInstaller` asset to the `RootLifetimeScope` field. The runner follows active scenes through `IActiveSceneScopeHolder`; each per-game step composes `Highlight` / `SpawnPanel` / `WaitForAction` from `TutorialStepAsset`. Fill the empty `ETutorialHighlightKey` and `ETutorialActionType` enums with per-game values; `TutorialAnalyticsTracker` emits `tutorial:step:<index>_<name>` and `tutorial:complete` through `IAnalyticsService` (bound to `NullAnalyticsService` by default — swap in a vendor implementation to receive events).
9. **Localization:** open **Edit → Project Settings → Localization → Create** to generate `LocalizationSettings.asset`, then **Window → Asset Management → Localization Tables** to author string tables. The default locale selector is `SystemLocaleSelector` — the game boots in the player's system locale, falling back to the project's default locale. Store table collections under `Assets/Project/Localization/`.

---

## License

MIT — both the kit itself and the GDF packages it depends on.

Imported third-party assets retain their original licenses; see each plugin folder for details.
