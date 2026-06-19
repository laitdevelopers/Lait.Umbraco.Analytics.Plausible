using Lait.Umbraco.Analytics.Plausible.Models;

namespace Lait.Umbraco.Analytics.Plausible.Services;

public interface ITrackingService
{
    /// <summary>Aggregate site stats (+ top pages) for the dashboard.</summary>
    Task<TrackingStats> GetStatsAsync(string period, CancellationToken ct = default);

    /// <summary>Per-page stats for a single Umbraco document, resolved to its published URL.</summary>
    Task<PageTrackingStats> GetPageStatsAsync(Guid documentKey, string period, CancellationToken ct = default);
}
