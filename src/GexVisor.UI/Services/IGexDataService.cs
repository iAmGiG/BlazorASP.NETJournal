using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Interface for loading GEX data from JSON files.
/// Enables dependency injection and unit testing.
/// </summary>
public interface IGexDataService
{
    /// <summary>
    /// Gets the loaded index of available symbols.
    /// </summary>
    GexIndex? Index { get; }

    /// <summary>
    /// Gets whether real data files are available and loaded.
    /// </summary>
    bool IsRealDataAvailable { get; }

    /// <summary>
    /// Load the index of available symbols.
    /// </summary>
    Task<GexIndex?> LoadIndexAsync();

    /// <summary>
    /// Load timeline data for a specific symbol.
    /// </summary>
    Task<GexTimeline?> LoadSymbolAsync(string symbol);

    /// <summary>
    /// Gets a list of unique asset classes from the index.
    /// </summary>
    IEnumerable<string> GetAssetClasses();

    /// <summary>
    /// Gets all symbols for a specific asset class.
    /// </summary>
    IEnumerable<string> GetSymbolsForClass(string assetClass);

    /// <summary>
    /// Gets symbol information for a specific symbol.
    /// </summary>
    SymbolInfo? GetSymbolInfo(string symbol);
}
