namespace GexVisor.UI.Models;

/// <summary>
/// Represents a single GEX (Gamma Exposure) data point from the timeline.
/// </summary>
public record GexDataPoint
{
    public required DateOnly Date { get; init; }
    public required decimal Price { get; init; }
    public required decimal Gex { get; init; }
    public required decimal CallGex { get; init; }
    public required decimal PutGex { get; init; }
    public required decimal ZeroGamma { get; init; }
    public required decimal MaxGamma { get; init; }
    public required string Regime { get; init; }
    public required decimal CallOi { get; init; }
    public required decimal PutOi { get; init; }
    public required int Contracts { get; init; }
    public required decimal Quality { get; init; }

    /// <summary>
    /// Optional label for display (e.g., "COVID Bottom", "Election Rally")
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Number of consecutive days in the current regime (for persistence tracking)
    /// </summary>
    public int RegimeDays { get; init; } = 1;

    /// <summary>
    /// Indicates if this is a negative gamma regime (dealers short gamma = amplifies moves)
    /// </summary>
    public bool IsNegativeGamma => Regime == "NEGATIVE_GAMMA";

    /// <summary>
    /// Price relative to zero gamma level. Positive = above zero gamma.
    /// </summary>
    public decimal PriceRelativeToZeroGamma => Price - ZeroGamma;
}
