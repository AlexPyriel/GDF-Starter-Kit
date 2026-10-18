using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Tutorial action bus: counts action publications and lets steps wait on them. Counts actions
    /// ONLY while a tutorial is in progress (<see cref="ITutorialStateProvider"/>), so counters do not
    /// accumulate outside a run. Thread-safe (lock on counters).
    /// </summary>
    public sealed class TutorialActionBus : ITutorialActionBus
    {
        private readonly object _sync = new object();
        private readonly Dictionary<ETutorialActionType, int> _counters = new Dictionary<ETutorialActionType, int>();
        private readonly ITutorialStateProvider _stateProvider;

        private event Action<ETutorialActionType, int> ActionPublished;

        /// <summary>Creates the action bus.</summary>
        /// <param name="stateProvider">"Is a tutorial running" provider — the publish gate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stateProvider"/> is <see langword="null"/>.</exception>
        public TutorialActionBus(ITutorialStateProvider stateProvider)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        /// <inheritdoc />
        public void ResetCounters()
        {
            lock (_sync)
            {
                _counters.Clear();
            }
        }

        /// <inheritdoc />
        public int GetCount(ETutorialActionType actionType)
        {
            lock (_sync)
            {
                return _counters.TryGetValue(actionType, out int value) ? value : 0;
            }
        }

        /// <inheritdoc />
        public void Publish(ETutorialActionType actionType)
        {
            if (actionType == ETutorialActionType.None || !_stateProvider.IsTutorialInProgress)
            {
                return;
            }

            int updatedCount;
            lock (_sync)
            {
                updatedCount = (_counters.TryGetValue(actionType, out int value) ? value : 0) + 1;
                _counters[actionType] = updatedCount;
            }

            ActionPublished?.Invoke(actionType, updatedCount);
        }

        /// <inheritdoc />
        public async UniTask WaitForCount(ETutorialActionType actionType, int targetCount, CancellationToken token)
        {
            if (actionType == ETutorialActionType.None || targetCount <= 0 || GetCount(actionType) >= targetCount)
            {
                return;
            }

            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();

            void OnActionPublished(ETutorialActionType publishedType, int count)
            {
                if (publishedType == actionType && count >= targetCount)
                {
                    completionSource.TrySetResult();
                }
            }

            ActionPublished += OnActionPublished;
            try
            {
                // Re-check after subscribing: the counter may have reached the target between GetCount and Subscribe.
                if (GetCount(actionType) >= targetCount)
                {
                    return;
                }

                await completionSource.Task.AttachExternalCancellation(token);
            }
            finally
            {
                ActionPublished -= OnActionPublished;
            }
        }
    }
}
