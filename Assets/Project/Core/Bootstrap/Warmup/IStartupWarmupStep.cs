using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Core.Bootstrap.Warmup
{
    /// <summary>
    /// A single startup warmup action executed in parallel with the home scene's
    /// opening animation. Steps are expected to be short, side-effect-free beyond
    /// their stated warmup purpose, and tolerant of being skipped on cancellation.
    /// </summary>
    public interface IStartupWarmupStep
    {
        /// <summary>Human-readable step name used in logs.</summary>
        public string Name { get; }

        /// <summary>
        /// Executes the warmup step.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the warmup pass.</param>
        /// <returns>Task that completes once the step has finished.</returns>
        public UniTask ExecuteAsync(CancellationToken cancellationToken);
    }
}
