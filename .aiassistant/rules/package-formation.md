---
apply: always
---

# Unity package formation

Use this rule when preparing a Unity package structure for future extraction, git installation, or publication.

## Source of truth

- Follow the standard Unity UPM package layout as the baseline.
- Default new package drafts to an embedded UPM layout under `Packages/com.<name>`.
- Treat the embedded package directory as the working runtime source when the goal is to shape a reusable package API and validate assembly boundaries early.
- It is acceptable to keep a live `Packages/com.<name>` directory during preparation when the package is being developed in package form from the start.
- If the host project still needs a temporary non-package mirror in `Assets/`, create it only when the user explicitly asks for that workflow.

## Package scope

- The package scaffold should contain only package-facing files and package-local test scaffolding.
- Keep the scaffold minimal until the package is ready to be published or installed from git.
- Package preparation must not break the current project build.

## Required package files

- `package.json`
- `README.md`
- `LICENSE.md`
- `CHANGELOG.md`
- `Runtime/*.asmdef`
- `Runtime/AssemblyInfo.cs` when `internal` runtime types must be exposed to package-owned installers or tests.
- `Tests/EditMode/*.asmdef` and test files only when package tests are needed.
- Store the scaffold under `Packages/com.<name>` by default.

## Standard layout

- Package metadata files stay at the package root.
- Runtime code lives under `Runtime/`.
- Editor-only code lives under `Editor/`.
- Package DI integrations live under `Installers/<Container>/`.
- Package tests live under `Tests/EditMode/` and `Tests/PlayMode/` when needed.
- Public contracts can live under `Runtime/Interfaces/`.
- Concrete runtime implementations and helper internals can live under `Runtime/Internal/`.
- Prefer runtime namespaces without the `GDF.` prefix when matching the established package style of `SceneLoader`, `ConfigRepository`, and `AddressableAssetLoader`.

## Documentation files

- `README.md` should follow the same broad layout as `com.gdf.scene-loader`:
  - title
  - package and assembly names
  - `Features`
  - `Requirements`
  - `Installation (UPM via Git)`
  - `Usage`
  - `Assembly Definition Notes`
- The installation section should use the same shape as `com.gdf.scene-loader`:
  - add the dependency to `Packages/manifest.json`
  - mention `#main` for development tracking
  - show the Unity Package Manager git URL path
- `LICENSE.md` should contain the package license text.
- `CHANGELOG.md` should start from the initial public release format used by `com.gdf.scene-loader`.
- Keep changelog wording factual and release-oriented. Avoid implementation narration like "introduced" or "added".

## Assembly definitions

- Prefer a dedicated runtime asmdef early when package work is intended to become a reusable assembly.
- Place the main runtime asmdef under `Runtime/`.
- Installer assemblies should live under `Installers/<Container>/` and reference the runtime asmdef.
- Prefer package-owned installer assemblies over exposing runtime internals to `Assembly-CSharp`.
- Use `AssemblyInfo.cs` with `InternalsVisibleTo` for package-owned installer and test assemblies when runtime implementations stay `internal`.
- Use a separate test asmdef for package EditMode tests.
- Test asmdefs should reference the runtime asmdef directly and include `optionalUnityReferences: ["TestAssemblies"]`.

## Tests

- Prefer EditMode tests for repository, provider, and serialization-invariant behavior.
- Package tests must live under `Tests/EditMode`.
- Package tests should be discoverable by Unity Test Runner through project-level `testables` in `Packages/manifest.json` when the package is actually installed as a package.

## Internal API and accessibility

- Keep the public API surface small.
- Make only the invariant-enforcing mutators `internal` when they truly should not be called from outside the assembly.
- Do not make externally consumed ScriptableObject models or Unity-facing concrete types `internal` if they must be referenced outside the package assembly.

## Meta files

- Preserve `.meta` files when moving or creating package assets.
- Do not delete package metas unless the corresponding file or folder is intentionally removed.
- When introducing new package folders such as `Runtime/`, `Tests/`, or `Tests/EditMode/`, make sure their folder `.meta` files are created and committed as well.

## Versioning

- Start new package changelogs at `1.0.0` when the package is being shaped as a public release baseline.
- Use the initial release entry to describe the package at a high level, not the development steps.
- If the user wants the extracted standalone package repository to look like a first clean public release, it is acceptable to keep:
  - one initial commit only,
  - one `v1.0.0` tag only,
  - and amend/force-update that initial release instead of creating follow-up historical commits.

## Publication workflow notes

- When validating package tests inside the host project, it is acceptable to temporarily add the package to `Packages/manifest.json -> testables`, run the tests, and then remove that entry afterward.
- If a git package keeps the same tag but its underlying commit is force-updated, ensure Unity resolves the new commit by:
  - forcing package resolution,
  - and, when needed, removing the package entry from `Packages/packages-lock.json` so Package Manager can refresh the pinned hash.
- When moving a package from a host project into its own repository, prefer preserving existing file GUIDs for moved runtime assets by moving files with their `.meta` files instead of recreating them from scratch.

## Operational rule

- Before making package-structure changes, verify whether the user wants:
  - a package scaffold only,
  - a working runtime migration,
  - or a publication-ready git package.
- If the request is only for preparation, using an embedded package in `Packages/com.<name>` is the default and preferred workflow.
- If the user explicitly says not to use `Packages/`, keep the package scaffold alongside the runtime code inside `Assets/` instead.
