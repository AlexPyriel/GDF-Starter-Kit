using Assets.Project.Features.Tutorial.Framework.Interfaces;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Mutable run state of the tutorial (root singleton). Breaks the cycle: the service SETS the flag,
    /// the action bus READS it (<see cref="ITutorialStateProvider"/>) — without a mutual inject between
    /// service and bus.
    /// </summary>
    public sealed class TutorialRunState : ITutorialStateProvider
    {
        /// <inheritdoc />
        public bool IsTutorialInProgress { get; private set; }

        /// <summary>Sets the "run in progress" flag (called by the service on start/stop).</summary>
        /// <param name="value">New flag value.</param>
        public void SetInProgress(bool value)
        {
            IsTutorialInProgress = value;
        }
    }
}
