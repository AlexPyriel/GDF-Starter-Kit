namespace Assets.Project.Features.Tutorial.Framework.Interfaces
{
    /// <summary>
    /// A tutorial list entry: a reference to the step prototype plus POSITIONAL orchestration
    /// (skip/checkpoint) authored by the designer directly in the tutorial config next to the step —
    /// without opening the step asset itself. The same step can appear in different tutorials with
    /// different flags.
    /// </summary>
    public interface ITutorialStepEntry
    {
        /// <summary>Step prototype (its behavior and <see cref="ITutorialStep.RequiredScene"/>). The runner clones it to run.</summary>
        public ITutorialStep Step { get; }

        /// <summary>Debug skip: the runner skips this step (for running only the steps you need).</summary>
        public bool IsDisabled { get; }

        /// <summary>Resume checkpoint: after a successful step the progress cursor is saved and the step is not replayed on re-entry.</summary>
        public bool ShouldSaveProgress { get; }
    }
}
