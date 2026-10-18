using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>Publishes game actions for tutorial steps (called by game code at the right moments).</summary>
    public interface ITutorialActionPublisher
    {
        /// <summary>Publishes an action to the bus (only counted while a tutorial is active).</summary>
        /// <param name="actionType">Action type.</param>
        public void Publish(ETutorialActionType actionType);
    }

    /// <summary>Tracks action counters and lets a step wait on them.</summary>
    public interface ITutorialActionTracker
    {
        /// <summary>Current number of actions of the given type published during the current run.</summary>
        /// <param name="actionType">Action type.</param>
        /// <returns>Action counter value.</returns>
        public int GetCount(ETutorialActionType actionType);

        /// <summary>Waits until the counter for an action reaches <paramref name="targetCount"/>.</summary>
        /// <param name="actionType">Action type.</param>
        /// <param name="targetCount">Required number of publications.</param>
        /// <param name="token">Cancellation token for the wait.</param>
        /// <returns>Task that completes when the counter is reached.</returns>
        public UniTask WaitForCount(ETutorialActionType actionType, int targetCount, CancellationToken token);
    }

    /// <summary>Unified tutorial action bus: publishing (game) + waiting (steps).</summary>
    public interface ITutorialActionBus : ITutorialActionPublisher, ITutorialActionTracker
    {
        /// <summary>
        /// Resets all counters (called by the service when a run starts, so a resume/restart does not
        /// pick up stale counts).
        /// </summary>
        public void ResetCounters();
    }
}
