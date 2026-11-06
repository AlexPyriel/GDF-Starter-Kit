# GDF Starter Kit v1.1.0 — Политика обогащения из narrative_poker

Документ фиксирует policy-решения по переносу наработок проекта
`narrative_poker` в шаблон `GDF Starter Kit`. Составлен через
`/grill-me` 2026-10-18, разрешает все развилки, которые влияют на
структуру и зависимости SK.

Для каждого решения зафиксировано: что принято, какая альтернатива
отвергнута и почему. Этот документ — источник правды для всех
последующих фаз реализации v1.1.0; при расхождении приоритет у него.

---

## 1. Preflight-рамки

| # | Рамка | Решение | Обоснование |
|---|---|---|---|
| P1 | Целевые платформы | PC Standalone + mobile (Android/iOS) агностично | SK — универсальный шаблон; Steam-only ограничение противоречит задаче «не затачивать под стим». Автоматически выводит из скоупа Steam-SDK, Steam-UI и PC-only системы (resolution/window-mode/software-cursor/brightness). |
| P2 | Язык документов | Гибрид: регламенты (CLAUDE.md, AGENTS.md, `.aiassistant/rules/`) — RU; публичное (README, CHANGELOG, LICENSE) — EN; code XML-docs — EN | Регламенты пишутся для своей команды — RU комфортнее. Публичные артефакты пригождаются внешним разработчикам. Code docs всегда EN для open-source-дружественности. |
| P3 | Модель релиза | Один большой v1.1.0 big-bang (orphan-squash + force-push + tag, как делали v1.0.0) | Чистая история, один тег, один чётко описанный rollup. Фазы — только для внутренней организации работы, в коммитах не отражаются. |
| P4 | Связь с narrative_poker | NP идёт своим путём, SK развивается независимо | NP — живой game-проект со своими задачами; SK — template-of-truth. Миграционный гайд не пишется; если NP в будущем захочет что-то из SK v1.1 — делает ручной cherry-pick. |

---

## 2. Принято к портированию (IN)

### 2.1. Пакеты (manifest.json)

| Пакет | Источник | Назначение | Trade-off |
|---|---|---|---|
| `com.unity.pipeline` 0.5.0-exp.1 | UPM registry | Unity CLI compile/test cycle | experimental: может поменять API в будущем — обновим регламент при необходимости |
| `com.unity.localization` | UPM registry | i18n framework (string tables, locale selector, per-locale assets) | first-party, работает на Addressables (у SK уже есть), долгосрочная поддержка |
| `com.coffee.ui-particle` | git | Particle System внутри UGUI Canvas | один git URL, никаких зависимостей |

### 2.2. Регламенты (CLAUDE.md)

| Раздел | Что фиксирует | Источник |
|---|---|---|
| «Компиляция и тесты — Unity CLI + Pipeline» | Цикл `unity command recompile → poll recompile_status → run_tests`; fallback на UnityMCP, если `unity` CLI не установлен | NP CLAUDE.md |
| «UI — развилка по технологии» | UGUI — дефолт для runtime UI; UI Toolkit — только для editor-тулинга; как какой tool-dispatch | NP CLAUDE.md (адаптировано) |
| «Грилл-ми и развилки — сначала ГДД» | Обязательная штудировка ГДД перед grill-me/AskUserQuestion/развилкой — **wrapped in `when GDD exists`** (SK как template ГДД не имеет; правило срабатывает, когда клон добавит `design/gdd/`) | NP CLAUDE.md (с оберткой) |

### 2.3. Код-инфраструктура

