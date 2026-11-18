using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Bootstrap;
using Assets.Project.Core.Infrastructure.Pause.Interfaces;
using Assets.Project.Core.Infrastructure.UI.Presenters;
using Assets.Project.Core.Infrastructure.UI.Services;
using R3;

namespace Assets.Project.Features.Scenes.Game
{
    /// <summary>
    /// Coordinates the gameplay screen and its pause and exit confirmation panel flow. The game stays
    /// paused through <see cref="IGamePauseService"/> for exactly as long as the pause panel exists, so any
    /// way of closing it (resume, backdrop click, exit to Home, scene unload) resumes the game.
    /// </summary>
    public sealed class GameScenePresenter : Presenter<GameSceneView>
    {
        private const string PAUSE_REASON = nameof(GamePausePanel);

        private readonly GameRunnerService _gameRunnerService;
        private readonly SceneUIService _sceneUIService;
        private readonly IGamePauseService _gamePauseService;
        private IDisposable _clickSubscription;
        private IDisposable _pauseHandle;

        /// <summary>
        /// Creates the gameplay scene presenter and binds it to gameplay transition and UI services.
        /// </summary>
        /// <param name="gameRunnerService">The game flow service used to leave gameplay.</param>
        /// <param name="sceneUIService">The scene-local UI service that owns gameplay screens and panels.</param>
        /// <param name="gamePauseService">The application pause service held while the pause panel is open.</param>
        public GameScenePresenter(
            GameRunnerService gameRunnerService,
            SceneUIService sceneUIService,
            IGamePauseService gamePauseService)
        {
            _gameRunnerService = gameRunnerService ?? throw new ArgumentNullException(nameof(gameRunnerService));
            _sceneUIService = sceneUIService ?? throw new ArgumentNullException(nameof(sceneUIService));
            _gamePauseService = gamePauseService ?? throw new ArgumentNullException(nameof(gamePauseService));
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

            ReleasePause(_pauseHandle);

            _sceneUIService.HideScreenAsync<GameSceneView>().Forget();
        }

        private async UniTask ShowPausePanelAsync()
        {
            if (_pauseHandle != null)
                return;

            IDisposable handle = _gamePauseService.Pause(PAUSE_REASON);
            _pauseHandle = handle;

            GamePausePanel panel;
            try
            {
                panel = await _sceneUIService.ShowPanelAsync<GamePausePanel>();
            }
            catch
            {
                ReleasePause(handle);
                throw;
            }

            panel.Configure(CloseTopPanel, OpenExitConfirmPanel);

            // Every close path destroys the panel (SceneUIService releases panels by destroying them),
            // so binding the handle to the panel's lifetime resumes the game however the panel closes.
            panel.destroyCancellationToken.Register(() => ReleasePause(handle));
        }

        private void ReleasePause(IDisposable handle)
        {
            if (handle == null)
                return;

            handle.Dispose();
            if (ReferenceEquals(_pauseHandle, handle))
                _pauseHandle = null;
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
