# ADR-0001: Event channels — R3, `event Action`, MessagePipe

## Status

Accepted

## Date

2026-11-04

## Last Verified

2026-11-04

## Decision Makers

Alex Boyarintsev (kit owner), Claude Code (analysis)

## Summary

The kit had three ways to signal events (R3 streams, C# `event Action` on service interfaces, and a
hand-rolled `TutorialActionBus`) with no rule on which to use. MessagePipe (Cysharp) is adopted for
cross-feature domain events; R3 and `event Action` keep their roles, and the tutorial waits on typed
MessagePipe messages instead of a central action enum.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3) |
| **Domain** | Core / Scripting |
| **Knowledge Risk** | LOW — MessagePipe 1.8.2 and VContainer 1.17 APIs verified against package sources |
| **References Consulted** | `Library/PackageCache/com.cysharp.messagepipe*` sources, MessagePipe README |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None beyond the EditMode suite (`TutorialMessageWaiterTests`) |

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

- GDF packages stay DI- and messaging-neutral: a package consumer must not be forced to install MessagePipe.
- VContainer is the only DI container; UniTask is the async primitive.
- The kit is a GitHub template: clones are independent copies, so a breaking change only affects clones that port it manually.

## Decision

Use each mechanism for one job:

| Mechanism | Use for | Example |
|-----------|---------|---------|
| **R3** | UI streams and state that need operators or `ReactiveProperty` | `view.ButtonClicked.ThrottleFirst(...)` |
| **`event Action` on an interface** | A service notifying about its own state, when the consumer already depends on that service | `ICurrencyService.CurrencyChanged` |
| **MessagePipe** | Cross-feature domain events where the publisher must not know its consumers, or one fact has several independent consumers | gameplay publishes `CardTapped`; tutorial, analytics and audio subscribe |

MessagePipe is registered once with `builder.RegisterMessagePipe()` in `RootLifetimeScope`. With
VContainer ≥ 1.14 `IPublisher<T>` / `ISubscriber<T>` resolve as open generics, so message types need
no registration. Brokers are Singleton (the default), so scene scopes and the root scope share one
channel per message type.

Exception semantics are the MessagePipe default and match C# events: an exception in a subscriber
propagates to `Publish` and stops the remaining subscribers. No global filters are configured.

### Key Interfaces

```csharp
// Game code (clone): declares and publishes its own messages.
public readonly struct CardTapped { public CardTapped(int cardId) { CardId = cardId; } public int CardId { get; } }
_cardTapped.Publish(new CardTapped(id));            // IPublisher<CardTapped> injected

// Tutorial step: waits for them; counting starts at the call.
await WaitFor<CardTapped>(token);
await WaitFor<CardTapped>(m => m.CardId == 3, token, count: 2);
```

### Implementation Guidelines

- Declare message types in the feature that publishes them, one type per file, as `readonly struct`.
- Hold subscriptions as `IDisposable` and release them with the owner (`CompositeDisposable`, `Track` in tutorial steps).
- Do not add MessagePipe to GDF packages. To expose a package event on the bus, write a bridge in the clone when a real consumer appears:

```csharp
public sealed class CurrencyChangedBridge : IStartable, IDisposable
{
    private readonly ICurrencyService _currency;
    private readonly IPublisher<CurrencyChangedMessage> _publisher;

    public CurrencyChangedBridge(ICurrencyService currency, IPublisher<CurrencyChangedMessage> publisher)
    {
        _currency = currency;
        _publisher = publisher;
    }

    public void Start() => _currency.CurrencyChanged += OnChanged;
    public void Dispose() => _currency.CurrencyChanged -= OnChanged;
    private void OnChanged(string id) => _publisher.Publish(new CurrencyChangedMessage(id));
}
```

## Alternatives Considered

### Alternative 1: Keep `TutorialActionBus`

Rejected: gameplay keeps depending on the tutorial, and the central enum must change for every game action.

### Alternative 2: Everything on MessagePipe

Rejected: service events lose their explicit interface contract without decoupling anything (the
consumer already depends on the service), and packages would force MessagePipe on every consumer.

### Alternative 3: R3 `Subject` as the bus

Rejected: needs a hand-written shared registry of subjects; MessagePipe gives typed brokers through DI with scope handling.

## Consequences

### Positive

- Gameplay is unaware of the tutorial; new actions need no framework edits.
- One registration line; any `TMessage` works without setup.
- The same messages are available to analytics, audio and quests in clones.

### Negative

- One more dependency (`com.cysharp.messagepipe`, `com.cysharp.messagepipe.vcontainer`).
- Publishers and subscribers are linked by type only; finding consumers means searching for `ISubscriber<TMessage>`.

### Neutral

- Tutorial waits count from the call, not from the run start (the old bus counted per run).

## Risks

- Developers mixing mechanisms ad hoc. Mitigation: the table above, referenced from `technical-preferences.md`.

## Performance Implications

Negligible at gameplay event rates; struct messages publish without allocation.

## Migration Plan

See `CHANGELOG.md` → 1.3.0 → Breaking.

## Validation Criteria

- `TutorialMessageWaiterTests` and `TutorialStepBaseTests` pass.
- Bootstrap reaches Home in Play Mode with no console errors.

## GDD Requirements Addressed

None — kit infrastructure.

## Related

- `Assets/Project/Features/Tutorial/Framework/Services/TutorialMessageWaiter.cs`
- `Assets/Project/Core/DI/RootLifetimeScope.cs`
