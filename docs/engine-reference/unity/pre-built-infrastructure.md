# Pre-Built Infrastructure (GDF Starter Kit)

This project is a **template / starter kit**. Core infrastructure is already
implemented and wired through DI. When planning or implementing a game on top of
this kit, **reuse these systems — do not rewrite them from scratch**.

All project code lives under `Assets/Project/` with namespaces `Assets.Project.*`
mirroring the folder path.

## Where the code lives (CCGS skills, read this)

Unity projects keep source under `Assets/`, not `src/`. For this kit:

- **`Assets/Project/`** — the real project source. Skills that scan or implement
  code (`/dev-story`, `/reverse-document`, `/architecture-review`, etc.) must target
  this folder, **not** `src/`.
- **`src/`** — an unused CCGS template placeholder. It is intentionally empty; do not
  put Unity code there.

### Assembly definitions

- **`Project.asmdef`** (`Assets/Project/`) — the runtime assembly. All game code
  compiles here (not into `Assembly-CSharp`). Add a package reference here when a new
  dependency is introduced.
- **`Project.Tests.EditMode`** / **`Project.Tests.PlayMode`** (`Assets/Project/Tests/`)
  — test assemblies referencing `Project`. Put EditMode unit tests under
  `Tests/EditMode/`, PlayMode tests under `Tests/PlayMode/`.
- Editor-only code (custom inspectors) is guarded with `#if UNITY_EDITOR` and lives
  inside the runtime assembly — no separate Editor asmdef is needed.

CI runs both test assemblies via `.github/workflows/unity-tests.yml` (game-ci).

## Composition & Bootstrap (`Assets/Project/Core/`)

- **`RootLifetimeScope`** (`Core/DI`) — root VContainer scope; registers project-wide
  services and infrastructure.
- **`BootstrapEntryPoint`** (`Core/DI/EntryPoints`) — VContainer entry point that
  kicks off startup.
- **`GameRunnerService`** (`Core/Bootstrap`) — drives the initial run / first scene flow.

DI is **VContainer**. Per-scene scopes derive from the scene UI scopes below.

## Scene Loading (`Core/Infrastructure/SceneLoading`)

Built on the `com.gdf.scene-loader` + `com.gdf.config-repository` packages.

- **`ESceneType`** — enum of scene identities.
- **`SceneConfig`** / **`SceneConfigRepository`** — ScriptableObject config + repository
  mapping scene types to addressable scene references.
- **`SceneLoadingInstaller`** — registers scene-loading services into DI.

To add a scene: extend `ESceneType`, add a `SceneConfig` entry to the repository asset.

## UI Framework (`Core/Infrastructure/UI`)

A View / Presenter UI system with Addressable-driven asset loading. Do not build a
parallel UI stack — extend this one.

**Contracts** (`UI/Interfaces`): `IView`, `IUIScreen`, `IUIPanel`, `IUIAssetProvider`,
`IOverlayUIService`.

**Base classes** (`UI/Views`, `UI/Presenters`): `View`, `ScreenView`, `PanelView`, `Presenter`.

**Loading & services** (`UI/Providers`, `UI/Services`):
- `AddressableUIAssetProvider` — resolves UI prefabs via Addressables.
- `AddressableUIViewLoaderService` — instantiates views from the repositories.
- `SceneUIService` — per-scene UI lifecycle.
- `GlobalOverlayService` (`IOverlayUIService`) — global overlay panels (pause, confirm, etc.).
- `UIRoot` (`UI/Hosts`) — root canvas host.

**Configuration** (`UI/Configuration`): `UIScreenPrefabConfig(Repository)`,
`UIPanelPrefabConfig(Repository)`, `UIAssetTypeKey`. Repository keys are
`type.FullName` of the view/panel type — keep them in sync with the class namespace.
Repository assets live in `Assets/Project/Settings/UI/Repositories`.

**Scopes** (`UI/Scopes`): `SceneUILifetimeScope` (base), `HomeSceneLifetimeScope`,
`LoadingSceneLifetimeScope`, `GameSceneLifetimeScope`.

**Editor** (`UI/Editor`): custom inspectors for the prefab repositories.

To add a screen/panel: create a `View`/`Presenter` pair under the relevant feature,
register its prefab in the matching repository asset (key = `type.FullName`).

## Sample Scenes & Features (`Assets/Project/Features/Scenes`)

Three reference scenes demonstrate the flow end to end — use them as patterns,
replace with real game content:

- **Loading** — `LoadingSceneView` / `LoadingScenePresenter`
- **Home** — `HomeSceneView` / `HomeScenePresenter`
- **Game** — `GameSceneView` / `GameScenePresenter`, plus sample overlay panels
  `GamePausePanel`, `GameExitConfirmPanel`

## Other Wired Infrastructure (GDF packages)

- **`com.gdf.config-repository`** — ScriptableObject config repositories (used by scene loading & UI).
- **`com.gdf.local-data-storage`** — local persistence.
- **`com.gdf.audio`** — audio service.
- **`com.gdf.addressable-asset-loader`** — addressable loading helpers.

## Implication for CCGS Skills

This is **not a blank project**. DI, scene loading, UI, config, persistence and audio
are already provided. During `/brainstorm`, `/create-architecture`, `/dev-story` etc.,
build game systems **on top of** this infrastructure rather than proposing to create
equivalents.
