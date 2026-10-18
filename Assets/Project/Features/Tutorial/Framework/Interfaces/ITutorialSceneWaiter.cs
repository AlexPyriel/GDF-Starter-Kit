using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Scene wait across a transition. Safe to scene changes: listens to the root-level active-scene
    /// signal (<see cref="IActiveSceneScopeHolder"/>) and holds no references to unloading-scene
    /// objects. The runner calls <see cref="WaitForScene"/> before starting a step so that the step
    /// begins in the required scene.
    /// </summary>
    public interface ITutorialSceneWaiter
    {
        /// <summary>Whether the given scene is currently active.</summary>
        /// <param name="scene">Scene to check.</param>
        /// <returns><see langword="true"/> if this is the current active scene.</returns>
        public bool IsActive(ESceneType scene);

        /// <summary>
        /// Waits until the given scene becomes active. If it is already active, completes immediately.
        /// </summary>
        /// <param name="scene">Scene to wait for.</param>
        /// <param name="token">Cancellation token for the wait.</param>
        /// <returns>Task that completes when the scene is activated.</returns>
        public UniTask WaitForScene(ESceneType scene, CancellationToken token);
    }
}
