---
apply: always
---

# Unity package formation

Use this rule when preparing a Unity package structure for future extraction, git installation, or publication.

## Namespaces (критично)

- **Корневой namespace нового пакета — всегда `GDF.<PackageName>`** (`GDF.Analytics`, `GDF.Currency`, `GDF.Ads`, `GDF.TimerService`, `GDF.LocalizationSheets`).
  - Префикс `GDF.*` защищает от конфликтов с built-in `UnityEngine.*` ветками (`UnityEngine.Analytics`, `UnityEngine.Advertisements`, `UnityEngine.Monetization` и т.п.): без префикса `using Analytics;` в коде-консьюмере резолвится в `UnityEngine.Analytics` и все символы пакета исчезают.
  - Устаревшие пакеты (`Audio`, `LocalDataStorage`, `SceneLoader`, `ConfigRepository`, `AddressableAssetLoader`) остаются с короткими namespace по историческим причинам; переименовывать только по отдельному запросу.
- **Installer sub-namespace: `GDF.<Package>.Installers.<Container>`** (напр. `GDF.Analytics.Installers.VContainer`). Короткий финальный сегмент, совпадающий с именем DI-пакета, допустим — Unity это терпит благодаря отдельным `.ref.dll`.
- **Asmdef `name` = полный namespace** (`GDF.Analytics`, `GDF.Analytics.Installers.VContainer`); имя файла asmdef совпадает с полем `name`.

## Только VContainer-инсталлер, реализации `public sealed` (критично)

- Пакет поставляет **один** инсталлер — `Installers/VContainer/`. Zenject/Reflex-инсталлеры не создаются.
- Реализации сервисов — **`public sealed`** с публичным конструктором, принимающим только публичные типы (конфиг, интерфейсы, `IPublisher<T>`). Хелперы (бэкенды, шифрование, реестры, состояния) остаются `internal`. Если зависимость конструктора — шов для подмены (напр. `IAddressableSceneLoader`), она становится публичным интерфейсом.
- В README раздел **«Using with Zenject / Reflex»** с биндингами публичных типов (Zenject: `Bind<I>().To<T>()`, брокеры через `MessagePipe.Zenject`; Reflex: `AddSingleton(typeof(T), typeof(I))`, брокеры через `BuiltinContainerBuilder`).

## MessagePipe-нативные пакеты

- Доменные факты пакета (изменился баланс, завершился таймер, досмотрена реклама) публикуются через `IPublisher<T>` из конструктора, а не `event Action`. Технические/частые уведомления (`ProgressChanged`, `VolumeChanged`, `DataSaved`) остаются `event Action` — см. ADR-0001 кита.
- Сообщения — `readonly struct` `<Subject><PastTenseVerb>Message`, по одному на файл, в `Runtime/Messages/`, namespace `GDF.<Package>.Messages`, с XML-доками.
- Runtime asmdef ссылается на `MessagePipe`; `package.json` — зависимость `com.cysharp.messagepipe`. VContainer-инсталлер **не** вызывает `RegisterMessagePipe()` — это делает корневой scope консьюмера один раз.
- Тесты пакета работают на настоящем брокере через `BuiltinContainerBuilder` (`AddMessagePipe`, `AddMessageBroker<T>`, `BuildServiceProvider`), без зависимости от VContainer.

## Installer-asmdef для опционального DI (критично)

VContainer-инсталлер **должен компилироваться только когда VContainer действительно установлен**, иначе консьюмер без него падает с CS0246. Паттерн:

```json
{
  "name": "GDF.Analytics.Installers.VContainer",
  "references": [ "GDF.Analytics", "VContainer" ],
  "autoReferenced": true,
  "defineConstraints": [ "ANALYTICS_VCONTAINER" ],
  "versionDefines": [
    { "name": "jp.hadashikick.vcontainer", "expression": "", "define": "ANALYTICS_VCONTAINER" }
  ]
}
```

- `defineConstraints` указывает: ассембли компилируется только когда define установлен.
- `versionDefines` устанавливает define при наличии UPM-пакета `jp.hadashikick.vcontainer`.
- Define должен быть **уникален для каждого пакета** (`ANALYTICS_VCONTAINER`, `CURRENCY_VCONTAINER`, …), иначе коллизии между пакетами.

## VContainer: IInstaller живёт в `VContainer.Unity`

- `IInstaller` объявлен в `VContainer.Unity`, не в `VContainer`. В VContainer-инсталлере **обязательно обе using-директивы**:

```csharp
using VContainer;
using VContainer.Unity;

namespace GDF.<Package>.Installers.VContainer
{
    public sealed class <Name>VContainerInstaller : IInstaller { ... }
}
```

Без `using VContainer.Unity;` получаешь `CS0246: 'IInstaller' could not be found` — классическая ловушка. Проверить место типа можно через `mcp__unityMCP__unity_reflect(action="search", query="IInstaller", scope="packages")` (scope `unity` ничего не вернёт — тип в стороннем UPM-пакете).

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
- `Runtime/AssemblyInfo.cs` when `internal` runtime types must be exposed to package-owned tests.
- `Tests/EditMode/*.asmdef` and test files only when package tests are needed.
- Store the scaffold under `Packages/com.<name>` by default.

## Standard layout

- Package metadata files stay at the package root.
- Runtime code lives under `Runtime/`.
- Editor-only code lives under `Editor/`.
- The package DI integration lives under `Installers/VContainer/`.
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
- The installer assembly lives under `Installers/VContainer/` and references the runtime asmdef (plus `MessagePipe` when the installer resolves publishers itself).
- Service implementations are `public sealed`, so installers need no `InternalsVisibleTo`.
- Use `AssemblyInfo.cs` with `InternalsVisibleTo` only for the package test assembly that exercises `internal` helpers.
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
- **Все `.md`-файлы в корне пакета (LICENSE.md, README.md, CHANGELOG.md) требуют `.meta`**, иначе Unity при импорте UPM-пакета пишет warning `"Asset ... has no meta file, but it's in an immutable folder. The asset will be ignored."`. Шаблон:
  ```yaml
  fileFormatVersion: 2
  guid: <32 hex chars, unique>
  DefaultImporter:
    externalObjects: {}
    userData:
    assetBundleName:
    assetBundleVariant:
  ```
  GUID генерировать `uuidgen | tr -d '-' | tr A-Z a-z`.

## Публикация и force-move тэгов (критично)

- При исправлении бага в уже опубликованном `v1.0.0` **разрешено force-move тэга**, если версию ещё никто внешний не закрепил:
  1. `git commit` в main
  2. `git tag -f v1.0.0`
  3. `git push origin main --force-with-lease` + `git push origin v1.0.0 --force`
  4. У консьюмера (SK) удалить `Library/PackageCache` и `Packages/packages-lock.json`, перезапустить Unity — Package Manager подтянет свежий коммит под тем же тэгом.
- Если тэг могут держать внешние проекты — выпускать патч-версию (`v1.0.1`), а не force-move.

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
