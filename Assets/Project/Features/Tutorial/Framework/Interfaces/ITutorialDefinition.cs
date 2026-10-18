using System.Collections.Generic;

namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// Tutorial definition contract: progress identifier, ordered step prototypes, and a skip flag.
    /// Abstracts <c>TutorialDefinition</c> (SO) from the orchestrator — the service depends on the
    /// contract rather than a concrete asset (testability: a fake definition with no ScriptableObject).
    /// </summary>
    public interface ITutorialDefinition
    {
        /// <summary>Tutorial identifier (completion key and resume cursor key).</summary>
        public string TutorialId { get; }

        /// <summary>Ordered entries (step + positional flags); list index = resume cursor.</summary>
        public IReadOnlyList<ITutorialStepEntry> Steps { get; }

        /// <summary>
        /// "Do not run tutorial" flag — makes <c>IsCompleted</c> computed-true without writing to
        /// progress. Editor-only: in a release build <c>TutorialService</c> does not read this.
        /// </summary>
        public bool SkipTutorial { get; }
    }
}
