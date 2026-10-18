using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Bootstrap.Warmup;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Configuration;
using Assets.Project.Features.Scenes.Game;
using Assets.Project.Features.Scenes.Home;
using Assets.Project.Features.Scenes.Loading;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using SceneLoader;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Assets.Project.Core.Bootstrap
{
    /// <summary>
    /// Orchestrates the initial bootstrap flow for the application.
    /// </summary>
    public sealed class GameRunnerService
    {
        private readonly SceneConfigRepository _sceneConfigRepository;
        private readonly IUIAssetProvider _uiAssetProvider;
        private readonly ISceneLoaderService _sceneLoaderService;
        private readonly StartupWarmupService _startupWarmupService;
        private int _transitionInProgress;

        /// <summary>
        /// Creates the startup flow service.
        /// </summary>
        /// <param name="sceneConfigRepository">Project-specific scene config repository.</param>
        /// <param name="uiAssetProvider">UI prefab provider used to warm up transition screens.</param>
        /// <param name="sceneLoaderService">Scene loading service used for bootstrap transitions.</param>
        /// <param name="startupWarmupService">Startup warmup service that runs in parallel with the initial scene load.</param>
        public GameRunnerService(
            SceneConfigRepository sceneConfigRepository,
            IUIAssetProvider uiAssetProvider,
            ISceneLoaderService sceneLoaderService,
            StartupWarmupService startupWarmupService)
        {
            _sceneConfigRepository = sceneConfigRepository;
            _uiAssetProvider = uiAssetProvider;
            _sceneLoaderService = sceneLoaderService;
            _startupWarmupService = startupWarmupService ?? throw new ArgumentNullException(nameof(startupWarmupService));
        }

        /// <summary>
        /// Runs the initial bootstrap sequence. Startup warmup is kicked off fire-and-forget
        /// so background initialization overlaps the first scene transition; consumers that
        /// need warmup completion wait on <see cref="IStartupWarmupAwaiter"/>.
        /// </summary>
        public async UniTask RunAsync()
        {
            _startupWarmupService.RunAsync().Forget();
            await TransitionToSceneAsync(ESceneType.Home);
        }

        /// <summary>
        /// Loads the home scene through the loading scene flow.
        /// </summary>
        public UniTask LoadHomeSceneAsync()
        {
            return TransitionToSceneAsync(ESceneType.Home);
        }

        /// <summary>
        /// Loads the game scene through the loading scene flow.
        /// </summary>
        public UniTask LoadGameSceneAsync()
        {
            return TransitionToSceneAsync(ESceneType.Game);
        }

        private async UniTask TransitionToSceneAsync(ESceneType targetSceneName)
        {
            if (targetSceneName == ESceneType.Loading)
            {
                Debug.LogWarning($"[{nameof(GameRunnerService)}] '{ESceneType.Loading}' is a service scene and cannot be used as a transition target.");
                return;
            }

            if (Interlocked.CompareExchange(ref _transitionInProgress, 1, 0) != 0)
            {
                Debug.LogWarning($"[{nameof(GameRunnerService)}] Transition to '{targetSceneName}' was ignored because another transition is already running.");
                return;
            }

            bool loadingSceneNeedsCleanup = false;

            try
            {
                if (!_sceneConfigRepository.TryGet(ESceneType.Loading, out SceneConfig loadingSceneConfig))
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Scene reference not found for key '{ESceneType.Loading}'.");
                    return;
                }
                AssetReference loadingSceneReference = loadingSceneConfig.SceneReference;

                if (!_sceneConfigRepository.TryGet(targetSceneName, out SceneConfig targetSceneConfig))
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Scene reference not found for key '{targetSceneName}'.");
                    return;
                }
                AssetReference targetSceneReference = targetSceneConfig.SceneReference;
                await PreloadTransitionUiAsync(targetSceneName);

                LoadResult loadResult = await _sceneLoaderService.LoadScene(
                    loadingSceneReference,
                    reportProgress: false);
                if (!loadResult.Success)
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Loading scene load failed.{FormatError(loadResult.Error)}");
                    return;
                }

                loadingSceneNeedsCleanup = true;

                await UniTask.Delay(TimeSpan.FromSeconds(1));

                loadResult = await _sceneLoaderService.LoadScene(
                    targetSceneReference,
                    LoadSceneMode.Additive,
                    activateOnLoad: false,
                    reportProgress: true);
                if (!loadResult.Success)
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Target scene load failed for '{targetSceneName}'.{FormatError(loadResult.Error)}");
                    return;
                }

                // Let the loading UI render the final 100% state before the target scene becomes active.
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                await UniTask.NextFrame();

                loadResult = await _sceneLoaderService.ActivateScene(targetSceneReference);
                if (!loadResult.Success)
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Target scene activation failed for '{targetSceneName}'.{FormatError(loadResult.Error)}");
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

                loadResult = await _sceneLoaderService.UnloadScene(loadingSceneReference);
                if (!loadResult.Success)
                {
                    Debug.LogWarning($"[{nameof(GameRunnerService)}] Loading scene unload failed.{FormatError(loadResult.Error)}");
                }
                loadingSceneNeedsCleanup = false;
            }
            finally
            {
                if (loadingSceneNeedsCleanup)
                {
                    if (_sceneConfigRepository.TryGet(ESceneType.Loading, out SceneConfig loadingSceneConfig))
                    {
                        AssetReference loadingSceneReference = loadingSceneConfig.SceneReference;
                        LoadResult unloadResult = await _sceneLoaderService.UnloadScene(loadingSceneReference);
                        if (!unloadResult.Success)
                        {
                            Debug.LogWarning($"[{nameof(GameRunnerService)}] Loading scene cleanup failed.{FormatError(unloadResult.Error)}");
                        }
                    }
                }

                Volatile.Write(ref _transitionInProgress, 0);
            }
        }

        private static string FormatError(string error)
        {
            return string.IsNullOrWhiteSpace(error) ? string.Empty : $" {error}";
        }

        /// <summary>
        /// Warms up the loading screen and the first screen of the target scene so the transition can reuse
        /// cached screen prefabs when each scene-local UI root asks the provider for its first visible screen.
        /// </summary>
        private async UniTask PreloadTransitionUiAsync(ESceneType targetSceneName)
        {
            await _uiAssetProvider.LoadScreenPrefabAsync<LoadingSceneView>();

            switch (targetSceneName)
            {
                case ESceneType.Home:
                    await _uiAssetProvider.LoadScreenPrefabAsync<HomeSceneView>();
                    break;
                case ESceneType.Game:
                    await _uiAssetProvider.LoadScreenPrefabAsync<GameSceneView>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetSceneName), targetSceneName, "Unsupported scene type for initial screen preload.");
            }
        }
    }
}
