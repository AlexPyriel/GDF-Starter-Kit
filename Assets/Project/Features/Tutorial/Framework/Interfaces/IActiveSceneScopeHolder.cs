using System;
using Assets.Project.Core.Infrastructure.SceneLoading;
using VContainer;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Holds the active scene and its DI resolver: the scene scope calls <see cref="SetActive"/>
    /// on load, and the tutorial runner injects a step with the resolver of exactly the active scene
    /// (its live managers). The same call raises <see cref="ActiveSceneChanged"/> — the single signal
    /// source for <see cref="ITutorialSceneWaiter"/>. The scene and resolver are set BEFORE the event,
    /// so subscribers always observe a consistent (scene ↔ resolver) pair — no race by construction.
    /// </summary>
    public interface IActiveSceneScopeHolder
    {
        /// <summary>Active scene, or <see langword="null"/> if none has been registered yet.</summary>
        public ESceneType? ActiveScene { get; }

        /// <summary>DI resolver of the active scene, or <see langword="null"/> if no scene is registered.</summary>
        public IObjectResolver ActiveResolver { get; }

        /// <summary>Raised when the active scene changes; fired AFTER the scene and resolver are set.</summary>
        public event Action<ESceneType> ActiveSceneChanged;

        /// <summary>
        /// Registers the active scene and its resolver (called by the scene scope on load): first sets
        /// the (scene ↔ resolver) pair, then raises <see cref="ActiveSceneChanged"/>.
        /// </summary>
        /// <param name="scene">Type of the loaded scene.</param>
        /// <param name="resolver">DI resolver of the scene scope.</param>
        public void SetActive(ESceneType scene, IObjectResolver resolver);
    }
}
