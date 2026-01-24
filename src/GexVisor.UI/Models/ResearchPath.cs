namespace GexVisor.UI.Models;

/// <summary>
/// Research path status indicating implementation state.
/// </summary>
public enum ResearchStatus
{
    Implemented,
    Partial,
    Deferred,
    Abandoned,
    Blocked,
    Infeasible,
    Superseded
}

/// <summary>
/// Taxonomy classification for research approaches.
/// </summary>
public enum ResearchTaxonomy
{
    Mechanical,
    Probabilistic,
    Narrative
}

/// <summary>
/// Quadrant/barrier type for research paths on the radar.
/// </summary>
public enum ResearchQuadrant
{
    Data,
    Knowledge,
    Scope,
    Methodology,
    Compute,
    None
}

/// <summary>
/// Represents a research path in the complexity map.
/// Maps barrier type, complexity level, and status for research scope visualization.
/// </summary>
public class ResearchPath
{
    public required string Id { get; init; }
    public int Ring { get; init; }
    public ResearchQuadrant Quadrant { get; init; }
    public int Angle { get; init; }
    public required string Label { get; init; }
    public string? LabelLong { get; init; }
    public ResearchStatus Status { get; init; }
    public ResearchTaxonomy Taxonomy { get; init; }
    public required string Title { get; init; }
    public string? Paper { get; init; }
    public string? IssueUrl { get; init; }
    public string? IssueNum { get; init; }
    public required string Description { get; init; }
    public required string Barrier { get; init; }
    public string? Unblock { get; init; }
    public ResearchQuadrant BarrierQuadrant { get; init; }
    public List<string> Related { get; init; } = [];
}

/// <summary>
/// Radar visualization constants for SVG rendering.
/// </summary>
public static class RadarConstants
{
    /// <summary>Ring radii for concentric circles (ring 0 center, ring 5 outermost).</summary>
    public static readonly int[] RingRadii = [0, 55, 105, 155, 210, 265, 315];

    /// <summary>Angle ranges for each quadrant in degrees (clockwise from top).</summary>
    public static readonly Dictionary<ResearchQuadrant, (int Start, int End)> QuadrantAngles = new()
    {
        [ResearchQuadrant.Data] = (Start: 0, End: 90),
        [ResearchQuadrant.Knowledge] = (Start: 90, End: 180),
        [ResearchQuadrant.Scope] = (Start: 180, End: 270),
        [ResearchQuadrant.Methodology] = (Start: 270, End: 360),
        [ResearchQuadrant.Compute] = (Start: 315, End: 360),  // Shares space with methodology
        [ResearchQuadrant.None] = (Start: 0, End: 90)
    };

    /// <summary>Foundation stats from the validated core research.</summary>
    public static class FoundationStats
    {
        public const string DetectionRate = "71.5%";
        public const string Accuracy = "91.2%";
        public const int TradingDays = 242;
    }
}

public static class ResearchPathData
{
    public static readonly string[] RingNames =
    [
        "Validated Core",
        "Minor Extensions",
        "Statistical Expertise",
        "Infrastructure Required",
        "Expensive/Rare Data",
        "Theoretical Barriers"
    ];

    public static readonly Dictionary<ResearchQuadrant, (string Label, string Color)> Quadrants = new()
    {
        [ResearchQuadrant.Data] = ("DATA ACCESS", "#e74c3c"),
        [ResearchQuadrant.Knowledge] = ("DOMAIN KNOWLEDGE", "#9b59b6"),
        [ResearchQuadrant.Scope] = ("SCOPE / FOCUS", "#3498db"),
        [ResearchQuadrant.Methodology] = ("METHODOLOGY", "#f39c12"),
        [ResearchQuadrant.Compute] = ("COMPUTE", "#2ecc71"),
        [ResearchQuadrant.None] = ("NONE", "#888")
    };

    public static readonly Dictionary<ResearchStatus, string> StatusColors = new()
    {
        [ResearchStatus.Implemented] = "#2ecc71",
        [ResearchStatus.Partial] = "#f39c12",
        [ResearchStatus.Deferred] = "#3498db",
        [ResearchStatus.Abandoned] = "#9b59b6",
        [ResearchStatus.Blocked] = "#e67e22",
        [ResearchStatus.Infeasible] = "#e74c3c",
        [ResearchStatus.Superseded] = "#1abc9c"
    };

