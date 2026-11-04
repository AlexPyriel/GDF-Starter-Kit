using System;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Tutorial.Framework.Config;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Progress;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Orchestrator of ONE tutorial (MVP). Holds one <see cref="TutorialDefinition"/>; on active-scene
    /// change tries to start (if not completed and not already running) and delegates execution to
    /// <see cref="TutorialStepRunner"/>. Exactly one active run at a time; the scene gate is handled by
    /// the runner itself (<c>WaitForScene</c> per step), so the service does not pre-check the scene.
    /// Public controls: <see cref="TryStart"/>, <see cref="Skip"/>, <see cref="Reset"/>,
    /// <see cref="Restart"/>, <see cref="CancelCurrentStep"/>. Multi-tutorial catalog/queue is deferred.
    /// </summary>
    public sealed class TutorialService : IInitializable, IDisposable
    {
        private readonly ITutorialDefinition _definition;
        private readonly TutorialStepRunner _runner;
        private readonly ITutorialProgressService _progress;
        private readonly IActiveSceneScopeHolder _sceneScopeHolder;

        private CancellationTokenSource _loopCts;

        /// <summary>Creates the service.</summary>
        /// <param name="definition">Single tutorial definition (injected).</param>
        /// <param name="runner">Step runner.</param>
        /// <param name="progress">Progress: completion flag and cursor.</param>
        /// <param name="sceneScopeHolder">Active-scene change signal (auto-start).</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialService(
            ITutorialDefinition definition,
            TutorialStepRunner runner,
            ITutorialProgressService progress,
            IActiveSceneScopeHolder sceneScopeHolder)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _sceneScopeHolder = sceneScopeHolder ?? throw new ArgumentNullException(nameof(sceneScopeHolder));
        }

        /// <summary>
        /// Whether the tutorial is completed. In the editor — the <see cref="TutorialDefinition.SkipTutorial"/>
        /// flag (no write to progress) OR the saved completion flag. In a release build the flag is
        /// ignored — only the completion flag matters: the tutorial always runs until completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
#if UNITY_EDITOR
                return _definition.SkipTutorial || _progress.IsCompleted(_definition.TutorialId);
#else
                return _progress.IsCompleted(_definition.TutorialId);
#endif
            }
        }

        /// <summary>Whether a tutorial run is currently in progress.</summary>
        public bool IsRunning => _loopCts != null;

        /// <inheritdoc />
        public void Initialize()
        {
            // Auto-start on active-scene change: the runner itself waits for the current step's required scene.
            _sceneScopeHolder.ActiveSceneChanged += OnActiveSceneChanged;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _sceneScopeHolder.ActiveSceneChanged -= OnActiveSceneChanged;
            CancelRun();
        }

        /// <summary>
        /// Starts a run if the tutorial is NOT completed (including computed skip) and no run is
        /// in progress. No-op otherwise. Single entry point: scene trigger and explicit call converge here.
        /// </summary>
        public void TryStart()
        {
            if (IsCompleted || IsRunning)
            {
                return;
            }

            _loopCts = new CancellationTokenSource();
            RunGuarded(_loopCts).Forget();
        }

        /// <summary>
        /// Runtime skip (a pre-start skip window): cancels the current run (if any) and marks the
        /// tutorial as completed (persisted).
        /// </summary>
        public void Skip()
        {
            CancelRun();
            _progress.MarkCompleted(_definition.TutorialId);
        }

        /// <summary>
        /// Resets the tutorial (debug panel "replay"): cancels the current run and clears completion
        /// flag + cursor.
        /// </summary>
        public void Reset()
        {
            CancelRun();
            _progress.Reset(_definition.TutorialId);
        }

        /// <summary>Restart the tutorial now: <see cref="Reset"/> + <see cref="TryStart"/>.</summary>
        public void Restart()
        {
            Reset();
            TryStart();
        }

        /// <summary>
        /// Cancels the currently executing step without killing the tutorial: the runner moves on to
        /// the next step.
        /// </summary>
        public void CancelCurrentStep()
        {
            _runner.CancelCurrentStep();
        }

        private void OnActiveSceneChanged(ESceneType scene)
        {
            TryStart();
        }

        private async UniTaskVoid RunGuarded(CancellationTokenSource cts)
        {
            try
            {
                await _runner.RunAsync(_definition.TutorialId, _definition.Steps, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Run cancelled (Skip/Reset/scope cancel) — expected.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                // Clear only "our own" CTS: the cancel path may have replaced or cleared it already.
                if (ReferenceEquals(_loopCts, cts))
                {
                    _loopCts = null;
                }

                cts.Dispose();
            }
        }

        private void CancelRun()
        {
            CancellationTokenSource cts = _loopCts;
            if (cts == null)
            {
                return;
            }

            // Null BEFORE Cancel: IsRunning turns false immediately -> Restart/TryStart are correct;
            // RunGuarded.finally will see the mismatch and not touch the already-cancelled CTS again.
            _loopCts = null;
            cts.Cancel();
        }
    }
}
