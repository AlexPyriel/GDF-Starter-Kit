using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Core.Infrastructure.UI.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.Infrastructure.UI.Scopes
{
    /// <summary>
    /// Scene-local UI scope for the starter kit scenes.
    /// Each scene instantiates its own authored SceneUICanvas prefab synchronously inside the scene scope,
    /// while the global root keeps only the global overlay host.
    /// </summary>
    public abstract class SceneUILifetimeScope : LifetimeScope
    {
        [SerializeField]
        private UIRoot _sceneUIRootPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register(CreateSceneUiRoot, Lifetime.Scoped);
            builder.Register<SceneUIService>(Lifetime.Scoped);
            ConfigureScene(builder);
        }

        protected abstract void ConfigureScene(IContainerBuilder builder);

        private UIRoot CreateSceneUiRoot(IObjectResolver resolver)
        {
            if (_sceneUIRootPrefab == null)
                throw new MissingReferenceException($"{nameof(SceneUILifetimeScope)} requires {nameof(_sceneUIRootPrefab)} to be assigned.");

            return resolver.Instantiate(_sceneUIRootPrefab, transform);
        }
    }
}
