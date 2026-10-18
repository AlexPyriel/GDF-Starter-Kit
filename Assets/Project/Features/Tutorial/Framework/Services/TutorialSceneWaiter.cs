using System;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Cysharp.Threading.Tasks;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <inheritdoc cref="ITutorialSceneWaiter" />
    public sealed class TutorialSceneWaiter : ITutorialSceneWaiter
    {
        private readonly IActiveSceneScopeHolder _holder;

        /// <summary>Creates the waiter on top of the active-scene holder.</summary>
        /// <param name="holder">Active-scene signal source.</param>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> is <see langword="null"/>.</exception>
        public TutorialSceneWaiter(IActiveSceneScopeHolder holder)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
        }

        /// <inheritdoc />
        public bool IsActive(ESceneType scene)
        {
            return _holder.ActiveScene == scene;
        }

        /// <inheritdoc />
        public async UniTask WaitForScene(ESceneType scene, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (IsActive(scene))
            {
                return;
            }

            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();

            void OnActiveSceneChanged(ESceneType active)
            {
                if (active == scene)
                {
                    completionSource.TrySetResult();
                }
            }

            _holder.ActiveSceneChanged += OnActiveSceneChanged;
            try
            {
                // Re-check after subscribing: the scene may have become active between IsActive and Subscribe.
                if (IsActive(scene))
                {
                    return;
                }

                await completionSource.Task.AttachExternalCancellation(token);
            }
            finally
            {
                _holder.ActiveSceneChanged -= OnActiveSceneChanged;
            }
        }
    }
}
