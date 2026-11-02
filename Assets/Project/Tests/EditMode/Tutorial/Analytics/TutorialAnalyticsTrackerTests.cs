using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GDF.Analytics;
using Assets.Project.Features.Tutorial.Analytics;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using Assets.Project.Features.Tutorial.Framework.Steps.Base;
using Assets.Project.Tests.EditMode.Tutorial.Framework;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Analytics
{
    /// <summary>
    /// Exercises <see cref="TutorialAnalyticsTracker"/>: each working-entry start and run completion
    /// are reported through <see cref="IAnalyticsService"/> as design events with the step index and
    /// name; asset-step name comes from the asset name; after Dispose no runner events reach analytics.
    /// </summary>
    public sealed class TutorialAnalyticsTrackerTests
    {
        private const string ID = "test.tutorial";

        private List<string> _log;
        private IObjectResolver _resolver;
        private RecordingAnalyticsService _analytics;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _resolver = new ContainerBuilder().Build();
            _analytics = new RecordingAnalyticsService();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
        }

        [UnityTest]
        public IEnumerator Run_TwoSteps_ReportsEachStepThenCompletion() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            TutorialStepRunner runner = CreateRunner();
            TutorialAnalyticsTracker tracker = new TutorialAnalyticsTracker(runner, _analytics);
            tracker.Initialize();

            // Act
            await runner.RunAsync(ID, Entries(new FakeStep(_log, "a"), new FakeStep(_log, "b")), CancellationToken.None);

            // Assert: a non-asset step is named by its type.
            CollectionAssert.AreEqual(
                new[] { "tutorial:step:00_FakeStep", "tutorial:step:01_FakeStep", "tutorial:complete" },
                _analytics.Info);
        });

        [UnityTest]
        public IEnumerator Dispose_Unsubscribes_RunReportsNothing() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            TutorialStepRunner runner = CreateRunner();
            TutorialAnalyticsTracker tracker = new TutorialAnalyticsTracker(runner, _analytics);
            tracker.Initialize();

            // Act
            tracker.Dispose();
            await runner.RunAsync(ID, Entries(new FakeStep(_log, "a")), CancellationToken.None);

            // Assert
            CollectionAssert.IsEmpty(_analytics.Info);
        });

        [Test]
        public void StepName_StepAsset_ReturnsAssetName()
        {
            // Arrange
            NamedStep step = ScriptableObject.CreateInstance<NamedStep>();
            step.name = "7 SampleStepName";

            try
            {
                // Act
                string result = TutorialAnalyticsTracker.StepName(step);

                // Assert
                Assert.AreEqual("7 SampleStepName", result);
            }
            finally
            {
                Object.DestroyImmediate(step);
            }
        }

        [Test]
        public void StepName_Null_ReturnsNull()
        {
            Assert.IsNull(TutorialAnalyticsTracker.StepName(null));
        }

        private TutorialStepRunner CreateRunner()
        {
            return new TutorialStepRunner(
                new FakeSceneWaiter(_log),
                new FakeSceneScopeHolder(_log, _resolver),
                new FakeProgress(_log));
        }

        private static List<ITutorialStepEntry> Entries(params ITutorialStep[] steps)
        {
            List<ITutorialStepEntry> entries = new List<ITutorialStepEntry>();
            foreach (ITutorialStep step in steps)
                entries.Add(new FakeStepEntry(step, false, false));

            return entries;
        }

        /// <summary>Minimal step asset — only to obtain an asset name.</summary>
        private sealed class NamedStep : TutorialStepAsset
        {
            protected override UniTask OnExecute(CancellationToken token) => UniTask.CompletedTask;
        }
    }
}
