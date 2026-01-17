namespace GexVisor.UI.Models;

/// <summary>
/// Represents an annotation on the GEX timeline for pattern discovery.
/// Used to create labeled training data for LLM experiments.
/// </summary>
public record PatternAnnotation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string StartDate { get; init; }  // YYYY-MM-DD
    public string? EndDate { get; init; }            // null for point annotation
    public required string PatternType { get; init; }
    public required string Taxonomy { get; init; }   // mechanical/probabilistic/narrative
    public required string Confidence { get; init; } // high/medium/low
    public string? Notes { get; init; }

    // Context captured at time of annotation
    public decimal PriceAtAnnotation { get; init; }
    public decimal GexAtAnnotation { get; init; }
    public bool IsNegativeGamma { get; init; }

    // Outcome (filled in later)
    public string? Outcome { get; init; }    // "confirmed" | "invalidated"
    public decimal? PriceMove { get; init; }

    /// <summary>
    /// Indicates if this is a range annotation (vs point annotation).
    /// </summary>
    public bool IsRange => EndDate != null;

    /// <summary>
    /// Gets the display date(s) for the annotation.
    /// </summary>
    public string DateDisplay => IsRange ? $"{StartDate} → {EndDate}" : StartDate;
}

/// <summary>
/// Pattern taxonomy classifications from gex-llm-patterns research.
/// </summary>
public static class PatternTaxonomy
{
    public const string Mechanical = "MECH";     // Must occur, passes obfuscation
    public const string Probabilistic = "PROB";  // Statistical edge (>60%)
    public const string Narrative = "NARR";      // Folklore, fails obfuscation

    public static readonly IReadOnlyList<(string Code, string Label, string Description)> All =
    [
        (Mechanical, "Mechanical", "Must occur, passes obfuscation"),
        (Probabilistic, "Probabilistic", "Statistical edge (>60%)"),
        (Narrative, "Narrative", "Folklore, fails obfuscation")
    ];
}

/// <summary>
/// Pattern types that can be annotated.
/// </summary>
public static class PatternTypes
{
    public const string GammaFlipPositive = "gamma_flip_pos";
    public const string GammaFlipNegative = "gamma_flip_neg";
    public const string OpexPinning = "opex_pinning";
    public const string DealerSqueeze = "dealer_squeeze";
    public const string VolExpansion = "vol_expansion";
    public const string VolCompression = "vol_compression";
    public const string SupportTest = "support_test";
    public const string ResistanceTest = "resistance_test";
    public const string TrendContinuation = "trend_continuation";
    public const string Other = "other";

    public static readonly IReadOnlyList<(string Code, string Label)> All =
    [
        (GammaFlipPositive, "Gamma Flip (+ to -)"),
        (GammaFlipNegative, "Gamma Flip (- to +)"),
        (OpexPinning, "OPEX Pinning"),
        (DealerSqueeze, "Dealer Squeeze"),
        (VolExpansion, "Vol Expansion"),
        (VolCompression, "Vol Compression"),
        (SupportTest, "Support Test"),
        (ResistanceTest, "Resistance Test"),
        (TrendContinuation, "Trend Continuation"),
        (Other, "Other")
    ];
}

/// <summary>
/// Confidence levels for annotations.
/// </summary>
public static class ConfidenceLevels
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";

    public static readonly IReadOnlyList<(string Code, string Label)> All =
    [
        (High, "High"),
        (Medium, "Medium"),
        (Low, "Low")
    ];
}

/// <summary>
/// Outcome classifications for completed annotations.
/// </summary>
public static class AnnotationOutcomes
{
    public const string Confirmed = "confirmed";
    public const string Invalidated = "invalidated";
    public const string Pending = "pending";

    public static readonly IReadOnlyList<(string Code, string Label)> All =
    [
        (Confirmed, "Confirmed"),
        (Invalidated, "Invalidated"),
        (Pending, "Pending")
    ];
}
