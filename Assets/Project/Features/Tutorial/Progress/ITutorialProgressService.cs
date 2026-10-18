namespace Assets.Project.Features.Tutorial.Progress
{
    /// <summary>
    /// Completion flags for tutorials and the resume cursor for step-by-step tutorials. Kept separate
    /// from the game save so that clearing the save or starting a new game does not reset the
    /// "show again?" state. The current implementation uses PlayerPrefs; swap to the game save gateway
    /// of a concrete game as needed.
    /// </summary>
    public interface ITutorialProgressService
    {
        /// <summary>Whether the tutorial is completed.</summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        /// <returns><see langword="true"/> if the tutorial is marked completed.</returns>
        public bool IsCompleted(string tutorialKey);

        /// <summary>Marks the tutorial completed and persists the flag immediately.</summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        public void MarkCompleted(string tutorialKey);

        /// <summary>
        /// Resume cursor — the index of the NEXT unfinished step. On re-entry the runner starts from
        /// it, skipping steps with a lower index.
        /// </summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        /// <returns>Next step index; 0 if the tutorial has not started.</returns>
        public int GetCursor(string tutorialKey);

        /// <summary>Saves the resume cursor (next step index) and persists it immediately.</summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        /// <param name="stepIndex">Next unfinished step index.</param>
        public void SetCursor(string tutorialKey, int stepIndex);

        /// <summary>
        /// Resets one tutorial — clears completion flag and resume cursor ("replay").
        /// A hook for the debug panel.
        /// </summary>
        /// <param name="tutorialKey">Tutorial key from <see cref="TutorialKeys"/>.</param>
        public void Reset(string tutorialKey);

        /// <summary>Resets ALL known tutorials (<see cref="TutorialKeys.All"/>) — for the editor save-reset menu.</summary>
        public void ResetAll();
    }
}
