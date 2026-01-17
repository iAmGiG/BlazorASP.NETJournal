namespace GexVisor.UI.Models;

/// <summary>
/// Application settings persisted to localStorage.
/// </summary>
public record AppSettings
{
    public int PlaybackSpeed { get; init; } = 2;
    public decimal YAxisScale { get; init; } = 1.0m;
    public decimal XAxisScale { get; init; } = 1.0m;
    public string? LastSymbol { get; init; }
    public string? LastAssetClass { get; init; }
    public DataMode LastDataMode { get; init; } = DataMode.Demo;
    public bool InvertScroll { get; init; } = false;
}
