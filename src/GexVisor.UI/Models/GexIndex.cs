using System.Text.Json.Serialization;

namespace GexVisor.UI.Models;

/// <summary>
/// Represents the index of available GEX data symbols.
/// </summary>
public record GexIndex
{
    [JsonPropertyName("asset_classes")]
    public required Dictionary<string, List<string>> AssetClasses { get; init; }

    [JsonPropertyName("symbols")]
    public required List<SymbolInfo> Symbols { get; init; }
}

public record SymbolInfo
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }

    [JsonPropertyName("asset_class")]
    public required string AssetClass { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("date_range")]
    public required DateRange DateRange { get; init; }

    // Convenience properties for backward compatibility
    public string Start => DateRange.Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    public string End => DateRange.End.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}