    public static readonly Dictionary<ResearchTaxonomy, (string Bg, string Text)> TaxonomyColors = new()
    {
        [ResearchTaxonomy.Mechanical] = ("#2ecc71", "#000"),
        [ResearchTaxonomy.Probabilistic] = ("#f39c12", "#000"),
        [ResearchTaxonomy.Narrative] = ("#e74c3c", "#fff")
    };

    public static readonly ResearchPath[] Paths =
    [
        // Ring 0 - Core (Implemented)
        new()
        {
            Id = "core", Ring = 0, Quadrant = ResearchQuadrant.Scope, Angle = 0,
            Label = "Core", LabelLong = "GEX",
            Status = ResearchStatus.Implemented, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "Core GEX Analysis System",
            Paper = "Paper 1",
            Description = "GEX calculations, 15-pattern library, single LLM agent (MarketMechanicsAgent), O3-mini integration. WHO→WHOM→WHAT causal attribution framework.",
            Barrier = "None - this is the validated foundation achieving 71.5% detection and 91.2% accuracy",
            Unblock = null,
            BarrierQuadrant = ResearchQuadrant.None,
            Related = ["Pattern Library", "Obfuscation Testing", "WHO→WHOM→WHAT"]
        },
        // Ring 1 - Minor Extensions
        new()
        {
            Id = "trailing", Ring = 1, Quadrant = ResearchQuadrant.Scope, Angle = 35,
            Label = "Trailing", LabelLong = "Stops",
            Status = ResearchStatus.Partial, Taxonomy = ResearchTaxonomy.Probabilistic,
            Title = "Dynamic Trailing Stops",
            Paper = "Paper 2+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/46",
            IssueNum = "#46",
            Description = "Adaptive position management that adjusts stops based on volatility and pattern confidence.",
            Barrier = "Trade execution is outside research scope - production trading belongs in AutoTrader-AgentEdge project",
            Unblock = "Post-PhD commercialization with dedicated trading system development",
            BarrierQuadrant = ResearchQuadrant.Scope,
            Related = ["Position Sizing", "Risk Management", "AutoTrader Integration"]
        },
        new()
        {
            Id = "sixcat", Ring = 1, Quadrant = ResearchQuadrant.Knowledge, Angle = 145,
            Label = "6-Cat", LabelLong = "Patterns",
            Status = ResearchStatus.Partial, Taxonomy = ResearchTaxonomy.Probabilistic,
            Title = "Six-Category Pattern Classification",
            Paper = "Paper 1",
            Description = "Gamma Trap, Gamma Squeeze, Vol Compression, Mean Reversion, Momentum, Neutral.",
            Barrier = "Each category requires dedicated historical event testing - validation bottleneck with limited time",
            Unblock = "Dedicated validation sprints for remaining 5 categories with curated historical events",
            BarrierQuadrant = ResearchQuadrant.Knowledge,
            Related = ["Pattern Taxonomy", "Historical Validation", "Win Rate Analysis"]
        },
        // Ring 2 - Statistical Expertise
        new()
        {
            Id = "montecarlo", Ring = 2, Quadrant = ResearchQuadrant.Compute, Angle = 10,
            Label = "Monte", LabelLong = "Carlo",
            Status = ResearchStatus.Partial, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "Monte Carlo & Permutation Testing",
            Paper = "Paper 1",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/11",
            IssueNum = "#11",
            Description = "Basic stats completed (Wilson CI, Sharpe ratio, Kelly Criterion). Missing: 10K+ permutation iterations.",
            Barrier = "Massive compute for 10K+ iterations; PhD timeline pressure made basic validation sufficient",
            Unblock = "Cloud compute budget for large-scale permutation testing",
            BarrierQuadrant = ResearchQuadrant.Compute,
            Related = ["Statistical Significance", "FDR Correction", "Regime Testing"]
        },
        new()
        {
            Id = "cpcv", Ring = 2, Quadrant = ResearchQuadrant.Knowledge, Angle = 75,
            Label = "CPCV", LabelLong = "",
            Status = ResearchStatus.Deferred, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "Combinatorial Purged Cross-Validation",
            Paper = "Paper 2+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/27",
            IssueNum = "#27",
            Description = "de Prado's AFML methodology. Proper time-series CV with purging and embargo periods.",
            Barrier = "Requires advanced ML validation expertise; with only 7-15 pattern instances per type, sophisticated CV provides marginal benefit",
            Unblock = "Scale to 100+ pattern instances; dedicated implementation sprint with ML expertise",
            BarrierQuadrant = ResearchQuadrant.Knowledge,
            Related = ["de Prado AFML Ch.7", "Time-Series CV", "Overfitting Prevention"]
        },
        new()
        {
            Id = "prefixspan", Ring = 2, Quadrant = ResearchQuadrant.Methodology, Angle = 140,
            Label = "Seq", LabelLong = "Mining",
            Status = ResearchStatus.Superseded, Taxonomy = ResearchTaxonomy.Probabilistic,
            Title = "Sequential Pattern Mining (PrefixSpan)",
            Paper = "Paper 1",
            Description = "Algorithmic approach using frequency analysis and support/confidence thresholds.",
            Barrier = "LLM-based detection became core thesis contribution; algorithmic mining relegated to potential baseline comparison",
            Unblock = "Revisit as comparative baseline in future work",
            BarrierQuadrant = ResearchQuadrant.Methodology,
            Related = ["PrefixSpan Algorithm", "Frequent Patterns", "Baseline Comparison"]
        },
        // Ring 3 - Infrastructure
        new()
        {
            Id = "multiagent", Ring = 3, Quadrant = ResearchQuadrant.Scope, Angle = 25,
            Label = "Multi", LabelLong = "Agent",
            Status = ResearchStatus.Abandoned, Taxonomy = ResearchTaxonomy.Narrative,
            Title = "Multi-Agent LLM Orchestration",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/20",
            IssueNum = "#20",
            Description = "Complex AutoGen system with specialized agents: DataRetrievalAgent, GEXAgent, PatternAgent.",
            Barrier = "Architecture analysis concluded: 75% win rate achieved with single agent. Multi-agent complexity not justified",
            Unblock = "Would reconsider only if single-agent hits performance ceiling requiring specialization",
            BarrierQuadrant = ResearchQuadrant.Scope,
            Related = ["AutoGen Framework", "Agent Architecture", "Complexity Analysis"]
        },
        new()
        {
            Id = "fewshot", Ring = 3, Quadrant = ResearchQuadrant.Compute, Angle = 90,
            Label = "Few", LabelLong = "Shot",
            Status = ResearchStatus.Abandoned, Taxonomy = ResearchTaxonomy.Narrative,
            Title = "LLM Few-Shot Training Pipeline",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/38",
            IssueNum = "#38",
            Description = "Full ML infrastructure: example library with quality scoring, context templates, prompt A/B testing.",
            Barrier = "Production infrastructure, not research contribution. Simple prompts proved sufficient",
            Unblock = "Post-PhD commercialization with dedicated ML engineering team",
            BarrierQuadrant = ResearchQuadrant.Compute,
            Related = ["Prompt Engineering", "Example Curation", "ML Pipeline"]
        },
        new()
        {
            Id = "forwardtest", Ring = 3, Quadrant = ResearchQuadrant.Scope, Angle = 155,
            Label = "Live", LabelLong = "Testing",
            Status = ResearchStatus.Abandoned, Taxonomy = ResearchTaxonomy.Narrative,
            Title = "Forward-Test Live Trading",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/39",
            IssueNum = "#39",
            Description = "Real-time paper trading: live data feeds, continuous GEX calculation, position management.",
            Barrier = "Production trading system exceeds research scope. Historical backtesting proved sufficient",
            Unblock = "Partnership with trading firm for live testing environment",
            BarrierQuadrant = ResearchQuadrant.Scope,
            Related = ["Paper Trading", "Real-Time Systems", "Production Infrastructure"]
        },
        // Ring 4 - Expensive Data
        new()
        {
            Id = "shortput", Ring = 4, Quadrant = ResearchQuadrant.Data, Angle = 45,
            Label = "Short", LabelLong = "Put Arb",
            Status = ResearchStatus.Abandoned, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "Short Put Arbitrage Detection",
            Paper = "N/A",
            Description = "Identify anomalous short put positioning indicating dealer hedging pressure.",
            Barrier = "Requires fill-side TAQ data ($10K+/year) to determine trade initiator",
            Unblock = "Academic data partnership (e.g., WRDS subscription) or institutional sponsorship",
            BarrierQuadrant = ResearchQuadrant.Data,
            Related = ["TAQ Data", "Order Flow Analysis", "Fill-Side Inference"]
        },
        new()
        {
            Id = "0dte", Ring = 4, Quadrant = ResearchQuadrant.Methodology, Angle = 115,
            Label = "0DTE", LabelLong = "Gamma",
            Status = ResearchStatus.Blocked, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "0DTE Intraday Gamma Dynamics",
            Paper = "Paper 3",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/130",
            IssueNum = "#130",
            Description = "Time-dependent dealer hedging when same-day expiry gamma concentrated.",
            Barrier = "Methodological conflict: adding time context breaks obfuscation testing",
            Unblock = "Develop relative-time obfuscation that preserves mechanics without revealing market hours",
            BarrierQuadrant = ResearchQuadrant.Methodology,
            Related = ["Intraday Analysis", "Gamma Decay", "Obfuscation Methodology"]
        },
        new()
        {
            Id = "crossasset", Ring = 4, Quadrant = ResearchQuadrant.Data, Angle = 185,
            Label = "Cross", LabelLong = "Asset",
            Status = ResearchStatus.Deferred, Taxonomy = ResearchTaxonomy.Probabilistic,
            Title = "Cross-Asset Dealer Hedging Networks",
            Paper = "Paper 4+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/132",
            IssueNum = "#132",
            Description = "Test methodology generalization: Treasury options (TLT), FX options, Commodities.",
            Barrier = "Each asset class requires separate literature base, data vendors, potentially years of domain expertise",
            Unblock = "Complete Papers 1-3 for credibility; identify multi-asset data vendors",
            BarrierQuadrant = ResearchQuadrant.Data,
            Related = ["Fixed Income", "FX Markets", "Methodology Generalization"]
        },
        // Ring 5 - Theoretical Barriers
        new()
        {
            Id = "thirdorder", Ring = 5, Quadrant = ResearchQuadrant.Knowledge, Angle = 50,
            Label = "3rd", LabelLong = "Greeks",
            Status = ResearchStatus.Infeasible, Taxonomy = ResearchTaxonomy.Narrative,
            Title = "Third-Order Greeks (Speed, Zomma, Color)",
            Paper = "N/A",
            Description = "Higher-order sensitivities: Speed (DgammaDspot), Zomma (DgammaDvol), Color (DgammaDtime).",
            Barrier = "Signal-to-noise ratio makes third-order derivatives unmeasurable with daily data",
            Unblock = "Would require tick-level data with sub-second precision AND academic literature establishing predictive value",
            BarrierQuadrant = ResearchQuadrant.Knowledge,
            Related = ["Higher-Order Greeks", "Numerical Stability", "Practitioner Relevance"]
        },
        new()
        {
            Id = "volgreeks", Ring = 5, Quadrant = ResearchQuadrant.Data, Angle = 130,
            Label = "Vol", LabelLong = "Greeks",
            Status = ResearchStatus.Infeasible, Taxonomy = ResearchTaxonomy.Narrative,
            Title = "Volatility Greeks (Vomma, Veta, Vanna)",
            Paper = "N/A",
            Description = "Second-order volatility sensitivities requiring full implied volatility surface modeling.",
            Barrier = "Requires real-time IV surface data (OptionMetrics ~$15K/year). High LLM hallucination risk",
            Unblock = "Partnership with volatility surface provider; separate research focus on vol trading",
            BarrierQuadrant = ResearchQuadrant.Data,
            Related = ["IV Surface Modeling", "OptionMetrics", "Volatility Trading"]
        },
        new()
        {
            Id = "partial1", Ring = 1, Quadrant = ResearchQuadrant.Methodology, Angle = 200,
            Label = "Win", LabelLong = "Rate",
            Status = ResearchStatus.Partial, Taxonomy = ResearchTaxonomy.Mechanical,
            Title = "Pattern Win Rate Validation",
            Paper = "Paper 1",
            Description = "Validating win rates for each pattern type across different market regimes.",
            Barrier = "Requires extensive historical data and dedicated validation effort",
            Unblock = "Systematic backtesting framework with regime-specific analysis",
            BarrierQuadrant = ResearchQuadrant.Methodology,
            Related = ["Backtesting", "Regime Analysis", "Statistical Validation"]
        }
    ];
}
