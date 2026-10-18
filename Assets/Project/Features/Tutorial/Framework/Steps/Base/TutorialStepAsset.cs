using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace Assets.Project.Features.Tutorial.Framework.Steps.Base
{
    /// <summary>
    /// Base class for a tutorial step: a <see cref="ScriptableObject"/> asset referenced from the
    /// ordered <c>TutorialDefinition.Steps</c>. One step — one file: edits by different authors do
    /// not conflict. Backs the orchestration metadata via <c>[SerializeField]</c> (the "Orchestration"
    /// group) and reduces execution to <see cref="OnExecute"/>. Gives the subclass GRANULAR
    /// mechanisms — a step composes them itself: highlight (<see cref="Highlight"/>), its own panel
    /// (<see cref="SpawnPanel{T}"/>), action waits (<see cref="WaitForAction"/>, <see cref="WaitForButtonClick"/>,
    /// <see cref="WaitForEvent"/>).
    /// <para>
    /// The runner executes a clone (<see cref="CreateRuntimeInstance"/>), not the asset itself —
    /// per-run isolation keeps the project asset untouched. Subscriptions from <see cref="Track{T}"/>
    /// and spawns from <see cref="SpawnPanel{T}"/> are auto-released in <see cref="Dispose"/>, where
    /// the clone also destroys itself.
    /// </para>
    /// </summary>
    public abstract class TutorialStepAsset : ScriptableObject, ITutorialStep
    {
        [Tooltip("Technical description of the step. Not used anywhere.")]
        [SerializeField, TextArea(minLines:2, maxLines:3)] private string _description;

        [Header("Orchestration")]
        [Tooltip("Scene in which the step runs. The runner waits for it before starting the step. " +
                 "Skip/checkpoint flags are positional and live on TutorialStepEntry, not here.")]
        [SerializeField] private ESceneType _requiredScene = ESceneType.Home;

        [Inject] private TutorialContext _context;

        private List<IDisposable> _tracked;
        private List<GameObject> _spawned;

        /// <inheritdoc />
        public ESceneType RequiredScene => _requiredScene;

        /// <summary>Framework context of the step (overlay, scene waiter, progress, highlights, actions).</summary>
        protected TutorialContext Context => _context;

        /// <summary>Tutorial overlay (shortcut to <c>Context.Overlay</c>).</summary>
        protected ITutorialOverlay Overlay => _context.Overlay;

        /// <inheritdoc />
        public ITutorialStep CreateRuntimeInstance()
        {
            return Instantiate(this);
        }

        /// <inheritdoc />
        public UniTask Execute(CancellationToken token)
        {
            return OnExecute(token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_tracked != null)
            {
                for (int i = _tracked.Count - 1; i >= 0; i--)
                {
                    _tracked[i]?.Dispose();
                }

                _tracked.Clear();
            }

            if (_spawned != null)
            {
                for (int i = _spawned.Count - 1; i >= 0; i--)
                {
                    if (_spawned[i] != null)
                    {
                        SafeDestroy(_spawned[i]);
                    }
                }

                _spawned.Clear();
            }

            // Auto-clear highlights: every step clears its cutouts on finish so they do not leak into the
            // next step (the overlay is a singleton and survives scene changes). The next step sets up
            // its own highlights.
            if (_context != null)
            {
                _context.Overlay.ClearHighlight();
            }

            OnDispose();

            // The per-run clone destroys itself — the runner executes the clone, not the asset prototype,
            // so this disposes the transient instance without touching the project asset.
            SafeDestroy(this);
        }

        /// <summary>
        /// Step logic: highlight, show its own panel, wait for an action. Implemented by subclasses.
        /// Completes within <see cref="RequiredScene"/>.
        /// </summary>
        /// <param name="token">Step cancellation token.</param>
        /// <returns>Task that completes when the step finishes.</returns>
        protected abstract UniTask OnExecute(CancellationToken token);

        /// <summary>Resets transient state of the subclass (flags, etc.). Spawns and subscriptions are cleared automatically.</summary>
        protected virtual void OnDispose()
        {
        }

        // Destruction that is safe both at runtime (Destroy) and in edit-mode tests (DestroyImmediate).
        private static void SafeDestroy(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        /// <summary>
        /// Highlights a group by key (a <c>TutorialHighlightGroup</c> component in the scene): shows
        /// the overlay and cuts holes over its targets. No-op if no group is registered under this key.
        /// </summary>
        /// <param name="highlightKey">Highlight group key.</param>
        protected void Highlight(ETutorialHighlightKey highlightKey)
        {
            if (_context.HighlightRegistry.TryGetTargets(highlightKey, out IReadOnlyList<Graphic> targets) && targets.Count > 0)
            {
                Overlay.Show();
                Overlay.Highlight(targets);
            }
        }

        /// <summary>
        /// Highlights targets obtained not by key but from the scene (for example a specific room):
        /// shows the overlay and cuts holes. Empty list — no-op.
        /// </summary>
        /// <param name="targets">Highlight targets.</param>
        protected void Highlight(IReadOnlyList<Graphic> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                Overlay.Show();
                Overlay.Highlight(targets);
            }
        }

        /// <summary>
        /// Highlights arbitrary regions as rectangular cutouts by their <see cref="RectTransform"/>
        /// (without a shaped mask) — for targets without their own graphic (a hand zone), or when
        /// plain rectangular cutouts are preferable.
        /// </summary>
        /// <param name="regions">Rectangular regions for cutouts.</param>
        protected void HighlightRegions(IReadOnlyList<RectTransform> regions)
        {
            if (regions != null && regions.Count > 0)
            {
                Overlay.Show();
                Overlay.Highlight(regions);
            }
        }

        /// <summary>
        /// Waits until a highlight group with the given key appears (scene UI loads async), then
        /// highlights. Useful when the target does not exist immediately after scene activation.
        /// </summary>
        /// <param name="highlightKey">Highlight group key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes after the highlight is applied.</returns>
        protected async UniTask WaitAndHighlight(ETutorialHighlightKey highlightKey, CancellationToken token)
        {
            await UniTask.WaitUntil(
                () => _context.HighlightRegistry.TryGetTargets(highlightKey, out IReadOnlyList<Graphic> targets) && targets.Count > 0,
                PlayerLoopTiming.Update,
                token);
            Highlight(highlightKey);
        }

        /// <summary>
        /// Lifts elements above the dim (without cutting holes) — a "live" highlight for hand cards:
        /// the card renders fully above the dim with its own shape/lift/hover and receives clicks
        /// directly. Shows the overlay. Cleared automatically in <see cref="Dispose"/> (via
        /// <see cref="ClearHighlight"/>). Empty list — no-op.
        /// </summary>
        /// <param name="elements">Elements lifted above the dim.</param>
        protected void RaiseAbove(IReadOnlyList<RectTransform> elements)
        {
            if (elements != null && elements.Count > 0)
            {
                Overlay.Show();
                Overlay.RaiseAbove(elements);
            }
        }

        /// <summary>Clears highlights (cutouts and raised elements); the dim stays.</summary>
        protected void ClearHighlight()
        {
            Overlay.ClearHighlight();
        }

        /// <summary>
        /// Spawns the step's OWN panel above the dim (into <c>Overlay.ContentRoot</c>) and shows the
        /// overlay. The instance is auto-destroyed in <see cref="Dispose"/>. Text/content are set by
        /// the subclass on the component.
        /// </summary>
        /// <typeparam name="T">Panel component type on the prefab.</typeparam>
        /// <param name="prefab">Panel prefab (<c>[SerializeField]</c> on the step).</param>
        /// <returns>Panel instance.</returns>
        protected T SpawnPanel<T>(T prefab) where T : Component
        {
            Overlay.Show();
            T instance = Instantiate(prefab, Overlay.ContentRoot);
            (_spawned ??= new List<GameObject>()).Add(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Registers a disposable for auto-release in <see cref="Dispose"/> (returns the same instance).
        /// </summary>
        /// <typeparam name="T">Disposable type.</typeparam>
        /// <param name="disposable">Resource or subscription.</param>
        /// <returns>The same <paramref name="disposable"/>.</returns>
        protected T Track<T>(T disposable) where T : IDisposable
        {
            (_tracked ??= new List<IDisposable>()).Add(disposable);
            return disposable;
        }

        /// <summary>Waits for a game action (published to the bus by game code) — universal step completion.</summary>
        /// <param name="actionType">Action type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="count">Required number of occurrences (default 1).</param>
        /// <returns>Task that completes when the counter is reached.</returns>
        protected UniTask WaitForAction(ETutorialActionType actionType, CancellationToken token, int count = 1)
        {
            return _context.Actions.WaitForCount(actionType, count, token);
        }

        /// <summary>Waits for a click on the dim (outside cutouts) — "advance" for message steps.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes on a click.</returns>
        protected UniTask WaitForAdvanceClick(CancellationToken token)
        {
            return Overlay.WaitForAdvanceClick(token);
        }

        /// <summary>Waits for <paramref name="seconds"/> seconds (step timer), cancellable.</summary>
        /// <param name="seconds">Delay duration in seconds (negative is treated as 0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes when the delay elapses.</returns>
        protected UniTask Delay(float seconds, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, seconds)), cancellationToken: token);
        }

        /// <summary>Waits for a click on a button (direct reference, e.g. a button on the step's own panel) with guaranteed unsubscribe.</summary>
        /// <param name="button">Button.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes on click.</returns>
        protected async UniTask WaitForButtonClick(Button button, CancellationToken token)
        {
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            UnityAction handler = () => completionSource.TrySetResult();
            button.onClick.AddListener(handler);
            try
            {
                await completionSource.Task.AttachExternalCancellation(token);
            }
            finally
            {
                button.onClick.RemoveListener(handler);
            }
        }

        /// <summary>Generic event wait with guaranteed unsubscribe.</summary>
        /// <param name="subscribe">Handler subscription.</param>
        /// <param name="unsubscribe">Handler unsubscription.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task that completes on the first event.</returns>
        protected async UniTask WaitForEvent(Action<Action> subscribe, Action<Action> unsubscribe, CancellationToken token)
        {
            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            Action handler = () => completionSource.TrySetResult();
            subscribe(handler);
            try
            {
                await completionSource.Task.AttachExternalCancellation(token);
            }
            finally
            {
                unsubscribe(handler);
            }
        }

        /// <summary>Generic event wait with argument; returns the argument of the first invocation.</summary>
        /// <typeparam name="TArg">Argument type.</typeparam>
        /// <param name="subscribe">Handler subscription.</param>
        /// <param name="unsubscribe">Handler unsubscription.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task with the first-event argument.</returns>
        protected async UniTask<TArg> WaitForEvent<TArg>(Action<Action<TArg>> subscribe, Action<Action<TArg>> unsubscribe, CancellationToken token)
        {
            UniTaskCompletionSource<TArg> completionSource = new UniTaskCompletionSource<TArg>();
            Action<TArg> handler = arg => completionSource.TrySetResult(arg);
            subscribe(handler);
            try
            {
                return await completionSource.Task.AttachExternalCancellation(token);
            }
            finally
            {
                unsubscribe(handler);
            }
        }
    }
}
