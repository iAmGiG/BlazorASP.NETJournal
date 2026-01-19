namespace GexVisor.UI.Models;

/// <summary>
/// Represents a research path in the complexity map.
/// Maps barrier type, complexity level, and status for research scope visualization.
/// </summary>
public class ResearchPath
{
    public required string Id { get; init; }
    public int Ring { get; init; }                  // 0-5 complexity level
    public required string Quadrant { get; init; }  // data, knowledge, scope, methodology
    public int Angle { get; init; }                 // Position within quadrant
    public required string Label { get; init; }
    public string? LabelLong { get; init; }
    public required string Status { get; init; }    // implemented, partial, deferred, abandoned, blocked, infeasible, superseded
    public required string Taxonomy { get; init; }  // mechanical, probabilistic, narrative
    public required string Title { get; init; }
    public string? Paper { get; init; }
    public string? IssueUrl { get; init; }
    public string? IssueNum { get; init; }
    public required string Description { get; init; }
    public required string Barrier { get; init; }
    public string? Unblock { get; init; }
    public required string BarrierType { get; init; }
    public List<string> Related { get; init; } = [];
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

    public static readonly Dictionary<string, (string Label, string Color)> Quadrants = new()
    {
        ["data"] = ("DATA ACCESS", "#e74c3c"),
        ["knowledge"] = ("DOMAIN KNOWLEDGE", "#9b59b6"),
        ["scope"] = ("SCOPE / FOCUS", "#3498db"),
        ["methodology"] = ("METHODOLOGY", "#f39c12"),
        ["compute"] = ("COMPUTE", "#2ecc71"),
        ["none"] = ("NONE", "#888")
    };

    public static readonly Dictionary<string, string> StatusColors = new()
    {
        ["implemented"] = "#2ecc71",
        ["partial"] = "#f39c12",
        ["deferred"] = "#3498db",
        ["abandoned"] = "#9b59b6",
        ["blocked"] = "#e67e22",
        ["infeasible"] = "#e74c3c",
        ["superseded"] = "#1abc9c"
    };

    public static readonly Dictionary<string, (string Bg, string Text)> TaxonomyColors = new()
    {
        ["mechanical"] = ("#2ecc71", "#000"),
        ["probabilistic"] = ("#f39c12", "#000"),
        ["narrative"] = ("#e74c3c", "#fff")
    };

