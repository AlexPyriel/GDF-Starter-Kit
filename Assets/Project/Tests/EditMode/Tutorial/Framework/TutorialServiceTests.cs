using System;
using System.Collections.Generic;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="TutorialService"/> on top of the real <see cref="TutorialStepRunner"/>
    /// with fakes: computed skip without a write, "single active run" gate, auto-start on scene
    /// change, and the Skip/Reset/Restart/CancelCurrentStep hooks. Fakes are synchronous, so a
    /// non-blocking run completes synchronously inside <see cref="TutorialService.TryStart"/>.
    /// </summary>
    public sealed class TutorialServiceTests
    {
        private const string ID = "test.tutorial";

        private List<string> _log;
        private IObjectResolver _resolver;
        private FakeProgress _progress;
        private FakeSceneScopeHolder _holder;
        private IPublisher<SceneActivatedMessage> _sceneActivatedPublisher;
        private ISubscriber<SceneActivatedMessage> _sceneActivated;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterMessagePipe();
            _resolver = builder.Build();
            _sceneActivatedPublisher = _resolver.Resolve<IPublisher<SceneActivatedMessage>>();
            _sceneActivated = _resolver.Resolve<ISubscriber<SceneActivatedMessage>>();
            _progress = new FakeProgress(_log);
            _holder = new FakeSceneScopeHolder(_log, _resolver);
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
        }

        private TutorialStepRunner CreateRunner()
        {
            return new TutorialStepRunner(new FakeSceneWaiter(_log), _holder, _progress);
        }

        private TutorialService CreateService(ITutorialDefinition definition)
        {
            return new TutorialService(definition, CreateRunner(), _progress, _sceneActivated);
        }

        private ITutorialDefinition MakeDefinition(bool skip, params ITutorialStep[] steps)
        {
            List<ITutorialStepEntry> entries = new List<ITutorialStepEntry>();
            foreach (ITutorialStep step in steps)
            {
                entries.Add(new FakeStepEntry(step));
            }

            return new FakeTutorialDefinition(ID, skip, entries);
        }

        [Test]
        public void Ctor_NullDependency_Throws()
        {
            ITutorialDefinition definition = MakeDefinition(false);
            TutorialStepRunner runner = CreateRunner();

            Assert.Throws<ArgumentNullException>(() => new TutorialService(null, runner, _progress, _sceneActivated));
            Assert.Throws<ArgumentNullException>(() => new TutorialService(definition, null, _progress, _sceneActivated));
            Assert.Throws<ArgumentNullException>(() => new TutorialService(definition, runner, null, _sceneActivated));
            Assert.Throws<ArgumentNullException>(() => new TutorialService(definition, runner, _progress, null));
        }

        [Test]
        public void TryStart_Runs_ExecutesStepsAndMarksCompleted()
        {
            FakeStep a = new FakeStep(_log, "a");
            FakeStep b = new FakeStep(_log, "b");
            TutorialService service = CreateService(MakeDefinition(false, a, b));

            service.TryStart();

            Assert.AreEqual(1, a.ExecuteCount);
            Assert.AreEqual(1, b.ExecuteCount);
            Assert.IsFalse(service.IsRunning);
            Assert.IsTrue(service.IsCompleted);
            CollectionAssert.Contains(_log, "complete:" + ID);
        }

        [Test]
        public void TryStart_SkipTutorialConfig_DoesNotRun_ComputedCompletedWithoutWrite()
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialService service = CreateService(MakeDefinition(true, a));

            service.TryStart();

            Assert.AreEqual(0, a.ExecuteCount, "With skip the tutorial does not start.");
            Assert.IsTrue(service.IsCompleted, "IsCompleted is computed from the flag.");
            Assert.IsFalse(_progress.IsCompleted(ID), "The flag must NOT write to progress.");
        }

        [Test]
        public void TryStart_AlreadyCompleted_DoesNotRun()
        {
            _progress.MarkCompleted(ID);
            FakeStep a = new FakeStep(_log, "a");
            TutorialService service = CreateService(MakeDefinition(false, a));

            service.TryStart();

            Assert.AreEqual(0, a.ExecuteCount);
        }

        [Test]
        public void TryStart_WhileRunning_DoesNotStartSecondRun()
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            TutorialService service = CreateService(MakeDefinition(false, hanging));

            service.TryStart();
            Assert.IsTrue(service.IsRunning);
            Assert.AreEqual(1, hanging.ExecuteCount);

            service.TryStart(); // must be no-op
            Assert.AreEqual(1, hanging.ExecuteCount, "A second TryStart must not start a parallel run.");

            gate.TrySetResult();
            Assert.IsFalse(service.IsRunning);
        }

        [Test]
        public void Skip_CancelsRun_AndMarksCompleted()
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            TutorialService service = CreateService(MakeDefinition(false, hanging));

            service.TryStart();
            Assert.IsTrue(service.IsRunning);

            service.Skip();

            Assert.IsFalse(service.IsRunning);
            Assert.IsTrue(service.IsCompleted);
            Assert.AreEqual(1, hanging.DisposeCount, "Aborted step is disposed.");
        }

        [Test]
        public void Reset_CancelsRun_AndClearsProgress()
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            TutorialService service = CreateService(MakeDefinition(false, hanging));

            service.TryStart();
            service.Reset();

            Assert.IsFalse(service.IsRunning);
            Assert.IsFalse(service.IsCompleted, "After Reset the tutorial is not completed.");
            Assert.AreEqual(1, hanging.DisposeCount);
        }

        [Test]
        public void Restart_RunsAgainFromStart()
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialService service = CreateService(MakeDefinition(false, a));

            service.TryStart();
            Assert.AreEqual(1, a.ExecuteCount);
            Assert.IsTrue(service.IsCompleted);

            service.Restart();

            Assert.AreEqual(2, a.ExecuteCount, "Restart runs the tutorial again from the start.");
            Assert.IsTrue(service.IsCompleted);
        }

        [Test]
        public void CancelCurrentStep_MovesToNextStep_TutorialSurvives()
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            FakeStep next = new FakeStep(_log, "next");
            TutorialService service = CreateService(MakeDefinition(false, hanging, next));

            service.TryStart();
            Assert.AreEqual(1, hanging.ExecuteCount);
            Assert.AreEqual(0, next.ExecuteCount);

            service.CancelCurrentStep();

            Assert.AreEqual(1, next.ExecuteCount, "After step cancellation the tutorial moves on.");
            Assert.IsFalse(service.IsRunning);
            Assert.IsTrue(service.IsCompleted);
        }

        [Test]
        public void Initialize_AutoStartsOnSceneChange()
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialService service = CreateService(MakeDefinition(false, a));
            service.Initialize();

            Assert.AreEqual(0, a.ExecuteCount, "Before the scene signal the tutorial does not start.");

            _sceneActivatedPublisher.Publish(new SceneActivatedMessage(ESceneType.Game));

            Assert.AreEqual(1, a.ExecuteCount, "On scene change the tutorial auto-starts.");
            Assert.IsTrue(service.IsCompleted);

            service.Dispose();
        }

        [Test]
        public void Dispose_UnsubscribesFromSceneChange()
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialService service = CreateService(MakeDefinition(false, a));
            service.Initialize();
            service.Dispose();

            _sceneActivatedPublisher.Publish(new SceneActivatedMessage(ESceneType.Game));

            Assert.AreEqual(0, a.ExecuteCount, "After Dispose the scene signal must not start the tutorial.");
        }
    }
}
