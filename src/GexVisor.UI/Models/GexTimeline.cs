namespace GexVisor.UI.Models;

/// <summary>
/// Represents a complete GEX timeline dataset for a symbol.
/// </summary>
public record GexTimeline
{
    public required string Symbol { get; init; }
    public required string AssetClass { get; init; }
    public required DateRange DateRange { get; init; }
    public required int Count { get; init; }
    public required List<GexDataPoint> Timeline { get; init; }

    /// <summary>
    /// Get the price range across the timeline
    /// </summary>
    public (decimal Min, decimal Max) GetPriceRange()
    {
        if (Timeline.Count == 0)
            return (0, 0);
        return (Timeline.Min(t => t.Price), Timeline.Max(t => t.Price));
    }

    /// <summary>
    /// Get the GEX range across the timeline
    /// </summary>
    public (decimal Min, decimal Max) GetGexRange()
    {
        if (Timeline.Count == 0)
            return (0, 0);
        return (Timeline.Min(t => t.Gex), Timeline.Max(t => t.Gex));
    }

    /// <summary>
    /// Analyze regime segments and transitions across the timeline.
    /// </summary>
    public RegimeAnalysisSummary AnalyzeRegimes()
    {
        if (Timeline.Count == 0)
        {
            return new RegimeAnalysisSummary
            {
                Symbol = Symbol,
                DateRange = $"{DateRange.Start} → {DateRange.End}",
                TotalDays = 0,
                Segments = [],
                Transitions = []
            };
        }

        var segments = new List<RegimeSegment>();
        var transitions = new List<RegimeTransition>();

        // Build segments
        var segmentStart = 0;
        var currentRegime = GammaRegimeExtensions.FromString(Timeline[0].Regime);

        for (int i = 1; i <= Timeline.Count; i++)
        {
            var isEnd = i == Timeline.Count;
            var regimeChanged = !isEnd && GammaRegimeExtensions.FromString(Timeline[i].Regime) != currentRegime;

            if (isEnd || regimeChanged)
            {
                var segmentEnd = isEnd ? i - 1 : i - 1;
                var segmentPoints = Timeline.Skip(segmentStart).Take(segmentEnd - segmentStart + 1).ToList();
                var duration = segmentPoints.Count;

                var startPrice = segmentPoints.First().Price;
                var endPrice = segmentPoints.Last().Price;
                var priceChange = endPrice - startPrice;
                var priceChangePercent = startPrice > 0 ? priceChange / startPrice * 100 : 0;

                // Calculate position as percentage of total timeline
                var startPosition = (double)segmentStart / Timeline.Count * 100;
                var widthPercent = (double)duration / Timeline.Count * 100;

                segments.Add(new RegimeSegment
                {
                    StartDate = segmentPoints.First().Date,
                    EndDate = segmentPoints.Last().Date,
                    Regime = currentRegime,
                    DurationDays = duration,
                    AverageGex = segmentPoints.Average(p => p.Gex),
                    MinGex = segmentPoints.Min(p => p.Gex),
                    MaxGex = segmentPoints.Max(p => p.Gex),
                    PriceChange = priceChange,
                    PriceChangePercent = priceChangePercent,
                    StartPosition = startPosition,
                    WidthPercent = widthPercent
                });

                if (regimeChanged)
                {
                    var newRegime = GammaRegimeExtensions.FromString(Timeline[i].Regime);
                    transitions.Add(new RegimeTransition
                    {
                        Date = Timeline[i].Date,
                        FromRegime = currentRegime,
                        ToRegime = newRegime,
                        GexAtTransition = Timeline[i].Gex,
                        PriceAtTransition = Timeline[i].Price,
                        DaysInPreviousRegime = duration,
                        Position = (double)i / Timeline.Count * 100
                    });

                    currentRegime = newRegime;
                    segmentStart = i;
                }
            }
        }

        // Calculate statistics
        var positiveSegments = segments.Where(s => s.Regime == GammaRegime.Positive).ToList();
        var negativeSegments = segments.Where(s => s.Regime == GammaRegime.Negative).ToList();

        return new RegimeAnalysisSummary
        {
            Symbol = Symbol,
            DateRange = $"{DateRange.Start} → {DateRange.End}",
            TotalDays = Timeline.Count,
            PositiveGammaDays = positiveSegments.Sum(s => s.DurationDays),
            NegativeGammaDays = negativeSegments.Sum(s => s.DurationDays),
            TotalTransitions = transitions.Count,
            AverageRegimeDuration = segments.Count > 0 ? Math.Round(segments.Average(s => s.DurationDays), 1) : 0,
            LongestPositiveStreak = positiveSegments.Count > 0 ? positiveSegments.Max(s => s.DurationDays) : 0,
            LongestNegativeStreak = negativeSegments.Count > 0 ? negativeSegments.Max(s => s.DurationDays) : 0,
            Segments = segments,
            Transitions = transitions
        };
    }
}

public record DateRange
{
    public required string Start { get; init; }
    public required string End { get; init; }
}
