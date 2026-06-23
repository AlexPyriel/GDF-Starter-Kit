namespace Assets.Project.Core.Infrastructure.SceneLoading
{
    /// <summary>
    /// Identifies addressable scenes used by the project scene-loading flow.
    /// </summary>
    public enum ESceneType
    {
        /// <summary>
        /// Bootstrap scene that composes the initial application root.
        /// </summary>
        Bootstrap = 0,

        /// <summary>
        /// Transitional loading scene that presents loading progress.
        /// </summary>
        Loading = 1,

        /// <summary>
        /// Main menu or home scene.
        /// </summary>
        Home = 2,

        /// <summary>
        /// Gameplay scene.
        /// </summary>
        Game = 3,
    }
}
