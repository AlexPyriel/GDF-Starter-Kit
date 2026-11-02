using System;
using GDF.Analytics;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using VContainer.Unity;

namespace Assets.Project.Features.Tutorial.Analytics
{
    /// <summary>
    /// Tutorial steps -> analytics funnel: each working-entry start emits
    /// <c>tutorial:step:&lt;NN&gt;_&lt;step name&gt;</c>, run completion emits <c>tutorial:complete</c>.
    /// Subscribes to <see cref="TutorialStepRunner"/> events (the runner is a root-scope singleton,
    /// so the tracker also lives there) and reports through <see cref="IAnalyticsService"/> from the
    /// <c>com.gdf.analytics</c> package. The starter kit's default binding is
    /// <see cref="DebugAnalyticsBackend"/> — swap for a vendor adapter to receive the events.
    /// </summary>
    public sealed class TutorialAnalyticsTracker : IInitializable, IDisposable
    {
        private const string EVENT_PREFIX_STEP = "tutorial:step:";
        private const string EVENT_COMPLETED = "tutorial:complete";

        private readonly TutorialStepRunner _runner;
        private readonly IAnalyticsService _analytics;

        /// <summary>Creates the tracker over the tutorial runner.</summary>
        /// <param name="runner">Main tutorial step runner.</param>
        /// <param name="analytics">Analytics service to report events to.</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialAnalyticsTracker(TutorialStepRunner runner, IAnalyticsService analytics)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <summary>Subscribes to runner events.</summary>
        public void Initialize()
        {
            _runner.StepStarted += OnStepStarted;
            _runner.Completed += OnCompleted;
        }

        /// <summary>Unsubscribes from runner events.</summary>
        public void Dispose()
        {
            _runner.StepStarted -= OnStepStarted;
            _runner.Completed -= OnCompleted;
        }

        /// <summary>
        /// Step name used in the event id: asset name of the step (how steps are named in the project);
        /// for a non-asset step — its type name.
        /// </summary>
        /// <param name="step">Step prototype from the tutorial list.</param>
        /// <returns>Step name; <see langword="null"/> if there is no step.</returns>
        internal static string StepName(ITutorialStep step)
        {
            if (step is UnityEngine.Object asset)
                return asset != null ? asset.name : null;

            return step?.GetType().Name;
        }

        private void OnStepStarted(int stepIndex, ITutorialStep step)
        {
            string name = StepName(step) ?? "unknown";
            _analytics.TrackInfo($"{EVENT_PREFIX_STEP}{stepIndex:00}_{name}");
        }

        private void OnCompleted()
        {
            _analytics.TrackInfo(EVENT_COMPLETED);
        }
    }
}
