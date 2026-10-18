using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Progress;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Runs tutorial steps by index from the resume cursor: for each step it waits for the required
    /// scene, injects the step with the active-scene resolver, runs under a per-step token (step
    /// cancellation does not kill the tutorial) and advances the cursor on a checkpoint. A scope-level
    /// cancellation kills the whole run. The runner holds no run state (only a reference to the active
    /// per-step CTS for <see cref="CancelCurrentStep"/>) — exactly one active run is enforced by the service.
    /// </summary>
    public sealed class TutorialStepRunner
    {
        private readonly ITutorialSceneWaiter _sceneWaiter;
        private readonly IActiveSceneScopeHolder _sceneScopeHolder;
        private readonly ITutorialProgressService _progress;

        private CancellationTokenSource _currentStepCts;

        /// <summary>Creates the runner.</summary>
        /// <param name="sceneWaiter">Pre-step scene wait.</param>
        /// <param name="sceneScopeHolder">Active-scene resolver source for step injection.</param>
        /// <param name="progress">Progress: resume cursor and completion flag.</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialStepRunner(
            ITutorialSceneWaiter sceneWaiter,
            IActiveSceneScopeHolder sceneScopeHolder,
            ITutorialProgressService progress)
        {
            _sceneWaiter = sceneWaiter ?? throw new ArgumentNullException(nameof(sceneWaiter));
            _sceneScopeHolder = sceneScopeHolder ?? throw new ArgumentNullException(nameof(sceneScopeHolder));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        /// <summary>
        /// A working entry begins execution: the required scene is active and the step is injected.
        /// Arguments are the entry index in the list and the step prototype (asset, not its clone).
        /// Disabled entries and entries before the cursor do not raise this event. Consumed by funnel
        /// analytics.
        /// </summary>
        public event Action<int, ITutorialStep> StepStarted;

        /// <summary>All entries have been executed; the tutorial is marked completed. Consumed by funnel analytics.</summary>
        public event Action Completed;

        /// <summary>
        /// Runs tutorial steps starting from the saved cursor. Skips disabled entries and entries
        /// already passed (index less than cursor). For each working step: waits for
        /// <see cref="ITutorialStep.RequiredScene"/>, injects with the active-scene resolver, runs
        /// under a per-step token, guarantees <see cref="IDisposable.Dispose"/>, and on a successful
        /// finish of a step with <see cref="ITutorialStepEntry.ShouldSaveProgress"/> advances the
        /// cursor to the next index. After all steps the tutorial is marked completed.
        /// </summary>
        /// <param name="tutorialId">Tutorial identifier (cursor and completion key).</param>
        /// <param name="steps">Ordered tutorial entries (step + positional skip/checkpoint flags).</param>
        /// <param name="loopToken">Scope token: its cancellation kills the whole run.</param>
        /// <returns>Task that completes when the run finishes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tutorialId"/> or <paramref name="steps"/> is <see langword="null"/>.</exception>
        public async UniTask RunAsync(string tutorialId, IReadOnlyList<ITutorialStepEntry> steps, CancellationToken loopToken)
        {
            if (tutorialId == null)
                throw new ArgumentNullException(nameof(tutorialId));
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            int cursor = _progress.GetCursor(tutorialId);

            for (int i = 0; i < steps.Count; i++)
            {
                loopToken.ThrowIfCancellationRequested();

                ITutorialStepEntry entry = steps[i];
                if (entry == null || entry.Step == null || entry.IsDisabled || i < cursor)
                {
                    continue;
                }

                ITutorialStep proto = entry.Step;

                // Wait for the required scene BEFORE injection: only then does the active scene match
                // the required one and the resolver carries its live managers (otherwise we inject with
                // the wrong scope).
                await _sceneWaiter.WaitForScene(proto.RequiredScene, loopToken);

                // Per-run clone: execute/inject/dispose the clone, the asset prototype stays untouched.
                // Clone only steps that actually run (skipped entries are not cloned — no extra instances).
                ITutorialStep step = proto.CreateRuntimeInstance();
                InjectStep(step);

                using (CancellationTokenSource stepCts = CancellationTokenSource.CreateLinkedTokenSource(loopToken))
                {
                    _currentStepCts = stepCts;
                    try
                    {
                        StepStarted?.Invoke(i, proto);
                        await step.Execute(stepCts.Token);

                        // Checkpoint — only on SUCCESSFUL finish: cursor = next index, so resume does not
                        // replay the checkpoint step itself. The flag is positional (from the list entry).
                        if (entry.ShouldSaveProgress)
                        {
                            _progress.SetCursor(tutorialId, i + 1);
                        }
                    }
                    catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !loopToken.IsCancellationRequested)
                    {
                        // Only the current step was cancelled (CancelCurrentStep) — move to the next, tutorial lives.
                        // An unfinished step is not checkpointed.
                    }
                    finally
                    {
                        _currentStepCts = null;
                        step.Dispose();
                    }
                }
            }

            _progress.MarkCompleted(tutorialId);
            Completed?.Invoke();
        }

        /// <summary>
        /// Cancels the currently executing step without killing the whole tutorial: the runner moves
        /// to the next step. No-op if no step is active.
        /// </summary>
        public void CancelCurrentStep()
        {
            _currentStepCts?.Cancel();
        }

        private void InjectStep(ITutorialStep step)
        {
            IObjectResolver resolver = _sceneScopeHolder.ActiveResolver;
            resolver?.Inject(step);
        }
    }
}
