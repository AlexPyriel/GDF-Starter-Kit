using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Core.Infrastructure.UI.Services;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
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

        /// <summary>
        /// Scene type announced by this scope on load. Published into
        /// <see cref="IActiveSceneScopeHolder"/> — the single active-scene signal for the tutorial
        /// framework (the scene scope is both the signal source and the holder of the scene DI resolver).
        /// </summary>
        protected abstract ESceneType SceneType { get; }

        protected override void Awake()
        {
            base.Awake();

            // Publish active scene + its resolver: the holder raises the signal in the same call for
            // TutorialSceneWaiter. The resolver is set BEFORE the signal, so the runner injects the step
            // with the correct scene resolver. The holder is a root-scope singleton; in contexts without
            // it (tests, bare scene) this is a no-op.
            if (Container != null && Container.TryResolve(out IActiveSceneScopeHolder sceneScopeHolder))
            {
                sceneScopeHolder.SetActive(SceneType, Container);
            }
        }

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
