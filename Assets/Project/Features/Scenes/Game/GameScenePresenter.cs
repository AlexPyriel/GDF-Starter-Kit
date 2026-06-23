using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Bootstrap;
using Assets.Project.Core.Infrastructure.UI.Presenters;
using Assets.Project.Core.Infrastructure.UI.Services;
using R3;

namespace Assets.Project.Features.Scenes.Game
{
    /// <summary>
    /// Coordinates the gameplay screen and its pause and exit confirmation panel flow.
    /// </summary>
    public sealed class GameScenePresenter : Presenter<GameSceneView>
    {
        private readonly GameRunnerService _gameRunnerService;
        private readonly SceneUIService _sceneUIService;
        private IDisposable _clickSubscription;

        /// <summary>
        /// Creates the gameplay scene presenter and binds it to gameplay transition and UI services.
        /// </summary>
        /// <param name="gameRunnerService">The game flow service used to leave gameplay.</param>
        /// <param name="sceneUIService">The scene-local UI service that owns gameplay screens and panels.</param>
        public GameScenePresenter(
            GameRunnerService gameRunnerService,
            SceneUIService sceneUIService)
        {
            _gameRunnerService = gameRunnerService ?? throw new ArgumentNullException(nameof(gameRunnerService));
            _sceneUIService = sceneUIService ?? throw new ArgumentNullException(nameof(sceneUIService));
        }

        protected override async UniTask StartPresenterAsync(CancellationToken cancellation)
        {
            View = await _sceneUIService.ShowScreenAsync<GameSceneView>();
            cancellation.ThrowIfCancellationRequested();

            _clickSubscription?.Dispose();
            _clickSubscription = View.PauseButtonClicked.Subscribe(_ => ShowPausePanelAsync().Forget());
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _clickSubscription?.Dispose();
            _clickSubscription = null;

            _sceneUIService.HideScreenAsync<GameSceneView>().Forget();
        }

        private async UniTask ShowPausePanelAsync()
        {
            GamePausePanel panel = await _sceneUIService.ShowPanelAsync<GamePausePanel>();
            panel.Configure(CloseTopPanel, OpenExitConfirmPanel);
        }

        private void OpenExitConfirmPanel()
        {
            ShowExitConfirmPanelAsync().Forget();
        }

        private async UniTask ShowExitConfirmPanelAsync()
        {
            GameExitConfirmPanel panel = await _sceneUIService.ShowPanelAsync<GameExitConfirmPanel>();
            panel.Configure(CloseTopPanel, LoadHomeScene);
        }

        private void CloseTopPanel()
        {
            _sceneUIService.DismissTopPanelAsync().Forget();
        }

        private void LoadHomeScene()
        {
            ExitToHomeAsync().Forget();
        }

        private async UniTaskVoid ExitToHomeAsync()
        {
            await _sceneUIService.DismissAllPanelsAsync();
            await _gameRunnerService.LoadHomeSceneAsync();
        }
    }
}
