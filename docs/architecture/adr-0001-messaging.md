# ADR-0001: Event channels — R3, `event Action`, MessagePipe

## Status

Accepted

## Date

2026-11-04

## Last Verified

2026-11-06 (amended for kit 1.4.0: GDF packages publish MessagePipe messages natively)

## Decision Makers

Alex Boyarintsev (kit owner), Claude Code (analysis)

## Summary

The kit had three ways to signal events (R3 streams, C# `event Action` on service interfaces, and a
hand-rolled `TutorialActionBus`) with no rule on which to use. MessagePipe (Cysharp) is adopted for
cross-feature domain events, including the facts published by the GDF Currency, Ads and TimerService
packages; R3 and `event Action` keep narrower roles, and the tutorial waits on typed MessagePipe messages
instead of a central action enum. State is read with queries; facts are published as messages.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3) |
| **Domain** | Core / Scripting |
| **Knowledge Risk** | LOW — MessagePipe 1.8.2 and VContainer 1.19 APIs verified against package sources |
| **References Consulted** | `Library/PackageCache/com.cysharp.messagepipe*` sources, MessagePipe README |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None beyond the EditMode suites (kit tutorial tests, package `*ServiceTests`) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | None |
| **Blocks** | None |
| **Ordering Note** | None |

## Context

### Problem Statement

Gameplay code in a clone needs to tell the tutorial (and later analytics, audio, quests) that
something happened — a card was tapped, a fight started. With `TutorialActionBus`, gameplay injected
`ITutorialActionPublisher` and published values of `ETutorialActionType`, so gameplay depended on the
tutorial and every new action meant editing a central framework enum.

### Current State (before v1.3.0)

- R3 `Observable` on views (`ButtonClicked`) — UI streams.
- `event Action` on service interfaces in GDF packages (`CurrencyChanged`, `TimerCompleted`, …).
- `TutorialActionBus` — enum-keyed counters, run-scoped, gated by `ITutorialStateProvider`.

### Constraints

- GDF packages are consumed through the kit, where VContainer, UniTask and MessagePipe are already installed
  and registered. Shipping messages from the packages avoids per-clone bridge adapters.
- VContainer is the only DI container with a bundled installer; other containers bind the public `sealed`
  implementations (recipes in each package README).
- UniTask is the async primitive.
- The kit is a GitHub template: clones are independent copies, so a breaking change only affects clones that port it manually.

## Decision

Use each mechanism for one job:

| Mechanism | Use for | Example |
|-----------|---------|---------|
| **R3** | UI streams and state that need operators or `ReactiveProperty` | `view.ButtonClicked.ThrottleFirst(...)` |
| **`event Action` on an interface** | High-frequency or technical notifications about a service's own state, consumed by code that already depends on it | `ISceneLoadingProgress.ProgressChanged`, `IAudioSettingsService.VolumeChanged`, `ILocalDataStorage.DataSaved` |
| **MessagePipe** | Domain facts: the publisher must not know its consumers, or one fact has several independent consumers | `CurrencyChangedMessage`, `TimerCompletedMessage`, `SceneActivatedMessage`; gameplay publishes `CardTappedMessage`, tutorial, analytics and audio subscribe |

**State via queries, facts via messages.** A service exposes its current state through query members
(`ICurrencyService.Get`, `ITimerService.TryGet`, `IActiveSceneScopeHolder.ActiveScene`). What happened is
published as a message carrying enough data for the common subscriber (`OldValue`/`NewValue`, `Reason`,
`CompletedOffline`), so subscribers rarely need a follow-up query.

MessagePipe is registered once with `builder.RegisterMessagePipe()` in `RootLifetimeScope`. With
VContainer ≥ 1.14 `IPublisher<T>` / `ISubscriber<T>` resolve as open generics, so message types need
no registration. Brokers are Singleton (the default), so scene scopes and the root scope share one
channel per message type.

Exception semantics are the MessagePipe default and match C# events: an exception in a subscriber
propagates to `Publish` and stops the remaining subscribers. No global filters are configured.

### Key Interfaces

