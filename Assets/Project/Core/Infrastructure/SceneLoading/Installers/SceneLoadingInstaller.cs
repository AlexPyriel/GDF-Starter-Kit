using Assets.Project.Core.Infrastructure.SceneLoading.Configuration;
using Assets.Project.Core.Infrastructure.SceneLoading.Interfaces;
using Assets.Project.Core.Infrastructure.SceneLoading.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Assets.Project.Core.Infrastructure.UI.Configuration;

namespace Assets.Project.Core.Infrastructure.SceneLoading.Installers
{
    /// <summary>
    /// Registers scene-loading and UI prefab configuration repositories and the active-scene holder
    /// into the root container.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SceneLoadingInstaller), menuName = "Project/" + nameof(SceneLoadingInstaller), order = 0)]
    public class SceneLoadingInstaller : ScriptableObject, IInstaller
    {
        [SerializeField]
        private SceneConfigRepository _sceneConfigRepository;

        [SerializeField]
        private UIScreenPrefabConfigRepository _uiScreenPrefabConfigRepository;

        [SerializeField]
        private UIPanelPrefabConfigRepository _uiPanelPrefabConfigRepository;

        /// <summary>
        /// Registers the scene-loading configuration assets used by runtime bootstrap flows and the
        /// <see cref="IActiveSceneScopeHolder"/> that scene scopes report to on load.
        /// </summary>
        /// <param name="builder">The container builder that receives the configuration instances.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_sceneConfigRepository);
            builder.RegisterInstance(_uiScreenPrefabConfigRepository);
            builder.RegisterInstance(_uiPanelPrefabConfigRepository);
            builder.Register<ActiveSceneScopeHolder>(Lifetime.Singleton).As<IActiveSceneScopeHolder>();
        }
    }
}