**Core/Bootstrap/Warmup/** — система прогрева на старте.
- Framework: `StartupWarmupService`, `IStartupWarmupStep`, `IStartupWarmupAwaiter`
- Готовые шаги: `PreloadLoadingSceneViewStep`, `AddressablesInitializeStep`
- Интеграция в `BootstrapEntryPoint`
- **Не** переносится: `LoadSavesStep` (зависит от game-specific `IGameSaveGateway`)

**Core/Infrastructure/Analytics/** — вендор-агностичная аналитика.
- `IAnalyticsService` — контракт
- `NullAnalyticsService` — default-impl (debug-log или ноль)
- Регистрация в `RootLifetimeScope`
- **Не** переносится: GameAnalytics-импл (vendor-lock)

**Core/Infrastructure/UI/Views/ConfirmDialogPanel.cs** — переиспользуемая модальная «да/нет».
- Берётся из `Features/Home/Presentation/Views/ConfirmDialogPanel.cs` NP
- Перемещается в `Core/Infrastructure/UI/Views/` (генерик, не game-specific)
- Префаб — минимальный scaffold

**Features/Tutorial/Framework/\*** — полный tutorial framework.
- `Services/`: TutorialService, StepRunner, HighlightRegistry, MessageWaiter (MessagePipe, с v1.3.0 вместо ActionBus/RunState), SceneWaiter (на `SceneActivatedMessage`), Overlay; `ActiveSceneScopeHolder` с v1.4.0 живёт в `Core/Infrastructure/SceneLoading`
- `Config/`: TutorialDefinition, TutorialStepEntry
- `Enums/`: **ETutorialHighlightKey — пустой со TODO-комментарием для клона** (NP-значения не переносятся)
- `Interfaces/`: все
- `Steps/Base/` — базовые классы
- `Presentation/Views/` — TutorialOverlayView
- `Installers/TutorialInstaller.cs` — DI-обвязка
- **Не** переносится: `Steps/Castle/`, `Steps/Poker/` (game-specific)

**Features/Tutorial/Progress/** — прогресс туториала.
- `ITutorialProgressService` + `PlayerPrefsTutorialProgressService` (default-impl)
- **Не** переносится: `TutorialKeys` (game-specific значения ключей)

**Features/Tutorial/Analytics/TutorialAnalyticsTracker.cs** — подписывается на tutorial events, посылает в `IAnalyticsService`.

**Shared/UI/Shaders/UITutorialCutout.shader** + материал — шейдер затемнения с «дырками» под подсвеченные RectTransform. Используется `TutorialOverlayView`.

### 2.4. Тесты

| Файл | Назначение |
|---|---|
| `Assets/Project/AssemblyInfo.cs` | `[assembly: InternalsVisibleTo("Project.Tests.EditMode")]` для white-box тестирования |
| `Tests/EditMode/TestInfrastructureSmokeTest.cs` | Замена заглушки `UIAssetTypeKeyTests.cs` — проверяет sanity test-runner + видимость runtime-типов |
| `Tests/EditMode/Tutorial/Framework/\*` | Полный test suite Tutorial Framework (7 файлов из NP): TutorialServiceTests, TutorialStepRunnerTests, TutorialStepBaseTests, TutorialProgressServiceTests, TutorialSceneWaiterTests, TutorialOverlayViewTests, FrameworkFakes |
| `Tests/EditMode/Project.Tests.EditMode.asmdef` | Обновить refs: + UniTask, VContainer, R3.Unity, GDF.LocalDataStorage, GDF.Audio, Unity.Localization, Coffee.UIParticle; + precompiled DOTween.dll, R3.dll |

### 2.5. Конфигурация MCP

| Файл | Изменение |
|---|---|
| `.mcp.json` | `mcpforunityserver`: 9.6.8 → 9.7.3 (или новейшая стабильная на момент релиза); блок `serena` остаётся на project-level (zero-setup для контрибьютеров) |

### 2.6. Project.asmdef

Расширить `references`:
- + `Unity.Localization`
- + `Unity.Localization.Runtime`
- + `Coffee.UIParticle`

Не добавлять: Spine, GameAnalytics, Steamworks, LeanLocalization.

### 2.7. Локализация scaffold

Минимальный scaffold Unity Localization:
- Default EN LocaleTable (пустая)
- SystemLocaleSelector
- README-пример замены TMP-текста на `LocalizedString`

Файлы в `Assets/Project/Shared/Localization/` (новая папка).

---

## 3. Явно отклонено (OUT)

### 3.1. Пакеты

| Пакет | Причина отказа |
|---|---|
| `com.rlabrecque.steamworks.net` | PC-Steam-only, нарушает платформо-агностичность (P1) |
| `com.esotericsoftware.spine.spine-csharp`, `spine-unity` | 2D skeletal — узкий use-case; Pro-лицензия Esoteric для commercial — риск юр. путаницы в template |
| `com.gameanalytics.sdk` | Vendor-lock; у нас только абстракция (`IAnalyticsService`); каждый клон выбирает вендора сам |
| OpenUPM scopedRegistry | Нужен был только для GameAnalytics — не требуется |
| `com.google.external-dependency-manager` | Нужен был только для GameAnalytics (Android/iOS native deps) — не требуется |
| LeanLocalization (via CW / Asset Store) | В пользу Unity Localization (first-party, Addressables-backed, лучше для mobile/CJK/RTL) |

### 3.2. Код

| Что | Причина отказа |
|---|---|
| `Core/Infrastructure/Audio/\*` (NP custom audio stack: ~30 файлов) | Конфликт философии с `com.gdf.audio` v1.0.0 (cue-driven). Решено оставить GDF-пакет, игнорировать NP-стек. Не хотим идти против опубликованного пакета. |
| `Core/Persistence/IGameSaveGateway.cs` + `Features/Castle/Persistence/GameSaveGateway.cs` | Game-specific: явные упоминания «замок» и «покерная кампания». SK = только низкоуровневый `com.gdf.local-data-storage`. |
| `Core/Infrastructure/Settings/\*` целиком | PC-leaning: Resolution, WindowMode, SoftwareCursor, Brightness. Volume/mute уже покрыт `IAudioSettingsService`. Language покроет Unity Localization. |
| `Core/Infrastructure/Steam/\*` + `Core/Infrastructure/Steam/UI/\*` | Steam-only — противоречит P1 |
| `Core/Bootstrap/Warmup/Steps/LoadSavesStep.cs` | Зависит от `IGameSaveGateway` (который не переносим) |
| `Shared/UI/Widgets/\*` (HoverTooltip, PerspectiveTiltWarp, CustomVerticalScrollbar, OrnamentFrameGraphic, LabelPlate, TrapezoidWarp, TooltipView, ToggleActiveButton, ScrollRectSliderLink) | Opinionated UI-виджеты; SK не навязывает визуальный язык. Клон сам решает |
| `Shared/UI/Utilities/\*` (IdleParallaxDrift, UIFogWarpSpriteRect, GlowFlickerSeed, RandomSeedInstance, UIForceRebuilder) | Same reason |
| `Shared/UI/Zones/\*` (SpriteUvRect, StringsStrumDriver, StrumModel, ImageBorder, ShaderStarter, ZoneMaskBaker, ZonePolygon) | Thematic (strum/zone — poker/casino) + opinionated |
| `Shared/UI/Shaders/\*` (кроме `UITutorialCutout`) | Thematic (CardBurn, CardTrail, MagicVeil, SharedThread, Zone-{Wind, Strings, Smoke, Curtain}, Fog, FogWarp, HardFog, Dust, PaintLayer, TmpPaintReveal) — casino/narrative/fantasy визуальный язык |
| `Shared/UI/Dialog/\*` (DialogMessageView, DialogCutsceneTuningConfig, DialogBubbleGrowth) | Narrative-specific (cutscene bubbles) |
| `Shared/UI/Animation/Decorations/\*` (HoverDrift, VerticalStretch, PopInAppear, PictureSwing, FogSway, Pendulum, CloudDrift, CurvedPath, StarTwinkle, EdgeLightning + EyeDecorations) | Thematic — анимационный язык конкретной игры |
| `Shared/UI/Fonts/{Ebbe, LXGW_WenKai_TC}` | Visual language-specific (Ebbe — poker-brand); CJK-fallback — клон добавит сам |
| `Shared/UI/{Materials, Paint, Sprites}` | Art assets конкретной игры |
| `Shared/Localization/{Loc, LocText, LocalizedSpriteSwitcher}` | Обёртки над LeanLocalization — не переносим; будем на Unity Localization |
| `Shared/{Abilities, Demons, Sounds}` | Game-specific |
| `Features/Castle, Features/Poker, Features/Tutorial/Home, Features/Tutorial/Narrative, Features/Playtest` | Game-specific |
| `Features/Home/{Configuration, Startup, Presentation/Views/HomeStartupArtworkView, Installers/HomeSceneInstaller}` | Art-direction-opinionated animation flow |
| `Features/Loading/*` (расширения над basic) | Same reason |
| `Features/Tutorial/Framework/Steps/{Castle, Poker}` | Game-specific tutorial steps |
| `Features/Tutorial/Progress/TutorialKeys.cs` | Game-specific keys enum |
| `Assets/Project/Editor/SaveTools/SaveToolsMenu.cs` | Зависит от game-specific save data (Castle, Playtest, Tutorial-Progress keys) |
| `Assets/Project/Editor/Build/BuildCommand.cs` | PC + Windows + Steam-specific; противоречит P1 |
| `Assets/Project/Editor/SceneLauncher/SceneLauncherWindow.cs` | Хардкод Castle/Poker в `FLOW_SCENES` dictionary; game-coupled |
| `Features/Castle/Editor/\*` (CastleImportReport, CastleImportTargets, CharacterVisualPrefabGenerator, RelationshipKindCatalogMenu, CastleDataImportWindow, …) | Castle-specific editor tooling |

### 3.3. Инфраструктура и docs

| Что | Причина отказа |
|---|---|
| `.github/workflows/unity-tests.yml` активный | GameCI требует UNITY_LICENSE секретов, которые GitHub не копирует в template-forks. Будет красная галочка на каждом push у клонов. Оставляем `.yml.disabled` — клон переименовывает когда настроит секреты |
| `docs/analytics/\*`, `docs/consistency-failures.md`, `docs/poker-banner-content.md`, `docs/adoption-plan-2026-05-28.md` | Game-specific project docs |
| `Assets/AddressableAssetsData/Windows/` | Platform-specific build state |
| `Assets/Editor/SpineSettings.asset` | Spine-specific (Spine не переносим) |

---

## 4. Фазовый план реализации

Фазы — для внутренней организации работы. В git-истории не отражаются
(всё squash-итcя в один `Initial release (v1.1.0)` в финале, как
делали v1.0.0).

| # | Фаза | Что внутри | Scope-оценка |
|---|---|---|---|
| 0 | Политика на диск | Этот файл | 1 файл (готово) |
| 1 | Packages + CLAUDE.md regulations | `manifest.json` (+ 3 пакета, в том числе Unity Localization) · `.mcp.json` (+ version bump) · CLAUDE.md (+ 3 раздела: Pipeline cycle, UI tech, GDD-first-wrapped) | 3 файла |
| 2 | AssemblyInfo + Warmup | `Assets/Project/AssemblyInfo.cs` · `Core/Bootstrap/Warmup/*` (framework + 2 степа) · интеграция в `BootstrapEntryPoint` | ~6 файлов |
| 3 | Analytics abstraction | `Core/Infrastructure/Analytics/{IAnalyticsService.cs, NullAnalyticsService.cs}` · регистрация в `RootLifetimeScope` | ~3 файла |
| 4 | ConfirmDialog | `Core/Infrastructure/UI/Views/ConfirmDialogPanel.cs` · минимальный префаб | ~2 файла + prefab |
| 5 | Tutorial Framework | Полный `Features/Tutorial/Framework/*` + `Progress/*` + overlay view + shader + материал + префабы + `TutorialAnalyticsTracker` + `TutorialInstaller` | ~30 файлов + shader + material + prefab |
| 6 | Tests | `TestInfrastructureSmokeTest.cs` → замена заглушки · Tutorial Framework test suite (7 файлов) · обновление `Project.Tests.EditMode.asmdef` refs | ~10 файлов |
| 7 | Localization scaffold | Default EN LocaleTable + SystemLocaleSelector + README-пример (`Shared/Localization/`) | ~3 файла + 2 asset |
| 8 | Финализация | Обновить `Project.asmdef` refs · README (новые секции про Pipeline, Unity Localization, Tutorial framework) · CHANGELOG `## [1.1.0]` | 3 файла |
| 9 | Verify + release | Local compile check · run tests · orphan-squash · force-push main · tag v1.1.0 | git operations |

Каждая фаза заканчивается подведением итогов. Переход к следующей —
только по подтверждению пользователя. После всех фаз — git-операции
(Фаза 9) с обязательным safety-tag перед force-push.

---

## 5. Что сделать после релиза v1.1.0 (не в скоупе)

Явно вне скоупа текущего релиза, записано на будущее:

- Если `com.unity.pipeline` сменит стабильную версию — bump в
  следующем релизе SK.
- Если `com.gdf.audio` v2.0.0 когда-то будет разработан (с
  Catalog-подходом как NP) — отдельный major-релиз SK, миграция
  клонов задокументирована отдельно.
- `Shared/UI/Widgets/HoverTooltip` + `TooltipView` — потенциальный
  следующий UPM-пакет `com.gdf.ui-tooltip`, если накопится
  спрос.
- Полноценный Settings-framework — отдельный пакет `com.gdf.settings`
  с модульным дизайном (desktop/mobile опциональны).

---

## 6. История решений

| Дата | Действие | Автор |
|---|---|---|
| 2026-10-18 | Создание политики по итогам `/grill-me` | AlexPyriel |
