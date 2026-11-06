using System;
using Assets.Project.Core.Infrastructure.SceneLoading.Interfaces;
using Assets.Project.Core.Infrastructure.SceneLoading.Messages;
using MessagePipe;
using VContainer;

namespace Assets.Project.Core.Infrastructure.SceneLoading.Services
{
    /// <inheritdoc cref="IActiveSceneScopeHolder" />
    public sealed class ActiveSceneScopeHolder : IActiveSceneScopeHolder
    {
        private readonly IPublisher<SceneActivatedMessage> _sceneActivatedPublisher;

        /// <summary>Creates the holder.</summary>
        /// <param name="sceneActivatedPublisher">Channel for <see cref="SceneActivatedMessage"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sceneActivatedPublisher"/> is <see langword="null"/>.</exception>
        public ActiveSceneScopeHolder(IPublisher<SceneActivatedMessage> sceneActivatedPublisher)
        {
            _sceneActivatedPublisher = sceneActivatedPublisher ?? throw new ArgumentNullException(nameof(sceneActivatedPublisher));
        }

        /// <inheritdoc />
        public ESceneType? ActiveScene { get; private set; }

        /// <inheritdoc />
        public IObjectResolver ActiveResolver { get; private set; }

        /// <inheritdoc />
        public void SetActive(ESceneType scene, IObjectResolver resolver)
        {
            // Order matters: the (scene <-> resolver) pair is set BEFORE the message, so a subscriber
            // (waiter -> runner) already sees the correct resolver of the active scene.
            ActiveScene = scene;
            ActiveResolver = resolver;
            _sceneActivatedPublisher.Publish(new SceneActivatedMessage(scene));
        }
    }
}
