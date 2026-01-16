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
}

public record DateRange
{
    public required string Start { get; init; }
    public required string End { get; init; }
}
