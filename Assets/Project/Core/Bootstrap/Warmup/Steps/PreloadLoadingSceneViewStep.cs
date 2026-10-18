using System;
using System.Threading;
using Assets.Project.Core.Infrastructure.UI.Interfaces;
using Assets.Project.Features.Scenes.Loading;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Core.Bootstrap.Warmup.Steps
{
    /// <summary>
    /// Pre-caches the loading-screen prefab so the first scene transition does not
    /// have to wait for its Addressables load on the hot path. The provider caches
    /// the loaded prefab by key, so subsequent transitions reuse the cached handle.
    /// </summary>
    public sealed class PreloadLoadingSceneViewStep : IStartupWarmupStep
    {
        private readonly IUIAssetProvider _uiAssetProvider;

        /// <summary>
        /// Creates the loading-screen prefab warmup step.
        /// </summary>
        /// <param name="uiAssetProvider">Provider of authored UI prefabs.</param>
        public PreloadLoadingSceneViewStep(IUIAssetProvider uiAssetProvider)
        {
            _uiAssetProvider = uiAssetProvider ?? throw new ArgumentNullException(nameof(uiAssetProvider));
        }

        /// <inheritdoc />
        public string Name => nameof(PreloadLoadingSceneViewStep);

        /// <inheritdoc />
        public async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            await _uiAssetProvider.LoadScreenPrefabAsync<LoadingSceneView>();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
