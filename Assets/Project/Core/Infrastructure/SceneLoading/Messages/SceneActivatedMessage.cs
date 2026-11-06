namespace Assets.Project.Core.Infrastructure.SceneLoading.Messages
{
    /// <summary>
    /// Published by <see cref="Interfaces.IActiveSceneScopeHolder.SetActive"/> after a scene scope has
    /// loaded and registered itself. By then the holder already exposes the new scene and its resolver,
    /// so subscribers (tutorial, analytics, audio) observe a consistent pair.
    /// </summary>
    public readonly struct SceneActivatedMessage
    {
        /// <summary>Creates the message.</summary>
        /// <param name="sceneType">Type of the scene that became active.</param>
        public SceneActivatedMessage(ESceneType sceneType)
        {
            SceneType = sceneType;
        }

        /// <summary>Type of the scene that became active.</summary>
        public ESceneType SceneType { get; }
    }
}
