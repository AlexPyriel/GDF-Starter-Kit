using VContainer;

namespace Assets.Project.Core.Infrastructure.SceneLoading.Interfaces
{
    /// <summary>
    /// Holds the active scene and its DI resolver: the scene scope calls <see cref="SetActive"/> on load,
    /// and features that need the live scene managers (the tutorial runner injecting a step) read
    /// <see cref="ActiveResolver"/>. State is read with queries; the change itself is published as
    /// <see cref="Messages.SceneActivatedMessage"/>. The scene and resolver are set BEFORE the message,
    /// so subscribers always observe a consistent (scene ↔ resolver) pair.
    /// </summary>
    public interface IActiveSceneScopeHolder
    {
        /// <summary>Active scene, or <see langword="null"/> if none has been registered yet.</summary>
        public ESceneType? ActiveScene { get; }

        /// <summary>DI resolver of the active scene, or <see langword="null"/> if no scene is registered.</summary>
        public IObjectResolver ActiveResolver { get; }

        /// <summary>
        /// Registers the active scene and its resolver (called by the scene scope on load): first sets
        /// the (scene ↔ resolver) pair, then publishes <see cref="Messages.SceneActivatedMessage"/>.
        /// </summary>
        /// <param name="scene">Type of the loaded scene.</param>
        /// <param name="resolver">DI resolver of the scene scope.</param>
        public void SetActive(ESceneType scene, IObjectResolver resolver);
    }
}
