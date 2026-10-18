using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Scenes.Home;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Scopes
{
    /// <summary>
    /// Scene UI scope for the home scene.
    /// </summary>
    public sealed class HomeSceneLifetimeScope : SceneUILifetimeScope
    {
        /// <inheritdoc />
        protected override ESceneType SceneType => ESceneType.Home;

        protected override void ConfigureScene(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<HomeScenePresenter>();
        }
    }
}
