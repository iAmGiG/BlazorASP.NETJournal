namespace GexVisor.UI.Models;

/// <summary>
/// Application state for the GEX visualizer.
/// Mirrors the state object from state.js in the original implementation.
/// </summary>
public class GexState
{
    // Current values
    public decimal Price { get; set; } = 400m;
    public decimal OpenInterest { get; set; } = 5m;
    public decimal Tilt { get; set; } = -0.15m;

    // Simulation state
    public bool IsSimulating { get; set; }
    public int SimFrame { get; set; }
    public int PersistenceDay { get; set; } = 1;
    public int CurrentIndex { get; set; } = -1;
    public int PlaybackSpeed { get; set; } = 2;

    // Interaction state
    public bool IsDragging { get; set; }
    public bool IsResizing { get; set; }
    public decimal YAxisScale { get; set; } = 1.0m;
    public decimal XAxisScale { get; set; } = 1.0m;
    public bool InvertScroll { get; set; }

    // Data mode
    public DataMode Mode { get; set; } = DataMode.Demo;
    public string? CurrentSymbol { get; set; }
    public string? CurrentAssetClass { get; set; }

    // Strike range (calculated)
    public decimal StrikeStart { get; set; } = 280m;
    public decimal StrikeEnd { get; set; } = 650m;
    public decimal StrikeStep { get; set; } = 10m;

    /// <summary>
    /// Current data point being displayed
    /// </summary>
    public GexDataPoint? CurrentDataPoint { get; set; }

    /// <summary>
    /// Calculate S² factor (spot price squared scaling)
    /// </summary>
    public decimal S2Factor => Price * Price / 160000m; // Normalized to $400 base
}

public enum DataMode
{
    Demo,
    Real
}
