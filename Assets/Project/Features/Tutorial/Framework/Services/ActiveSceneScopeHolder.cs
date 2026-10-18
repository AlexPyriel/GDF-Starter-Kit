using System;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using VContainer;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <inheritdoc cref="IActiveSceneScopeHolder" />
    public sealed class ActiveSceneScopeHolder : IActiveSceneScopeHolder
    {
        /// <inheritdoc />
        public ESceneType? ActiveScene { get; private set; }

        /// <inheritdoc />
        public IObjectResolver ActiveResolver { get; private set; }

        /// <inheritdoc />
        public event Action<ESceneType> ActiveSceneChanged;

        /// <inheritdoc />
        public void SetActive(ESceneType scene, IObjectResolver resolver)
        {
            // Order matters: the (scene <-> resolver) pair is set BEFORE the event, so a subscriber
            // (waiter -> runner) already sees the correct resolver of the active scene. Removes the
            // event-before-state race.
            ActiveScene = scene;
            ActiveResolver = resolver;
            ActiveSceneChanged?.Invoke(scene);
        }
    }
}
