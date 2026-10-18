using System;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Contract of a single tutorial step — a scene-atomic unit of teaching: a step lives and
    /// completes inside a single scene, scene transitions are boundaries BETWEEN steps. Carries
    /// BEHAVIOR and the intrinsic metadata <see cref="RequiredScene"/>; execution is
    /// <see cref="Execute"/>. Positional orchestration (skip/checkpoint) is factored out into
    /// <see cref="ITutorialStepEntry"/> — set in the tutorial list next to the step reference.
    /// Concrete steps are <c>ScriptableObject</c> assets (<c>TutorialStepAsset</c>).
    /// </summary>
    public interface ITutorialStep : IDisposable
    {
        /// <summary>Scene in which the step must execute; the runner waits for it (<c>WaitForScene</c>) before start.</summary>
        public ESceneType RequiredScene { get; }

        /// <summary>
        /// Creates a runtime instance for a single run: an SO step returns a clone
        /// (<c>Instantiate</c>) — per-run isolation, the project asset stays untouched; transient
        /// or test steps may return themselves. The runner injects, executes, and disposes exactly
        /// this instance (not the asset prototype).
        /// </summary>
        /// <returns>Step instance to execute.</returns>
        public ITutorialStep CreateRuntimeInstance();

        /// <summary>Executes the step; completes within <see cref="RequiredScene"/>.</summary>
        /// <param name="token">Cancellation token (linked with the tutorial-wide token — step cancellation does not kill the tutorial).</param>
        /// <returns>Task that completes when the step finishes.</returns>
        public UniTask Execute(CancellationToken token);
    }
}
