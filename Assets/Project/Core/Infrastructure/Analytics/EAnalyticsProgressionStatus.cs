namespace Assets.Project.Core.Infrastructure.Analytics
{
    /// <summary>
    /// Status of a progression attempt reported through <see cref="IAnalyticsService.TrackProgression"/>.
    /// Vendor-neutral enum so game code does not depend on any analytics SDK types.
    /// </summary>
    public enum EAnalyticsProgressionStatus
    {
        /// <summary>Attempt started.</summary>
        Start = 0,

        /// <summary>Attempt completed successfully.</summary>
        Complete = 1,

        /// <summary>Attempt failed (loss, draw, quit).</summary>
        Fail = 2,
    }
}
