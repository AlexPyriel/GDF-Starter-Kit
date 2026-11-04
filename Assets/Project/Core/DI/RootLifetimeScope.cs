using System;
using GDF.Ads.Installers.VContainer;
using GDF.Analytics.Installers.VContainer;
using Audio.Installers.VContainer;
using Assets.Project.Core.Bootstrap;
using Assets.Project.Core.Bootstrap.Warmup;
using Assets.Project.Core.Bootstrap.Warmup.Steps;
using Assets.Project.Core.DI.EntryPoints;
using Assets.Project.Core.Infrastructure.SceneLoading.Installers;
using Assets.Project.Core.Infrastructure.UI.Hosts;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using Assets.Project.Core.Infrastructure.UI.Providers;
using Assets.Project.Core.Infrastructure.UI.Services;
using Assets.Project.Features.Tutorial.Framework.Installers;
using GDF.Currency.Config;
using GDF.Currency.Installers.VContainer;
using LocalDataStorage.Installers.VContainer;
using MessagePipe;
using SceneLoader.Installers.VContainer;
using GDF.TimerService.Installers.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Core.DI
{
    /// <summary>
    /// Composition root for the starter kit. Registers every GDF package service through its DI
    /// installer, wires the UI host prefab, warmup pipeline, scene loader, tutorial system and the
    /// bootstrap entry point. All GDF packages are hooked up on default settings — a cloned game
    /// can override anything through the serialized fields or by editing this file.
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

        [SerializeField]
        [Tooltip("Currency repository asset. Optional: leave empty for a prototype without currencies — the Currency service stays registered as a safe no-op surface.")]
        private CurrencyRepository _currencyRepository;

        /// <summary>Registers global application dependencies.</summary>
        /// <param name="builder">Container builder used for every GDF package installer.</param>
        protected override void Configure(IContainerBuilder builder)
        {
            if (_globalOverlayRootPrefab == null)
                throw new InvalidOperationException($"{nameof(RootLifetimeScope)} requires {nameof(_globalOverlayRootPrefab)} to be assigned.");

            // MessagePipe brokers for cross-feature domain events. Singleton brokers in the root scope
            // are shared with every scene scope, so gameplay publishers and root subscribers (tutorial,
            // analytics) meet on the same channel. See docs/architecture/adr-0001-messaging.md.
            builder.RegisterMessagePipe();

            _sceneLoadingInstaller.Install(builder);

            // Local data storage uses the crash-safe file backend by default (atomic writes + .bak)
            // so clones get Steam Cloud / Google Play / iCloud compatibility without extra wiring.
            new LocalDataStorageVContainerInstaller()
                .WithFileBackend()
                .Install(builder);

            new SceneLoaderVContainerInstaller().Install(builder);
            new AudioVContainerInstaller().Install(builder);

            // Default binding writes every analytics event to the console. Swap in a vendor backend
            // (GameAnalytics, Firebase, mixpanel) by chaining .WithBackend(...) above Install().
            new AnalyticsVContainerInstaller().Install(builder);

            new TimerServiceVContainerInstaller().Install(builder);

            // Currency is registered with a repository asset or omitted entirely in a non-F2P prototype.
            if (_currencyRepository != null)
                new CurrencyVContainerInstaller(_currencyRepository).Install(builder);

            // Ads facade with no vendor adapters — placeholder calls return Failed / false until a clone
            // plugs in AppLovin / UnityAds / IronSource adapters through this installer's fluent API.
            new AdsVContainerInstaller().Install(builder);

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
