using Audio.Installers.VContainer;
using LocalDataStorage.Installers.VContainer;
using Assets.Project.Core.Bootstrap;
using Assets.Project.Core.Bootstrap.Warmup;
using Assets.Project.Core.Bootstrap.Warmup.Steps;
using Assets.Project.Core.DI.EntryPoints;
using Assets.Project.Core.Infrastructure.Analytics;
using Assets.Project.Core.Infrastructure.SceneLoading.Installers;
using System;
using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using Assets.Project.Core.Infrastructure.UI.Providers;
using Assets.Project.Core.Infrastructure.UI.Services;
using Assets.Project.Features.Tutorial.Framework.Installers;
using SceneLoader.Installers.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.DI
{
    /// <summary>
    /// Корневой LifetimeScope проекта. Используется как composition root приложения.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private SceneLoadingInstaller _sceneLoadingInstaller;

        [SerializeField]
        private UIRoot _globalOverlayRootPrefab;

        [SerializeField]
        [Tooltip("Tutorial installer asset. Optional: assign to enable the tutorial system; leave empty to omit it.")]
        private TutorialInstaller _tutorialInstaller;

        /// <summary>
        /// Регистрирует глобальные зависимости приложения в корневом контейнере.
        /// </summary>
        /// <param name="builder">Построитель контейнера для регистрации зависимостей.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            if (_globalOverlayRootPrefab == null)
                throw new InvalidOperationException($"{nameof(RootLifetimeScope)} requires {nameof(_globalOverlayRootPrefab)} to be assigned.");

            _sceneLoadingInstaller.Install(builder);
            new LocalDataStorageVContainerInstaller().Install(builder);
            new SceneLoaderVContainerInstaller().Install(builder);
            new AudioVContainerInstaller().Install(builder);
            builder.Register<IAnalyticsService, NullAnalyticsService>(Lifetime.Singleton);
            builder.Register<AddressableUIViewLoaderService>(Lifetime.Singleton);
            builder.Register(CreateGlobalOverlayRoot, Lifetime.Singleton);
            builder.Register<IUIAssetProvider, AddressableUIAssetProvider>(Lifetime.Singleton);
            builder.Register<GlobalOverlayService>(Lifetime.Singleton);
            builder.Register<IStartupWarmupStep, AddressablesInitializeStep>(Lifetime.Singleton);
            builder.Register<IStartupWarmupStep, PreloadLoadingSceneViewStep>(Lifetime.Singleton);
            builder.Register<StartupWarmupService>(Lifetime.Singleton).AsSelf().As<IStartupWarmupAwaiter>();
            builder.Register<GameRunnerService>(Lifetime.Singleton);

            if (_tutorialInstaller != null)
                _tutorialInstaller.Install(builder);

            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }

        private UIRoot CreateGlobalOverlayRoot(IObjectResolver resolver)
        {
            UIRoot uiRoot = resolver.Instantiate(_globalOverlayRootPrefab);
            DontDestroyOnLoad(uiRoot.gameObject);
            return uiRoot;
        }
    }
}
