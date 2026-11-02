using System.Collections.Generic;
using GDF.Analytics;

namespace Assets.Project.Tests.EditMode.Tutorial.Analytics
{
    /// <summary>
    /// Test double for <see cref="IAnalyticsService"/>: records every call into in-memory lists so
    /// tests can assert the exact funnel produced by the tutorial tracker without any vendor-specific
    /// transport. Short-circuits the composite dispatch to keep the test fixture tiny.
    /// </summary>
    internal sealed class RecordingAnalyticsService : IAnalyticsService
    {
        /// <summary>Info event ids recorded in call order.</summary>
        public List<string> Info { get; } = new List<string>();

        /// <summary>Error messages recorded in call order.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Progression triples recorded in call order (status, p01, p02).</summary>
        public List<(EAnalyticsProgressionStatus Status, string P01, string P02)> Progression { get; }
            = new List<(EAnalyticsProgressionStatus, string, string)>();

        /// <summary>Purchase revenue pairs recorded in call order (currency, price).</summary>
        public List<(string Currency, float Price)> Purchases { get; } = new List<(string, float)>();

        /// <summary>Ad revenue pairs recorded in call order (currency, price).</summary>
        public List<(string Currency, float Price)> Ads { get; } = new List<(string, float)>();

        /// <inheritdoc />
        public void Init() { }

        /// <inheritdoc />
        public void TrackInfo(string eventName) => Info.Add(eventName);

        /// <inheritdoc />
        public void TrackInfo(string eventName, string paramName, string paramValue) => Info.Add(eventName);

        /// <inheritdoc />
        public void TrackInfo(string eventName, IReadOnlyList<string> paramNames, IReadOnlyList<string> paramValues) => Info.Add(eventName);

        /// <inheritdoc />
        public void TrackError(string error) => Errors.Add(error);

        /// <inheritdoc />
        public void TrackProgression(EAnalyticsProgressionStatus status, string progression01, string progression02 = null)
        {
            Progression.Add((status, progression01, progression02));
        }

        /// <inheritdoc />
        public void TrackPurchaseRevenue(string currency, float price) => Purchases.Add((currency, price));

        /// <inheritdoc />
        public void TrackAdsRevenue(string currency, float price) => Ads.Add((currency, price));
    }
}
