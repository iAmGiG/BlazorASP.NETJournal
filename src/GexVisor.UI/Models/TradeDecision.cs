namespace GexVisor.UI.Models;

/// <summary>
/// Represents system decision metadata for an autotrader trade.
/// Contains pattern information, regime context, and decision rationale.
/// </summary>
public class TradeDecision
{
    /// <summary>
    /// Unique identifier for this decision.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Link to the associated OptionsLog trade.
    /// </summary>
    public Guid TradeId { get; set; }

    // === Pattern Information ===

    /// <summary>
    /// List of pattern names that were active at decision time.
    /// Examples: "gamma_positioning", "0dte_hedging", "stock_pinning"
    /// </summary>
    public List<string> ActivePatterns { get; set; } = [];

    /// <summary>
    /// Primary pattern that triggered this trade.
    /// </summary>
    public string? PrimaryTrigger { get; set; }

    /// <summary>
    /// System confidence in this trade decision (0.0 to 1.0).
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Signal strengths for each detected pattern (pattern name → strength).
    /// </summary>
    public Dictionary<string, decimal> SignalStrengths { get; set; } = new();

    // === Regime Context ===

    /// <summary>
    /// Market regime classification at decision time.
    /// Examples: "High GEX", "Low GEX", "Transitional", "Negative Gamma"
    /// </summary>
    public string? RegimeType { get; set; }

    /// <summary>
    /// GEX level at decision time (dollars of gamma exposure per 1% move).
    /// </summary>
    public decimal? GexLevel { get; set; }

    /// <summary>
    /// Implied volatility level at decision time (%).
    /// </summary>
    public decimal? IvLevel { get; set; }

    /// <summary>
    /// Whether the market was in negative gamma regime.
    /// </summary>
    public bool? IsNegativeGamma { get; set; }

    /// <summary>
    /// Spot price at decision time.
    /// </summary>
    public decimal? SpotPrice { get; set; }

    // === System Reasoning ===

    /// <summary>
    /// Human-readable explanation of why this trade was made.
    /// </summary>
    public string? DecisionRationale { get; set; }

    /// <summary>
    /// Risk assessment and expected drawdown.
    /// </summary>
    public string? RiskAssessment { get; set; }

    /// <summary>
    /// Expected profit target or exit criteria.
    /// </summary>
    public string? ProfitTarget { get; set; }

    /// <summary>
    /// Additional context or metadata (JSON string for flexibility).
    /// </summary>
    public string? AdditionalContext { get; set; }

    // === Timestamps ===

    /// <summary>
    /// When the decision was made (UTC).
    /// </summary>
    public DateTime DecisionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this decision record was created/imported.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
