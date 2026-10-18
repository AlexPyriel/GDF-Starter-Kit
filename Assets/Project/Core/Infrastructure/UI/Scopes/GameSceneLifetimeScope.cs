using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Scenes.Game;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Scopes
{
    /// <summary>
    /// Scene UI scope for the gameplay scene.
    /// </summary>
    public sealed class GameSceneLifetimeScope : SceneUILifetimeScope
    {
        /// <inheritdoc />
        protected override ESceneType SceneType => ESceneType.Game;

        protected override void ConfigureScene(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameScenePresenter>();
        }
    }
}
