using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Core.Bootstrap.Warmup
{
    /// <summary>
    /// Observation point for startup warmup for consumers that only need to know
    /// when it has finished (for example, a home-scene opening sequence that must
    /// not reveal interactive UI before background work is ready).
    /// </summary>
    public interface IStartupWarmupAwaiter
    {
        /// <summary>Whether warmup has finished (successfully, with per-step errors, or by cancellation).</summary>
        public bool IsCompleted { get; }

        /// <summary>
        /// Waits for warmup to complete. Completes immediately if warmup was never started.
        /// </summary>
        /// <param name="timeout">Maximum wait duration.</param>
        /// <param name="cancellationToken">Cancellation token for the wait.</param>
        /// <returns>Task that completes once warmup is finished; throws <see cref="TimeoutException"/> on timeout.</returns>
        public UniTask WaitUntilCompletedAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
