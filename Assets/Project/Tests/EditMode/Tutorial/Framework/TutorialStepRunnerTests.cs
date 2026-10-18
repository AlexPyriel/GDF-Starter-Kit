using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Services;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Exercises <see cref="TutorialStepRunner"/>: order by index, skipping disabled entries, starting
    /// from the cursor, checkpoint behavior (i+1 only on success, resume does not replay),
    /// "wait -> inject -> exec" order, guaranteed Dispose, per-step cancellation (tutorial survives)
    /// and scope cancellation (tutorial aborts). Skip/checkpoint are positional flags on the list
    /// entry (<see cref="ITutorialStepEntry"/>), not step fields.
    /// </summary>
    public sealed class TutorialStepRunnerTests
    {
        private const string ID = "test.tutorial";

        private List<string> _log;
        private IObjectResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _resolver = new ContainerBuilder().Build();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
        }

        private TutorialStepRunner CreateRunner(FakeProgress progress)
        {
            return new TutorialStepRunner(
                new FakeSceneWaiter(_log),
                new FakeSceneScopeHolder(_log, _resolver),
                progress);
        }

        private static ITutorialStepEntry Entry(ITutorialStep step, bool isDisabled = false, bool shouldSaveProgress = false)
        {
            return new FakeStepEntry(step, isDisabled, shouldSaveProgress);
        }

        [Test]
        public void Ctor_NullDependency_Throws()
        {
            FakeProgress progress = new FakeProgress(_log);
            Assert.Throws<ArgumentNullException>(() => new TutorialStepRunner(null, new FakeSceneScopeHolder(_log, _resolver), progress));
            Assert.Throws<ArgumentNullException>(() => new TutorialStepRunner(new FakeSceneWaiter(_log), null, progress));
            Assert.Throws<ArgumentNullException>(() => new TutorialStepRunner(new FakeSceneWaiter(_log), new FakeSceneScopeHolder(_log, _resolver), null));
        }

        [UnityTest]
        public IEnumerator Run_ExecutesStepsInOrder_ThenCompletes() => UniTask.ToCoroutine(async () =>
        {
            FakeStep a = new FakeStep(_log, "a");
            FakeStep b = new FakeStep(_log, "b");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a), Entry(b) }, CancellationToken.None);

            CollectionAssert.AreEqual(
                new[] { "wait:Home", "inject-resolver", "exec:a", "dispose:a", "wait:Home", "inject-resolver", "exec:b", "dispose:b", "complete:" + ID },
                _log);
        });

        [UnityTest]
        public IEnumerator Run_SkipsDisabledStep() => UniTask.ToCoroutine(async () =>
        {
            FakeStep disabled = new FakeStep(_log, "off");
            FakeStep on = new FakeStep(_log, "on");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(disabled, isDisabled: true), Entry(on) }, CancellationToken.None);

            Assert.AreEqual(0, disabled.ExecuteCount, "Disabled step must not execute.");
            Assert.AreEqual(0, disabled.DisposeCount, "Disabled step must not be disposed (never ran).");
            Assert.AreEqual(1, on.ExecuteCount);
        });

        [UnityTest]
        public IEnumerator Run_StartsFromCursor_SkipsEarlierSteps() => UniTask.ToCoroutine(async () =>
        {
            FakeStep a = new FakeStep(_log, "a");
            FakeStep b = new FakeStep(_log, "b");
            FakeStep c = new FakeStep(_log, "c");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log, startCursor: 2));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a), Entry(b), Entry(c) }, CancellationToken.None);

            Assert.AreEqual(0, a.ExecuteCount, "Step before cursor is skipped.");
            Assert.AreEqual(0, b.ExecuteCount, "Step before cursor is skipped.");
            Assert.AreEqual(1, c.ExecuteCount, "Step at cursor runs.");
        });

        [UnityTest]
        public IEnumerator Run_Checkpoint_SavesNextIndex_OnlyForCheckpointSteps() => UniTask.ToCoroutine(async () =>
        {
            FakeStep a = new FakeStep(_log, "a");
            FakeStep b = new FakeStep(_log, "b");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a, shouldSaveProgress: false), Entry(b, shouldSaveProgress: true) }, CancellationToken.None);

            // Cursor is written only for b (index 1) -> i+1 = 2. Not for a.
            CollectionAssert.Contains(_log, "cursor:2");
            CollectionAssert.DoesNotContain(_log, "cursor:1");
        });

        [UnityTest]
        public IEnumerator Run_Resume_DoesNotReplayCheckpointStep() => UniTask.ToCoroutine(async () =>
        {
            // A checkpoint at index 0 would have saved cursor 1 -> at startCursor=1 step 0 is not replayed.
            FakeStep checkpoint = new FakeStep(_log, "cp");
            FakeStep next = new FakeStep(_log, "next");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log, startCursor: 1));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(checkpoint, shouldSaveProgress: true), Entry(next) }, CancellationToken.None);

            Assert.AreEqual(0, checkpoint.ExecuteCount, "Checkpoint step is not replayed on resume.");
            Assert.AreEqual(1, next.ExecuteCount);
        });

        [UnityTest]
        public IEnumerator Run_CancelCurrentStep_MovesToNext_TutorialSurvives() => UniTask.ToCoroutine(async () =>
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            FakeStep next = new FakeStep(_log, "next");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));

            UniTask run = runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(hanging, shouldSaveProgress: true), Entry(next) }, CancellationToken.None);

            // Hanging is already running on the gate; cancel only it.
            runner.CancelCurrentStep();

            await run;

            Assert.AreEqual(1, hanging.DisposeCount, "Cancelled step is disposed.");
            Assert.AreEqual(1, next.ExecuteCount, "After step cancellation the tutorial moves on.");
            CollectionAssert.DoesNotContain(_log, "cursor:1", "A cancelled (unfinished) checkpoint step does not move the cursor.");
            CollectionAssert.Contains(_log, "complete:" + ID, "The tutorial reaches completion.");
        });

        [UnityTest]
        public IEnumerator Run_CancelLoopToken_AbortsTutorial() => UniTask.ToCoroutine(async () =>
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            FakeStep next = new FakeStep(_log, "next");
            CancellationTokenSource loopCts = new CancellationTokenSource();
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));

            UniTask run = runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(hanging), Entry(next) }, loopCts.Token);

            loopCts.Cancel();

            try
            {
                await run;
                Assert.Fail("Scope cancellation must abort the run.");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            finally
            {
                loopCts.Dispose();
            }

            Assert.AreEqual(1, hanging.DisposeCount, "Aborted step is still disposed.");
            Assert.AreEqual(0, next.ExecuteCount, "Next step must not run after scope cancellation.");
            CollectionAssert.DoesNotContain(_log, "complete:" + ID, "Aborted tutorial is not marked completed.");
        });

        [UnityTest]
        public IEnumerator Run_StepStarted_RaisedOnlyForExecutedRowsWithRowIndex() => UniTask.ToCoroutine(async () =>
        {
            // Arrange: row 0 is before cursor, row 1 is disabled — only rows 2 and 3 raise the event.
            FakeStep a = new FakeStep(_log, "a");
            FakeStep disabled = new FakeStep(_log, "off");
            FakeStep c = new FakeStep(_log, "c");
            FakeStep d = new FakeStep(_log, "d");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log, startCursor: 1));
            List<string> started = new List<string>();
            runner.StepStarted += (index, step) => started.Add(index + ":" + ReferenceEquals(step, index == 2 ? c : d));

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a), Entry(disabled, isDisabled: true), Entry(c), Entry(d) }, CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "2:True", "3:True" }, started);
        });

        [UnityTest]
        public IEnumerator Run_StepStarted_RaisedAfterInjectBeforeExecute() => UniTask.ToCoroutine(async () =>
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));
            runner.StepStarted += (index, step) => _log.Add("started:" + index);

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a) }, CancellationToken.None);

            CollectionAssert.AreEqual(
                new[] { "wait:Home", "inject-resolver", "started:0", "exec:a", "dispose:a", "complete:" + ID },
                _log);
        });

        [UnityTest]
        public IEnumerator Run_Completed_RaisedAfterTutorialMarkedCompleted() => UniTask.ToCoroutine(async () =>
        {
            FakeStep a = new FakeStep(_log, "a");
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));
            runner.Completed += () => _log.Add("completed-event");

            await runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(a) }, CancellationToken.None);

            Assert.AreEqual("completed-event", _log[_log.Count - 1]);
            Assert.AreEqual("complete:" + ID, _log[_log.Count - 2]);
        });

        [UnityTest]
        public IEnumerator Run_CancelLoopToken_DoesNotRaiseCompleted() => UniTask.ToCoroutine(async () =>
        {
            UniTaskCompletionSource gate = new UniTaskCompletionSource();
            FakeStep hanging = new FakeStep(_log, "hang", gate: gate);
            CancellationTokenSource loopCts = new CancellationTokenSource();
            TutorialStepRunner runner = CreateRunner(new FakeProgress(_log));
            int completedCount = 0;
            runner.Completed += () => completedCount++;

            UniTask run = runner.RunAsync(ID, new List<ITutorialStepEntry> { Entry(hanging) }, loopCts.Token);

            loopCts.Cancel();
            try
            {
                await run;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            finally
            {
                loopCts.Dispose();
            }

            Assert.AreEqual(0, completedCount, "Aborted tutorial does not announce completion.");
        });
    }
}
