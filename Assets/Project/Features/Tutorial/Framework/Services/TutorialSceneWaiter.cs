using System;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Interfaces;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <inheritdoc cref="ITutorialSceneWaiter" />
    public sealed class TutorialSceneWaiter : ITutorialSceneWaiter
    {
        private readonly IActiveSceneScopeHolder _holder;
        private readonly ISubscriber<SceneActivatedMessage> _sceneActivated;

        /// <summary>Creates the waiter on top of the active-scene holder and its message.</summary>
        /// <param name="holder">Active-scene state (query).</param>
        /// <param name="sceneActivated">Active-scene change (message).</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialSceneWaiter(IActiveSceneScopeHolder holder, ISubscriber<SceneActivatedMessage> sceneActivated)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _sceneActivated = sceneActivated ?? throw new ArgumentNullException(nameof(sceneActivated));
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

            void OnSceneActivated(SceneActivatedMessage message)
            {
                if (message.SceneType == scene)
                {
                    completionSource.TrySetResult();
                }
            }

            using (_sceneActivated.Subscribe(OnSceneActivated))
            {
                // Re-check after subscribing: the scene may have become active between IsActive and Subscribe.
                if (IsActive(scene))
                {
                    return;
                }

                await completionSource.Task.AttachExternalCancellation(token);
            }
        }
    }
}
