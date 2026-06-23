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
        protected override void ConfigureScene(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameScenePresenter>();
        }
    }
}
