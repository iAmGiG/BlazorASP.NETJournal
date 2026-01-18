using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for calculating cross-asset correlation and analysis metrics.
/// Implements statistical methods for comparing multiple assets.
/// </summary>
public class ComparisonAnalysisService
{
    /// <summary>
    /// Calculate pairwise correlation metrics between two assets.
    /// </summary>
    public CorrelationMetrics CalculateCorrelation(
        AssetComparisonData asset1,
        AssetComparisonData asset2)
    {
        // Find overlapping date range
        var overlap = FindDateOverlap(asset1.Timeline, asset2.Timeline);
        var data1 = FilterByDateRange(asset1.Timeline.Timeline, overlap);
        var data2 = FilterByDateRange(asset2.Timeline.Timeline, overlap);

        return new CorrelationMetrics
        {
            Symbol1 = asset1.Symbol,
            Symbol2 = asset2.Symbol,
            PriceCorrelation = CalculatePearsonCorrelation(
                data1.Select(d => d.Price).ToList(),
                data2.Select(d => d.Price).ToList()
            ),
            GexCorrelation = CalculatePearsonCorrelation(
                data1.Select(d => d.Gex).ToList(),
                data2.Select(d => d.Gex).ToList()
            ),
            RegimeAlignment = CalculateRegimeAlignment(data1, data2),
            RegimeFlipCorrelation = CalculateRegimeFlipCorrelation(
                asset1.RegimeAnalysis,
                asset2.RegimeAnalysis
            ),
            PerformanceDispersion = CalculatePerformanceDispersion(data1, data2)
        };
    }

    /// <summary>
    /// Generate complete cross-asset summary with all correlations.
    /// </summary>
    public CrossAssetSummary GenerateSummary(List<AssetComparisonData> assets)
    {
        var correlations = new List<CorrelationMetrics>();

        // Calculate pairwise correlations
        for (int i = 0; i < assets.Count; i++)
        {
            for (int j = i + 1; j < assets.Count; j++)
            {
                correlations.Add(CalculateCorrelation(assets[i], assets[j]));
            }
        }

        return new CrossAssetSummary
        {
            Assets = assets,
            Correlations = correlations,
            DispersionScore = CalculateDispersionScore(assets, correlations),
            CommonDateRange = FindCommonDateRange(assets),
            DivergenceEvents = FindDivergenceEvents(assets)
        };
    }

    /// <summary>
    /// Find divergence events (days where assets are in different regimes).
    /// </summary>
    public List<RegimeDivergenceEvent> FindDivergenceEvents(List<AssetComparisonData> assets)
    {
        // Find common date range
        var overlap = FindCommonDateRange(assets);
        var events = new List<RegimeDivergenceEvent>();

        // Get date-aligned data for each asset
        var assetData = assets.ToDictionary(
            a => a.Symbol,
            a => FilterByDateRange(a.Timeline.Timeline, overlap).ToDictionary(d => d.Date)
        );

        // Find all unique dates
        var allDates = assetData.Values
            .SelectMany(d => d.Keys)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        // Check each date for divergence
        foreach (var date in allDates)
        {
            var regimes = new Dictionary<string, GammaRegime>();
            var gexValues = new Dictionary<string, decimal>();
            var hasIndex = false;
            var hasStock = false;

            foreach (var asset in assets)
            {
                if (assetData[asset.Symbol].TryGetValue(date, out var dataPoint))
                {
                    var regime = dataPoint.IsNegativeGamma ? GammaRegime.Negative : GammaRegime.Positive;
                    regimes[asset.Symbol] = regime;
                    gexValues[asset.Symbol] = dataPoint.Gex;

                    if (asset.Timeline.AssetClass == "Index")
                        hasIndex = true;
                    else
                        hasStock = true;
                }
            }

            // Only consider days where we have data for all assets
            if (regimes.Count == assets.Count)
            {
                // Check if there's divergence (not all same regime)
                var uniqueRegimes = regimes.Values.Distinct().Count();
                if (uniqueRegimes > 1)
                {
                    events.Add(new RegimeDivergenceEvent
                    {
                        Date = date,
                        AssetRegimes = regimes,
                        AssetGexValues = gexValues,
                        IsDispersionOpportunity = hasIndex && hasStock
                    });
                }
            }
        }

        return events;
    }

