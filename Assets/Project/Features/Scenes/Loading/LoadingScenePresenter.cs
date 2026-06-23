using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Infrastructure.UI.Presenters;
using Assets.Project.Core.Infrastructure.UI.Services;
using SceneLoader;

namespace Assets.Project.Features.Scenes.Loading
{
    /// <summary>
    /// Coordinates the loading screen and mirrors scene-loading progress into the authored loading UI.
    /// </summary>
    public sealed class LoadingScenePresenter : Presenter<LoadingSceneView>
    {
        private readonly ISceneLoadingProgress _sceneLoadingProgress;
        private readonly SceneUIService _sceneUIService;
        private Action<float> _progressChangedHandler;

        /// <summary>
        /// Creates the loading scene presenter and binds it to loading progress updates.
        /// </summary>
        /// <param name="sceneLoadingProgress">The progress source for the current scene transition.</param>
        /// <param name="sceneUIService">The scene-local UI service that owns the loading screen.</param>
        public LoadingScenePresenter(
            ISceneLoadingProgress sceneLoadingProgress,
            SceneUIService sceneUIService)
        {
            _sceneLoadingProgress = sceneLoadingProgress ?? throw new ArgumentNullException(nameof(sceneLoadingProgress));
            _sceneUIService = sceneUIService ?? throw new ArgumentNullException(nameof(sceneUIService));
        }

        protected override async UniTask StartPresenterAsync(CancellationToken cancellation)
        {
            View = await _sceneUIService.ShowScreenAsync<LoadingSceneView>();
            cancellation.ThrowIfCancellationRequested();

            _progressChangedHandler = View.SetProgress;
            _sceneLoadingProgress.ProgressChanged += _progressChangedHandler;
            // Avoid showing stale progress from the previous transition before the new load reports.
            View.SetProgress(0f);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_progressChangedHandler != null)
            {
                _sceneLoadingProgress.ProgressChanged -= _progressChangedHandler;
                _progressChangedHandler = null;
            }

            View?.ResetState();
            _sceneUIService.HideScreenAsync<LoadingSceneView>().Forget();
        }
    }
}
