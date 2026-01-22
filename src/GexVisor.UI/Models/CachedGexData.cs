using GexVisor.Core;
using GexVisor.UI.Configuration;

namespace GexVisor.UI.Models;

/// <summary>
/// Wrapper for cached GEX calculation results with staleness tracking.
/// Used for offline persistence of live market data.
/// </summary>
public record CachedGexData
{
    /// <summary>
    /// The cached GEX calculation result.
    /// </summary>
    public required GexCalculationResult Data { get; init; }

    /// <summary>
    /// When this data was cached (UTC).
    /// </summary>
    public required DateTime CachedAt { get; init; }

    /// <summary>
    /// When the underlying data was fetched from API (UTC).
    /// </summary>
    public required DateTime FetchedAt { get; init; }

    /// <summary>
    /// Symbol this data is for.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Whether this data was loaded from cache (vs fresh API response).
    /// </summary>
    public bool IsCached { get; init; }

    /// <summary>
    /// Check if the cached data is stale based on market hours.
    /// During market hours: stale after 5 minutes.
    /// After hours/weekends: stale after 24 hours.
    /// </summary>
    public bool IsStale => CalculateStaleness();

    private bool CalculateStaleness()
    {
        var age = DateTime.UtcNow - FetchedAt;
        var ttl = MarketHoursHelper.IsMarketOpen()
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.FromHours(24);
        return age > ttl;
    }
}
