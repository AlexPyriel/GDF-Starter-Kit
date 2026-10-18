namespace Assets.Project.Core.Infrastructure.Analytics
{
    /// <summary>
    /// No-op <see cref="IAnalyticsService"/>. Registered by default so game code can call
    /// analytics safely from day one, before a concrete vendor backend is wired. Replace
    /// with a vendor implementation in the composition root when ready.
    /// </summary>
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        /// <summary>Shared instance; the type carries no state.</summary>
        public static readonly NullAnalyticsService Instance = new NullAnalyticsService();

        /// <inheritdoc />
        public void TrackDesign(string eventId)
        {
        }

        /// <inheritdoc />
        public void TrackProgression(EAnalyticsProgressionStatus status, string progression01, string progression02)
        {
        }
    }
}
