namespace GexVisor.UI.Models;

/// <summary>
/// Represents a continuous period of a single gamma regime.
/// Used for visualizing regime durations on a timeline.
/// </summary>
public record RegimeSegment
{
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public required GammaRegime Regime { get; init; }
    public required int DurationDays { get; init; }

    // Metrics during this segment
    public decimal AverageGex { get; init; }
    public decimal MinGex { get; init; }
    public decimal MaxGex { get; init; }
    public decimal PriceChange { get; init; }
    public decimal PriceChangePercent { get; init; }

    /// <summary>
    /// Position of this segment in the timeline (0-100%).
    /// Used for rendering.
    /// </summary>
    public double StartPosition { get; init; }
    public double WidthPercent { get; init; }
}

/// <summary>
/// Represents a transition point where the gamma regime changed.
/// </summary>
public record RegimeTransition
{
    public required string Date { get; init; }
    public required GammaRegime FromRegime { get; init; }
    public required GammaRegime ToRegime { get; init; }
    public decimal GexAtTransition { get; init; }
    public decimal PriceAtTransition { get; init; }

    /// <summary>
    /// Days in the previous regime before this transition.
    /// </summary>
    public int DaysInPreviousRegime { get; init; }

    /// <summary>
    /// Position of this transition in the timeline (0-100%).
    /// </summary>
    public double Position { get; init; }
}

/// <summary>
/// Summary statistics for regime analysis.
/// </summary>
public record RegimeAnalysisSummary
{
    public required string Symbol { get; init; }
    public required string DateRange { get; init; }
    public int TotalDays { get; init; }

    // Regime distribution
    public int PositiveGammaDays { get; init; }
    public int NegativeGammaDays { get; init; }
    public double PositiveGammaPercent => TotalDays > 0 ? Math.Round((double)PositiveGammaDays / TotalDays * 100, 1) : 0;
    public double NegativeGammaPercent => TotalDays > 0 ? Math.Round((double)NegativeGammaDays / TotalDays * 100, 1) : 0;

    // Transitions
    public int TotalTransitions { get; init; }
    public double AverageRegimeDuration { get; init; }
    public int LongestPositiveStreak { get; init; }
    public int LongestNegativeStreak { get; init; }

    // Segments
    public required IReadOnlyList<RegimeSegment> Segments { get; init; }
    public required IReadOnlyList<RegimeTransition> Transitions { get; init; }
}

/// <summary>
/// Gamma regime classification.
/// </summary>
public enum GammaRegime
{
    /// <summary>Net positive gamma - dealers dampen volatility.</summary>
    Positive,

    /// <summary>Net negative gamma - dealers amplify volatility.</summary>
    Negative
}

/// <summary>
/// Extension methods for GammaRegime.
/// </summary>
public static class GammaRegimeExtensions
{
    public static string ToLabel(this GammaRegime regime) => regime switch
    {
        GammaRegime.Positive => "Positive Gamma",
        GammaRegime.Negative => "Negative Gamma",
        _ => "Unknown"
    };

    public static string ToShortLabel(this GammaRegime regime) => regime switch
    {
        GammaRegime.Positive => "+γ",
        GammaRegime.Negative => "-γ",
        _ => "?"
    };

    public static string ToCssClass(this GammaRegime regime) => regime switch
    {
        GammaRegime.Positive => "regime-positive",
        GammaRegime.Negative => "regime-negative",
        _ => ""
    };

    public static GammaRegime FromString(string regime) =>
        regime == "NEGATIVE_GAMMA" ? GammaRegime.Negative : GammaRegime.Positive;
}
