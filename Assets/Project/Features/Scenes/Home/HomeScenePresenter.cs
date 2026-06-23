using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Project.Core.Bootstrap;
using Assets.Project.Core.Infrastructure.UI.Presenters;
using Assets.Project.Core.Infrastructure.UI.Services;
using R3;

namespace Assets.Project.Features.Scenes.Home
{
    /// <summary>
    /// Coordinates the home scene screen and routes its primary action into the game flow.
    /// </summary>
    public sealed class HomeScenePresenter : Presenter<HomeSceneView>
    {
        private readonly GameRunnerService _gameRunnerService;
        private readonly SceneUIService _sceneUIService;
        private IDisposable _gameButtonSubscription;

        /// <summary>
        /// Creates the home scene presenter and binds it to the scene UI flow and game runner.
        /// </summary>
        /// <param name="gameRunnerService">The game flow service used to start gameplay.</param>
        /// <param name="sceneUIService">The scene-local UI service that owns the home screen.</param>
        public HomeScenePresenter(
            GameRunnerService gameRunnerService,
            SceneUIService sceneUIService)
        {
            _gameRunnerService = gameRunnerService ?? throw new ArgumentNullException(nameof(gameRunnerService));
            _sceneUIService = sceneUIService ?? throw new ArgumentNullException(nameof(sceneUIService));
        }

        protected override async UniTask StartPresenterAsync(CancellationToken cancellation)
        {
            View = await _sceneUIService.ShowScreenAsync<HomeSceneView>();
            cancellation.ThrowIfCancellationRequested();

            _gameButtonSubscription?.Dispose();
            _gameButtonSubscription = View.ButtonClicked.Subscribe(_ => _gameRunnerService.LoadGameSceneAsync().Forget());
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _gameButtonSubscription?.Dispose();
            _gameButtonSubscription = null;

            _sceneUIService.HideScreenAsync<HomeSceneView>().Forget();
        }
    }
}
