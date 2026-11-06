using System;
using Assets.Project.Features.Tutorial.Analytics;
using Assets.Project.Features.Tutorial.Framework.Config;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Presentation.Views;
using Assets.Project.Features.Tutorial.Framework.Services;
using Assets.Project.Features.Tutorial.Framework.Steps.Base;
using Assets.Project.Features.Tutorial.Progress;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Project.Features.Tutorial.Framework.Installers
{
    /// <summary>
    /// Tutorial asset installer (root scope): holds references (overlay prefab, definition) and
    /// registers ALL tutorial system dependencies in one place. Invoked from <c>RootLifetimeScope</c>.
    /// No direct tutorial registrations should live in the root scope — everything goes here.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/" + nameof(TutorialInstaller), fileName = nameof(TutorialInstaller), order = 30)]
    public sealed class TutorialInstaller : ScriptableObject, IInstaller
    {
        [SerializeField]
        [Tooltip("Tutorial overlay view prefab (instantiated under the global UIRoot.TutorialRoot).")]
        private TutorialOverlayView _overlayPrefab;

        [SerializeField]
        [Tooltip("Single tutorial definition (MVP). The skipTutorial flag disables it in the editor only; " +
                 "in a release build the tutorial always runs until completed.")]
        private TutorialDefinition _definition;

        /// <summary>Registers all tutorial-system dependencies in the root scope.</summary>
        /// <param name="builder">Root container builder.</param>
        /// <exception cref="InvalidOperationException">A required reference is not assigned.</exception>
        public void Install(IContainerBuilder builder)
        {
            if (_overlayPrefab == null)
                throw new InvalidOperationException($"{nameof(TutorialInstaller)} requires {nameof(_overlayPrefab)} to be assigned.");

            if (_definition == null)
                throw new InvalidOperationException($"{nameof(TutorialInstaller)} requires {nameof(_definition)} to be assigned.");

            // Progress (PlayerPrefs by default; swap with a game-save-backed service for a concrete game).
            builder.Register<PlayerPrefsTutorialProgressService>(Lifetime.Singleton).As<ITutorialProgressService>();

            // Scene gate on top of the Core active-scene holder (SceneLoadingInstaller) and SceneActivatedMessage.
            builder.Register<TutorialSceneWaiter>(Lifetime.Singleton).As<ITutorialSceneWaiter>();

            // Highlight registry (TutorialHighlightGroup tags find targets by key). EAGER (entry point):
            // Current must be ready BEFORE scenes load, otherwise group OnEnable misses registration.
            builder.RegisterEntryPoint<TutorialHighlightRegistry>().As<ITutorialHighlightRegistry>();

            // Message waiter: game code publishes domain messages via MessagePipe (registered by the
            // root scope), steps wait for them.
            builder.Register<TutorialMessageWaiter>(Lifetime.Singleton).As<ITutorialMessageWaiter>();

            // Overlay (prefab -> instance under the global TutorialRoot), context, runner, orchestrator.
            builder.RegisterInstance(_overlayPrefab);
            builder.Register<TutorialOverlay>(Lifetime.Singleton).As<ITutorialOverlay>();
            builder.Register<TutorialContext>(Lifetime.Singleton);
            builder.Register<TutorialStepRunner>(Lifetime.Singleton);
            builder.RegisterInstance(_definition).As<ITutorialDefinition>();
            builder.RegisterEntryPoint<TutorialService>().AsSelf();

            // Analytics funnel for the tutorial (reports through IAnalyticsService, which the root scope
            // binds to NullAnalyticsService by default — swap for a vendor implementation).
            builder.RegisterEntryPoint<TutorialAnalyticsTracker>();
        }
    }
}
