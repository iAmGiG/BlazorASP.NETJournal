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

    // Note: Research paths data is now loaded from wwwroot/data/research-paths.json
    // Use IResearchPathService to access the paths.
}