    public static readonly ResearchPath[] Paths =
    [
        // Ring 0 - Core (Implemented)
        new()
        {
            Id = "core", Ring = 0, Quadrant = "scope", Angle = 0,
            Label = "Core", LabelLong = "GEX",
            Status = "implemented", Taxonomy = "mechanical",
            Title = "Core GEX Analysis System",
            Paper = "Paper 1",
            Description = "GEX calculations, 15-pattern library, single LLM agent (MarketMechanicsAgent), O3-mini integration. WHO→WHOM→WHAT causal attribution framework.",
            Barrier = "None - this is the validated foundation achieving 71.5% detection and 91.2% accuracy",
            Unblock = null,
            BarrierType = "none",
            Related = ["Pattern Library", "Obfuscation Testing", "WHO→WHOM→WHAT"]
        },
        // Ring 1 - Minor Extensions
        new()
        {
            Id = "trailing", Ring = 1, Quadrant = "scope", Angle = 35,
            Label = "Trailing", LabelLong = "Stops",
            Status = "partial", Taxonomy = "probabilistic",
            Title = "Dynamic Trailing Stops",
            Paper = "Paper 2+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/46",
            IssueNum = "#46",
            Description = "Adaptive position management that adjusts stops based on volatility and pattern confidence.",
            Barrier = "Trade execution is outside research scope - production trading belongs in AutoTrader-AgentEdge project",
            Unblock = "Post-PhD commercialization with dedicated trading system development",
            BarrierType = "scope",
            Related = ["Position Sizing", "Risk Management", "AutoTrader Integration"]
        },
        new()
        {
            Id = "sixcat", Ring = 1, Quadrant = "knowledge", Angle = 145,
            Label = "6-Cat", LabelLong = "Patterns",
            Status = "partial", Taxonomy = "probabilistic",
            Title = "Six-Category Pattern Classification",
            Paper = "Paper 1",
            Description = "Gamma Trap, Gamma Squeeze, Vol Compression, Mean Reversion, Momentum, Neutral.",
            Barrier = "Each category requires dedicated historical event testing - validation bottleneck with limited time",
            Unblock = "Dedicated validation sprints for remaining 5 categories with curated historical events",
            BarrierType = "knowledge",
            Related = ["Pattern Taxonomy", "Historical Validation", "Win Rate Analysis"]
        },
        // Ring 2 - Statistical Expertise
        new()
        {
            Id = "montecarlo", Ring = 2, Quadrant = "compute", Angle = 10,
            Label = "Monte", LabelLong = "Carlo",
            Status = "partial", Taxonomy = "mechanical",
            Title = "Monte Carlo & Permutation Testing",
            Paper = "Paper 1",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/11",
            IssueNum = "#11",
            Description = "Basic stats completed (Wilson CI, Sharpe ratio, Kelly Criterion). Missing: 10K+ permutation iterations.",
            Barrier = "Massive compute for 10K+ iterations; PhD timeline pressure made basic validation sufficient",
            Unblock = "Cloud compute budget for large-scale permutation testing",
            BarrierType = "compute",
            Related = ["Statistical Significance", "FDR Correction", "Regime Testing"]
        },
        new()
        {
            Id = "cpcv", Ring = 2, Quadrant = "knowledge", Angle = 75,
            Label = "CPCV", LabelLong = "",
            Status = "deferred", Taxonomy = "mechanical",
            Title = "Combinatorial Purged Cross-Validation",
            Paper = "Paper 2+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/27",
            IssueNum = "#27",
            Description = "de Prado's AFML methodology. Proper time-series CV with purging and embargo periods.",
            Barrier = "Requires advanced ML validation expertise; with only 7-15 pattern instances per type, sophisticated CV provides marginal benefit",
            Unblock = "Scale to 100+ pattern instances; dedicated implementation sprint with ML expertise",
            BarrierType = "knowledge",
            Related = ["de Prado AFML Ch.7", "Time-Series CV", "Overfitting Prevention"]
        },
        new()
        {
            Id = "prefixspan", Ring = 2, Quadrant = "methodology", Angle = 140,
            Label = "Seq", LabelLong = "Mining",
            Status = "superseded", Taxonomy = "probabilistic",
            Title = "Sequential Pattern Mining (PrefixSpan)",
            Paper = "Paper 1",
            Description = "Algorithmic approach using frequency analysis and support/confidence thresholds.",
            Barrier = "LLM-based detection became core thesis contribution; algorithmic mining relegated to potential baseline comparison",
            Unblock = "Revisit as comparative baseline in future work",
            BarrierType = "methodology",
            Related = ["PrefixSpan Algorithm", "Frequent Patterns", "Baseline Comparison"]
        },
        // Ring 3 - Infrastructure
        new()
        {
            Id = "multiagent", Ring = 3, Quadrant = "scope", Angle = 25,
            Label = "Multi", LabelLong = "Agent",
            Status = "abandoned", Taxonomy = "narrative",
            Title = "Multi-Agent LLM Orchestration",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/20",
            IssueNum = "#20",
            Description = "Complex AutoGen system with specialized agents: DataRetrievalAgent, GEXAgent, PatternAgent.",
            Barrier = "Architecture analysis concluded: 75% win rate achieved with single agent. Multi-agent complexity not justified",
            Unblock = "Would reconsider only if single-agent hits performance ceiling requiring specialization",
            BarrierType = "scope",
            Related = ["AutoGen Framework", "Agent Architecture", "Complexity Analysis"]
        },
        new()
        {
            Id = "fewshot", Ring = 3, Quadrant = "compute", Angle = 90,
            Label = "Few", LabelLong = "Shot",
            Status = "abandoned", Taxonomy = "narrative",
            Title = "LLM Few-Shot Training Pipeline",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/38",
            IssueNum = "#38",
            Description = "Full ML infrastructure: example library with quality scoring, context templates, prompt A/B testing.",
            Barrier = "Production infrastructure, not research contribution. Simple prompts proved sufficient",
            Unblock = "Post-PhD commercialization with dedicated ML engineering team",
            BarrierType = "compute",
            Related = ["Prompt Engineering", "Example Curation", "ML Pipeline"]
        },
        new()
        {
            Id = "forwardtest", Ring = 3, Quadrant = "scope", Angle = 155,
            Label = "Live", LabelLong = "Testing",
            Status = "abandoned", Taxonomy = "narrative",
            Title = "Forward-Test Live Trading",
            Paper = "N/A",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/39",
            IssueNum = "#39",
            Description = "Real-time paper trading: live data feeds, continuous GEX calculation, position management.",
            Barrier = "Production trading system exceeds research scope. Historical backtesting proved sufficient",
            Unblock = "Partnership with trading firm for live testing environment",
            BarrierType = "scope",
            Related = ["Paper Trading", "Real-Time Systems", "Production Infrastructure"]
        },
        // Ring 4 - Expensive Data
        new()
        {
            Id = "shortput", Ring = 4, Quadrant = "data", Angle = 45,
            Label = "Short", LabelLong = "Put Arb",
            Status = "abandoned", Taxonomy = "mechanical",
            Title = "Short Put Arbitrage Detection",
            Paper = "N/A",
            Description = "Identify anomalous short put positioning indicating dealer hedging pressure.",
            Barrier = "Requires fill-side TAQ data ($10K+/year) to determine trade initiator",
            Unblock = "Academic data partnership (e.g., WRDS subscription) or institutional sponsorship",
            BarrierType = "data",
            Related = ["TAQ Data", "Order Flow Analysis", "Fill-Side Inference"]
        },
        new()
        {
            Id = "0dte", Ring = 4, Quadrant = "methodology", Angle = 115,
            Label = "0DTE", LabelLong = "Gamma",
            Status = "blocked", Taxonomy = "mechanical",
            Title = "0DTE Intraday Gamma Dynamics",
            Paper = "Paper 3",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/130",
            IssueNum = "#130",
            Description = "Time-dependent dealer hedging when same-day expiry gamma concentrated.",
            Barrier = "Methodological conflict: adding time context breaks obfuscation testing",
            Unblock = "Develop relative-time obfuscation that preserves mechanics without revealing market hours",
            BarrierType = "methodology",
            Related = ["Intraday Analysis", "Gamma Decay", "Obfuscation Methodology"]
        },
        new()
        {
            Id = "crossasset", Ring = 4, Quadrant = "data", Angle = 185,
            Label = "Cross", LabelLong = "Asset",
            Status = "deferred", Taxonomy = "probabilistic",
            Title = "Cross-Asset Dealer Hedging Networks",
            Paper = "Paper 4+",
            IssueUrl = "https://github.com/iAmGiG/gex-llm-patterns/issues/132",
            IssueNum = "#132",
            Description = "Test methodology generalization: Treasury options (TLT), FX options, Commodities.",
            Barrier = "Each asset class requires separate literature base, data vendors, potentially years of domain expertise",
            Unblock = "Complete Papers 1-3 for credibility; identify multi-asset data vendors",
            BarrierType = "data",
            Related = ["Fixed Income", "FX Markets", "Methodology Generalization"]
        },
        // Ring 5 - Theoretical Barriers
        new()
        {
            Id = "thirdorder", Ring = 5, Quadrant = "knowledge", Angle = 50,
            Label = "3rd", LabelLong = "Greeks",
            Status = "infeasible", Taxonomy = "narrative",
            Title = "Third-Order Greeks (Speed, Zomma, Color)",
            Paper = "N/A",
            Description = "Higher-order sensitivities: Speed (DgammaDspot), Zomma (DgammaDvol), Color (DgammaDtime).",
            Barrier = "Signal-to-noise ratio makes third-order derivatives unmeasurable with daily data",
            Unblock = "Would require tick-level data with sub-second precision AND academic literature establishing predictive value",
            BarrierType = "knowledge",
            Related = ["Higher-Order Greeks", "Numerical Stability", "Practitioner Relevance"]
        },
        new()
        {
            Id = "volgreeks", Ring = 5, Quadrant = "data", Angle = 130,
            Label = "Vol", LabelLong = "Greeks",
            Status = "infeasible", Taxonomy = "narrative",
            Title = "Volatility Greeks (Vomma, Veta, Vanna)",
            Paper = "N/A",
            Description = "Second-order volatility sensitivities requiring full implied volatility surface modeling.",
            Barrier = "Requires real-time IV surface data (OptionMetrics ~$15K/year). High LLM hallucination risk",
            Unblock = "Partnership with volatility surface provider; separate research focus on vol trading",
            BarrierType = "data",
            Related = ["IV Surface Modeling", "OptionMetrics", "Volatility Trading"]
        },
        new()
        {
            Id = "partial1", Ring = 1, Quadrant = "methodology", Angle = 200,
            Label = "Win", LabelLong = "Rate",
            Status = "partial", Taxonomy = "mechanical",
            Title = "Pattern Win Rate Validation",
            Paper = "Paper 1",
            Description = "Validating win rates for each pattern type across different market regimes.",
            Barrier = "Requires extensive historical data and dedicated validation effort",
            Unblock = "Systematic backtesting framework with regime-specific analysis",
            BarrierType = "methodology",
            Related = ["Backtesting", "Regime Analysis", "Statistical Validation"]
        }
    ];
}
