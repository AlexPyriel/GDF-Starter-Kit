using Cysharp.Threading.Tasks;
using Assets.Project.Core.Bootstrap;
using VContainer.Unity;

namespace Assets.Project.Core.DI.EntryPoints
{
    /// <summary>
    /// Starts the bootstrap flow after the root container is built.
    /// </summary>
    public sealed class BootstrapEntryPoint : IStartable
    {
        private readonly GameRunnerService _gameRunnerService;

        /// <summary>
        /// Creates the entry point wrapper for the bootstrap flow.
        /// </summary>
        /// <param name="gameRunnerService">Application startup flow service.</param>
        public BootstrapEntryPoint(GameRunnerService gameRunnerService)
        {
            _gameRunnerService = gameRunnerService;
        }

        /// <summary>
        /// Kicks off the asynchronous bootstrap sequence.
        /// </summary>
        public void Start()
        {
            _gameRunnerService.RunAsync().Forget();
        }
    }
}