    /// <summary>
    /// Calculate Pearson correlation coefficient between two data series.
    /// Returns value between -1 (perfect negative correlation) and 1 (perfect positive correlation).
    /// </summary>
    public decimal CalculatePearsonCorrelation(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count == 0)
        {
            return 0;
        }

        var n = x.Count;
        var avgX = x.Average();
        var avgY = y.Average();

        var covariance = x.Zip(y, (xi, yi) => (xi - avgX) * (yi - avgY)).Sum() / n;
        var stdX = Math.Sqrt((double)(x.Sum(xi => (xi - avgX) * (xi - avgX)) / n));
        var stdY = Math.Sqrt((double)(y.Sum(yi => (yi - avgY) * (yi - avgY)) / n));

        if (stdX * stdY == 0)
        {
            return 0; // No variation in one or both series
        }

        return (decimal)(covariance / (decimal)(stdX * stdY));
    }

    /// <summary>
    /// Calculate percentage of days both assets are in the same regime.
    /// </summary>
    private decimal CalculateRegimeAlignment(List<GexDataPoint> data1, List<GexDataPoint> data2)
    {
        if (data1.Count != data2.Count || data1.Count == 0)
        {
            return 0;
        }

        var sameRegime = data1.Zip(data2, (d1, d2) => d1.Regime == d2.Regime)
                              .Count(same => same);

        return (decimal)sameRegime / data1.Count * 100;
    }

    /// <summary>
    /// Calculate correlation between regime flip timing (transitions within 5 days).
    /// </summary>
    private decimal CalculateRegimeFlipCorrelation(
        RegimeAnalysisSummary regime1,
        RegimeAnalysisSummary regime2)
    {
        var transitions1 = regime1.Transitions.Select(t => DateTime.Parse(t.Date)).ToList();
        var transitions2 = regime2.Transitions.Select(t => DateTime.Parse(t.Date)).ToList();

        if (transitions1.Count == 0 && transitions2.Count == 0)
        {
            return 100; // Both have no transitions - perfect "correlation"
        }

        var maxFlips = Math.Max(transitions1.Count, transitions2.Count);
        if (maxFlips == 0)
        {
            return 0;
        }

        // Count how many flips occur within 5 days of each other
        var correlatedFlips = 0;
        foreach (var t1 in transitions1)
        {
            if (transitions2.Any(t2 => Math.Abs((t2 - t1).TotalDays) <= 5))
            {
                correlatedFlips++;
            }
        }

        return (decimal)correlatedFlips / maxFlips * 100;
    }

    /// <summary>
    /// Calculate performance dispersion (difference in volatility).
    /// </summary>
    private decimal CalculatePerformanceDispersion(List<GexDataPoint> data1, List<GexDataPoint> data2)
    {
        var returns1 = CalculateReturns(data1);
        var returns2 = CalculateReturns(data2);

        var vol1 = CalculateVolatility(returns1);
        var vol2 = CalculateVolatility(returns2);

        return Math.Abs(vol1 - vol2);
    }

    /// <summary>
    /// Calculate dispersion score for index vs stock comparison.
    /// Higher score indicates better dispersion trade setup.
    /// </summary>
    private decimal? CalculateDispersionScore(
        List<AssetComparisonData> assets,
        List<CorrelationMetrics> correlations)
    {
        // Identify index and stock assets
        var indexAssets = assets.Where(a => a.Timeline.AssetClass == "Index").ToList();
        var stockAssets = assets.Where(a => a.Timeline.AssetClass != "Index").ToList();

        if (indexAssets.Count == 0 || stockAssets.Count == 0)
        {
            return null; // Not a dispersion trade setup
        }

        // Get correlations between index and stocks
        var indexStockCorrelations = correlations
            .Where(c =>
                (indexAssets.Any(a => a.Symbol == c.Symbol1) && stockAssets.Any(a => a.Symbol == c.Symbol2)) ||
                (indexAssets.Any(a => a.Symbol == c.Symbol2) && stockAssets.Any(a => a.Symbol == c.Symbol1)))
            .ToList();

        if (indexStockCorrelations.Count == 0)
        {
            return null;
        }

        // Dispersion Score = (1 - Avg Price Correlation) * Avg Performance Dispersion * 100
        var avgCorr = indexStockCorrelations.Average(c => c.PriceCorrelation);
        var avgDisp = indexStockCorrelations.Average(c => c.PerformanceDispersion);

        return (1 - avgCorr) * avgDisp * 100;
    }

    /// <summary>
    /// Find overlapping date range between two timelines.
    /// </summary>
    private DateRangeOverlap FindDateOverlap(GexTimeline t1, GexTimeline t2)
    {
        var start1 = DateTime.Parse(t1.DateRange.Start);
        var end1 = DateTime.Parse(t1.DateRange.End);
        var start2 = DateTime.Parse(t2.DateRange.Start);
        var end2 = DateTime.Parse(t2.DateRange.End);

        var overlapStart = start1 > start2 ? start1 : start2;
        var overlapEnd = end1 < end2 ? end1 : end2;

        var totalDays = (overlapEnd - overlapStart).Days + 1;

        return new DateRangeOverlap
        {
            Start = overlapStart.ToString("yyyy-MM-dd"),
            End = overlapEnd.ToString("yyyy-MM-dd"),
            TotalDays = totalDays
        };
    }

    /// <summary>
    /// Find common date range across all assets (intersection).
    /// </summary>
    private DateRangeOverlap FindCommonDateRange(List<AssetComparisonData> assets)
    {
        if (assets.Count == 0)
        {
            return new DateRangeOverlap { Start = "", End = "", TotalDays = 0 };
        }

        var starts = assets.Select(a => DateTime.Parse(a.Timeline.DateRange.Start)).ToList();
        var ends = assets.Select(a => DateTime.Parse(a.Timeline.DateRange.End)).ToList();

        var overlapStart = starts.Max();
        var overlapEnd = ends.Min();

        var totalDays = (overlapEnd - overlapStart).Days + 1;

        return new DateRangeOverlap
        {
            Start = overlapStart.ToString("yyyy-MM-dd"),
            End = overlapEnd.ToString("yyyy-MM-dd"),
            TotalDays = Math.Max(0, totalDays)
        };
    }

    /// <summary>
    /// Filter timeline data to specified date range.
    /// </summary>
    private List<GexDataPoint> FilterByDateRange(List<GexDataPoint> timeline, DateRangeOverlap range)
    {
        return timeline
            .Where(d => string.Compare(d.Date, range.Start) >= 0 &&
                       string.Compare(d.Date, range.End) <= 0)
            .ToList();
    }

    /// <summary>
    /// Calculate daily returns from price data.
    /// </summary>
    private List<decimal> CalculateReturns(List<GexDataPoint> data)
    {
        var returns = new List<decimal>();

        for (int i = 1; i < data.Count; i++)
        {
            if (data[i - 1].Price != 0)
            {
                var ret = (data[i].Price - data[i - 1].Price) / data[i - 1].Price;
                returns.Add(ret);
            }
        }

        return returns;
    }

    /// <summary>
    /// Calculate annualized volatility from returns.
    /// </summary>
    private decimal CalculateVolatility(List<decimal> returns)
    {
        if (returns.Count == 0)
        {
            return 0;
        }

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / returns.Count;
        var dailyVol = (decimal)Math.Sqrt((double)variance);

        // Annualize (assuming 252 trading days)
        return dailyVol * (decimal)Math.Sqrt(252);
    }
}
