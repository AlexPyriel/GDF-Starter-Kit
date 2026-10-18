namespace Assets.Project.Core.Infrastructure.Analytics
{
    /// <summary>
    /// Vendor-neutral low-level analytics sink used by the template.
    /// Game code should not call this directly: wrap it in a semantic facade
    /// (NewGameStarted, LevelCompleted, etc.) that composes event IDs from a
    /// per-game registry so the vendor backend can be swapped without touching callers.
    /// Register a concrete implementation (GameAnalytics, Unity Analytics, Firebase, Amplitude, …)
    /// in the composition root; <see cref="NullAnalyticsService"/> is the default when no vendor is wired.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Records a design event (a bare counter with no payload).
        /// </summary>
        /// <param name="eventId">Event identifier from the host-project event registry.</param>
        public void TrackDesign(string eventId);

        /// <summary>
        /// Records a progression event with two hierarchy levels (for example, "level" + a stage key).
        /// </summary>
        /// <param name="status">Progression status: start, success, or failure.</param>
        /// <param name="progression01">First hierarchy level (kind of stage).</param>
        /// <param name="progression02">Second hierarchy level (specific stage identifier).</param>
        public void TrackProgression(EAnalyticsProgressionStatus status, string progression01, string progression02);
    }
}