```csharp
// Game code (clone): declares and publishes its own messages.
public readonly struct CardTappedMessage { public CardTappedMessage(int cardId) { CardId = cardId; } public int CardId { get; } }
_cardTapped.Publish(new CardTappedMessage(id));     // IPublisher<CardTappedMessage> injected

// Tutorial step: waits for them; counting starts at the call.
await WaitFor<CardTappedMessage>(token);
await WaitFor<CardTappedMessage>(m => m.CardId == 3, token, count: 2);

// Any feature: reacts to package facts.
_currencyChanged.Subscribe(m => _goldLabel.text = m.NewValue.ToString()); // ISubscriber<CurrencyChangedMessage>
```

### Implementation Guidelines

- Name message types `<Subject><PastTenseVerb>Message` (`CurrencyChangedMessage`, `SceneActivatedMessage`),
  declare them as `readonly struct` with get-only properties, one type per file, in the feature or package that
  publishes them (`Messages/` folder, `*.Messages` namespace).
- Publish after the state is updated and persisted, so a subscriber's query sees the new state.
- Publish only real changes (no message when a value is set to itself) unless the message reports an attempt
  outcome (`RewardedAdFinishedMessage` is published for every call).
- Hold subscriptions as `IDisposable` and release them with the owner (`CompositeDisposable`, `Track` in tutorial steps).
- GDF packages reference `MessagePipe` from the runtime asmdef and take `IPublisher<T>` through the
  constructor. Package installers never call `RegisterMessagePipe()`; the host's root scope does it once.
- Packages ship only a VContainer installer. Implementations are `public sealed` with constructors that take
  public types, so Zenject (`MessagePipe.Zenject`) and Reflex (`BuiltinContainerBuilder`) projects bind them
  directly; each package README has the recipe.

## Alternatives Considered

### Alternative 1: Keep `TutorialActionBus`

Rejected: gameplay keeps depending on the tutorial, and the central enum must change for every game action.

### Alternative 2: Everything on MessagePipe

Rejected: progress, volume and storage notifications are technical, high-frequency and consumed by code that
already depends on the service; a message type per notification adds nothing there.

### Alternative 2a: Packages stay on `event Action`, clones write bridges

Superseded in 1.4.0. Every clone would write the same bridge classes, and an `Action<string>` carries too little
data (no old/new value, no reason). The packages are only consumed through the kit, where MessagePipe is already
registered, so publishing natively costs nothing.

### Alternative 3: R3 `Subject` as the bus

Rejected: needs a hand-written shared registry of subjects; MessagePipe gives typed brokers through DI with scope handling.

## Consequences

### Positive

- Gameplay is unaware of the tutorial; new actions need no framework edits.
- One registration line; any `TMessage` works without setup.
- The same messages are available to analytics, audio and quests in clones.
- Package facts (`CurrencyChangedMessage`, `TimerCompletedMessage`, `RewardedAdFinishedMessage`, …) need no
  bridge code in clones.

### Negative

- One more dependency (`com.cysharp.messagepipe`, `com.cysharp.messagepipe.vcontainer`), now required by the
  Currency, Ads and TimerService packages.
- Publishers and subscribers are linked by type only; finding consumers means searching for `ISubscriber<TMessage>`.

### Neutral

- Tutorial waits count from the call, not from the run start (the old bus counted per run).

## Risks

- Developers mixing mechanisms ad hoc. Mitigation: the table above, referenced from `technical-preferences.md`.

## Performance Implications

Negligible at gameplay event rates; struct messages publish without allocation.

## Migration Plan

See `CHANGELOG.md` → 1.3.0 → Breaking and 1.4.0 → Breaking.

## Validation Criteria

- `TutorialMessageWaiterTests`, `TutorialStepBaseTests`, `TutorialSceneWaiterTests` and `TutorialServiceTests` pass.
- `CurrencyServiceTests`, `AdsServiceTests` and `TimerServiceTests` pass on a real broker.
- Bootstrap reaches Home in Play Mode with no console errors.

## GDD Requirements Addressed

None — kit infrastructure.

## Related

- `Assets/Project/Features/Tutorial/Framework/Services/TutorialMessageWaiter.cs`
- `Assets/Project/Core/DI/RootLifetimeScope.cs`
- `Assets/Project/Core/Infrastructure/SceneLoading/Services/ActiveSceneScopeHolder.cs`
- Packages: `com.gdf.currency`, `com.gdf.ads`, `com.gdf.timer-service` (`Runtime/Messages/`)
