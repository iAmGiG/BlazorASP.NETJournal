using GexVisor.UI.Models;

namespace GexVisor.UI.Models;

/// <summary>
/// Contains timeline and analysis data for a single asset in a cross-asset comparison.
/// </summary>
public record AssetComparisonData
{
    public required string Symbol { get; init; }
    public required GexTimeline Timeline { get; init; }
    public required RegimeAnalysisSummary RegimeAnalysis { get; init; }
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Correlation metrics between two assets.
/// </summary>
public record CorrelationMetrics
{
    public required string Symbol1 { get; init; }
    public required string Symbol2 { get; init; }

    /// <summary>
    /// Pearson correlation coefficient for price movements (-1 to 1).
    /// </summary>
    public decimal PriceCorrelation { get; init; }

    /// <summary>
    /// Pearson correlation coefficient for GEX values (-1 to 1).
    /// </summary>
    public decimal GexCorrelation { get; init; }

    /// <summary>
    /// Percentage of days both assets are in the same gamma regime (0-100).
    /// </summary>
    public decimal RegimeAlignment { get; init; }

    /// <summary>
    /// Percentage of regime flips that occur within 5 days of each other (0-100).
    /// Measures regime transition synchronization.
    /// </summary>
    public decimal RegimeFlipCorrelation { get; init; }

    /// <summary>
    /// Absolute difference in annualized volatility between the two assets.
    /// </summary>
    public decimal PerformanceDispersion { get; init; }
}

/// <summary>
/// Aggregate summary of a cross-asset comparison with all metrics.
/// </summary>
public record CrossAssetSummary
{
    public required List<AssetComparisonData> Assets { get; init; }
    public required List<CorrelationMetrics> Correlations { get; init; }

    /// <summary>
    /// Average price correlation across all asset pairs.
    /// </summary>
    public decimal AvgPriceCorrelation => Correlations.Count > 0
        ? Correlations.Average(c => c.PriceCorrelation)
        : 0;

    /// <summary>
    /// Average GEX correlation across all asset pairs.
    /// </summary>
    public decimal AvgGexCorrelation => Correlations.Count > 0
        ? Correlations.Average(c => c.GexCorrelation)
        : 0;

    /// <summary>
    /// Average regime alignment across all asset pairs.
    /// </summary>
    public decimal AvgRegimeAlignment => Correlations.Count > 0
        ? Correlations.Average(c => c.RegimeAlignment)
        : 0;

    /// <summary>
    /// Dispersion trade score (index vs components).
    /// Higher score indicates better dispersion trade opportunity.
    /// Null if no index/stock combination present.
    /// </summary>
    public decimal? DispersionScore { get; init; }

    /// <summary>
    /// Common date range across all assets (intersection).
    /// </summary>
    public required DateRangeOverlap CommonDateRange { get; init; }

    /// <summary>
    /// Divergence events where assets had different regimes on the same date.
    /// </summary>
    public List<RegimeDivergenceEvent> DivergenceEvents { get; init; } = new();
}

/// <summary>
/// Represents the overlapping date range between multiple timelines.
/// </summary>
public record DateRangeOverlap
{
    public required DateOnly Start { get; init; }
    public required DateOnly End { get; init; }
    public int TotalDays { get; init; }

    /// <summary>
    /// Check if the overlap is sufficient for meaningful analysis (>= 100 days recommended).
    /// </summary>
    public bool IsSufficient => TotalDays >= 100;

    /// <summary>
    /// Warning message if overlap is insufficient.
    /// </summary>
    public string? WarningMessage => !IsSufficient
        ? $"Limited data overlap ({TotalDays} days). Recommend >= 100 days for reliable analysis."
        : null;
}

/// <summary>
/// Represents a divergence event where assets have different gamma regimes on the same date.
/// </summary>
public record RegimeDivergenceEvent
{
    public required DateOnly Date { get; init; }
    public required Dictionary<string, GammaRegime> AssetRegimes { get; init; }
    public required Dictionary<string, decimal> AssetGexValues { get; init; }

    /// <summary>
    /// Magnitude of divergence (0-1, where 1 = maximum divergence).
    /// Calculated as the proportion of assets in different regimes.
    /// </summary>
    public decimal DivergenceMagnitude
    {
        get
        {
            if (AssetRegimes.Count <= 1)
                return 0;

            var positiveCount = AssetRegimes.Values.Count(r => r == GammaRegime.Positive);
            var negativeCount = AssetRegimes.Count - positiveCount;

            // Maximum divergence when split 50/50
            var split = Math.Min(positiveCount, negativeCount);
            return (decimal)split / AssetRegimes.Count;
        }
    }

    /// <summary>
    /// Check if this is a dispersion trade opportunity (index vs stocks in different regimes).
    /// </summary>
    public bool IsDispersionOpportunity { get; init; }
}
