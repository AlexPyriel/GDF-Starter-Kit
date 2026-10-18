using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Assets.Project.Core.Bootstrap.Warmup.Steps
{
    /// <summary>
    /// Explicitly initializes Addressables at startup so the first scene or prefab
    /// load does not have to pay the catalog-build cost on the hot path. Idempotent:
    /// subsequent Addressables operations reuse the already-built catalog.
    /// </summary>
    public sealed class AddressablesInitializeStep : IStartupWarmupStep
    {
        /// <inheritdoc />
        public string Name => nameof(AddressablesInitializeStep);

        /// <inheritdoc />
        public async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            await Addressables.InitializeAsync().Task;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
