using System.Collections.Generic;
using Assets.Project.Core.Infrastructure.Analytics;

namespace Assets.Project.Tests.EditMode.Tutorial.Analytics
{
    /// <summary>
    /// Test double for <see cref="IAnalyticsService"/>: records every design event id and every
    /// progression triple into an in-memory list. Lets tests assert the funnel the tracker produces
    /// without any vendor-specific transport.
    /// </summary>
    internal sealed class RecordingAnalyticsService : IAnalyticsService
    {
        /// <summary>Design event ids recorded in call order.</summary>
        public List<string> Design { get; } = new List<string>();

        /// <summary>Progression triples recorded in call order (status, p01, p02).</summary>
        public List<(EAnalyticsProgressionStatus Status, string P01, string P02)> Progression { get; }
            = new List<(EAnalyticsProgressionStatus, string, string)>();

        /// <inheritdoc />
        public void TrackDesign(string eventId)
        {
            Design.Add(eventId);
        }

        /// <inheritdoc />
        public void TrackProgression(EAnalyticsProgressionStatus status, string progression01, string progression02 = null)
        {
            Progression.Add((status, progression01, progression02));
        }
    }
}
