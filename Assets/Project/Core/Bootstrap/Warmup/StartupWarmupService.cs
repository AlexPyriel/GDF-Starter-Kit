using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Project.Core.Bootstrap.Warmup
{
    /// <summary>
    /// Runs the registered warmup steps sequentially in registration order.
    /// A per-step failure is logged and does not abort the remaining steps
    /// (fail-open): no single step is treated as critical for startup.
    /// </summary>
    public sealed class StartupWarmupService : IStartupWarmupAwaiter
    {
        private readonly IReadOnlyList<IStartupWarmupStep> _steps;
        private UniTaskCompletionSource _completionSource;

        /// <summary>
        /// Creates the warmup service with the registered steps.
        /// </summary>
        /// <param name="steps">Warmup steps in execution order.</param>
        public StartupWarmupService(IEnumerable<IStartupWarmupStep> steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            _steps = steps.ToList();
        }

        /// <summary>Whether warmup has been kicked off through <see cref="RunAsync"/>.</summary>
        public bool IsStarted { get; private set; }

        /// <inheritdoc />
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Starts all warmup steps. Must not be called more than once.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the warmup pass.</param>
        /// <returns>Task that completes after the last step.</returns>
        public async UniTask RunAsync(CancellationToken cancellationToken = default)
        {
            if (IsStarted)
                throw new InvalidOperationException($"{nameof(StartupWarmupService)} has already been started.");

            IsStarted = true;
            _completionSource = new UniTaskCompletionSource();

            try
            {
                foreach (IStartupWarmupStep step in _steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await step.ExecuteAsync(cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[{nameof(StartupWarmupService)}] Step '{step.Name}' failed; continuing with the next step. {exception}");
                    }
                }
            }
            finally
            {
                IsCompleted = true;
                _completionSource.TrySetResult();
            }
        }

        /// <inheritdoc />
        public async UniTask WaitUntilCompletedAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (IsCompleted)
                return;

            if (!IsStarted)
            {
                Debug.LogWarning($"[{nameof(StartupWarmupService)}] Warmup was never started; treating it as completed.");
                return;
            }

            await _completionSource.Task
                .AttachExternalCancellation(cancellationToken)
                .Timeout(timeout, DelayType.Realtime);
        }
    }
}
