namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Tutorial state for external systems (for example, the action bus): is a tutorial currently
    /// running. Lets game systems publish actions only while a tutorial is active. Implemented by
    /// <c>TutorialService</c>.
    /// </summary>
    public interface ITutorialStateProvider
    {
        /// <summary>Whether a tutorial run is currently in progress.</summary>
        public bool IsTutorialInProgress { get; }
    }
}
