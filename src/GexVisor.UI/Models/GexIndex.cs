namespace GexVisor.UI.Models;

/// <summary>
/// Represents the index of available GEX data symbols.
/// </summary>
public record GexIndex
{
    public required Dictionary<string, List<string>> AssetClasses { get; init; }
    public required List<SymbolInfo> Symbols { get; init; }
}

public record SymbolInfo
{
    public required string Symbol { get; init; }
    public required string AssetClass { get; init; }
    public required int Count { get; init; }
    public required string Start { get; init; }
    public required string End { get; init; }
}
