using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing multi-symbol state in cross-asset comparisons.
/// Handles parallel loading of multiple timelines and selection management.
/// </summary>
public class ComparisonService
{
    private readonly GexDataService _dataService;
    private List<string> _selectedSymbols = [];
    private Dictionary<string, AssetComparisonData> _loadedAssets = new();

    public event Action? OnSelectionChanged;

    public IReadOnlyList<string> SelectedSymbols => _selectedSymbols.AsReadOnly();
    public IReadOnlyDictionary<string, AssetComparisonData> LoadedAssets => _loadedAssets;

    public int SelectionCount => _selectedSymbols.Count;
    public bool IsValidSelection => SelectionCount >= 2 && SelectionCount <= 4;

    public ComparisonService(GexDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Load multiple symbols in parallel.
    /// Returns true if at least 2 symbols loaded successfully.
    /// </summary>
    public async Task<bool> LoadSymbolsAsync(List<string> symbols)
    {
        if (symbols.Count < 2 || symbols.Count > 4)
        {
            throw new ArgumentException("Must select between 2 and 4 symbols", nameof(symbols));
        }

        _loadedAssets.Clear();

        // Parallel loading
        var tasks = symbols.Select(async symbol =>
        {
            try
            {
                var timeline = await _dataService.LoadSymbolAsync(symbol);
                if (timeline == null)
                {
                    return (Symbol: symbol, Data: (AssetComparisonData?)null);
                }

                return (Symbol: symbol, Data: new AssetComparisonData
                {
                    Symbol = symbol,
                    Timeline = timeline,
                    RegimeAnalysis = timeline.AnalyzeRegimes(),
                    LoadedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
                // Failed to load this symbol
                return (Symbol: symbol, Data: (AssetComparisonData?)null);
            }
        });

        var results = await Task.WhenAll(tasks);

        // Only add successfully loaded symbols
        foreach (var (symbol, data) in results.Where(r => r.Data != null))
        {
            _loadedAssets[symbol] = data!;
        }

        _selectedSymbols = _loadedAssets.Keys.ToList();
        OnSelectionChanged?.Invoke();

        return _loadedAssets.Count >= 2;
    }

    /// <summary>
    /// Clear all selected symbols and loaded data.
    /// </summary>
    public void ClearSelection()
    {
        _selectedSymbols.Clear();
        _loadedAssets.Clear();
        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    /// Get data for a specific symbol.
    /// </summary>
    public AssetComparisonData? GetAsset(string symbol)
    {
        return _loadedAssets.GetValueOrDefault(symbol);
    }

    /// <summary>
    /// Toggle symbol selection (for checkbox UI).
    /// Does not load data - call LoadSymbolsAsync() after selection.
    /// </summary>
    public void ToggleSymbol(string symbol)
    {
        if (_selectedSymbols.Contains(symbol))
        {
            _selectedSymbols.Remove(symbol);
        }
        else
        {
            if (_selectedSymbols.Count < 4)
            {
                _selectedSymbols.Add(symbol);
            }
        }

        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    /// Check if a symbol is currently selected.
    /// </summary>
    public bool IsSymbolSelected(string symbol)
    {
        return _selectedSymbols.Contains(symbol);
    }

    /// <summary>
    /// Get validation message for current selection.
    /// Returns null if selection is valid.
    /// </summary>
    public string? GetValidationMessage()
    {
        return SelectionCount switch
        {
            0 => "Select at least 2 symbols to compare",
            1 => "Select at least 1 more symbol to compare",
            > 4 => "Maximum 4 symbols allowed",
            _ => null
        };
    }
}
