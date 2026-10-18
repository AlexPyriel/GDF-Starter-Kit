using System;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Progress;

namespace Assets.Project.Features.Tutorial.Framework.Steps.Base
{
    /// <summary>
    /// Surface of FRAMEWORK dependencies for a step (root scope, single type): overlay, scene waiter,
    /// progress, highlight registry, action tracker. Injected into <see cref="TutorialStepAsset"/>; the
    /// base exposes it to subclasses. Game managers are obtained by the step itself via <c>[Inject]</c>
    /// against the scene resolver — they are not here.
    /// </summary>
    public sealed class TutorialContext
    {
        /// <summary>Tutorial overlay (dim, cutouts, ContentRoot).</summary>
        public ITutorialOverlay Overlay { get; }

        /// <summary>Scene wait across transitions.</summary>
        public ITutorialSceneWaiter SceneWaiter { get; }

        /// <summary>Progress: completion flag and resume cursor.</summary>
        public ITutorialProgressService Progress { get; }

        /// <summary>Highlight-group registry (target lookup by key).</summary>
        public ITutorialHighlightRegistry HighlightRegistry { get; }

        /// <summary>Action tracker: a step waits for actions published by game code.</summary>
        public ITutorialActionTracker Actions { get; }

        /// <summary>Creates the context.</summary>
        /// <param name="overlay">Tutorial overlay.</param>
        /// <param name="sceneWaiter">Scene waiter.</param>
        /// <param name="progress">Tutorial progress.</param>
        /// <param name="highlightRegistry">Highlight registry.</param>
        /// <param name="actions">Action tracker.</param>
        /// <exception cref="ArgumentNullException">Any dependency is <see langword="null"/>.</exception>
        public TutorialContext(
            ITutorialOverlay overlay,
            ITutorialSceneWaiter sceneWaiter,
            ITutorialProgressService progress,
            ITutorialHighlightRegistry highlightRegistry,
            ITutorialActionTracker actions)
        {
            Overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
            SceneWaiter = sceneWaiter ?? throw new ArgumentNullException(nameof(sceneWaiter));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            HighlightRegistry = highlightRegistry ?? throw new ArgumentNullException(nameof(highlightRegistry));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }
    }
}
