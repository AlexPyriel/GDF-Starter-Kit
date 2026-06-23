using Assets.Project.Features.Scenes.Loading;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Scopes
{
    /// <summary>
    /// Scene UI scope for the loading scene.
    /// </summary>
    public sealed class LoadingSceneLifetimeScope : SceneUILifetimeScope
    {
        protected override void ConfigureScene(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LoadingScenePresenter>();
        }
    }
}
