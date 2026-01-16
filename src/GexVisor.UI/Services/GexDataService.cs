using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for loading GEX data from JSON files.
/// Handles index loading, symbol data fetching, and data transformation.
/// </summary>
public class GexDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    private GexIndex? _index;

    public GexIndex? Index => _index;

    public GexDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Load the index of available symbols.
    /// </summary>
    public async Task<GexIndex?> LoadIndexAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("data/index.json");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            _index = JsonSerializer.Deserialize<GexIndex>(json, _jsonOptions);
            return _index;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load data index: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load timeline data for a specific symbol.
    /// </summary>
    public async Task<GexTimeline?> LoadSymbolAsync(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"data/{symbol.ToLower()}.json");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Data not found for {symbol}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var rawTimeline = JsonSerializer.Deserialize<RawGexTimeline>(json, _jsonOptions);
            if (rawTimeline == null)
                return null;

            return TransformTimeline(rawTimeline);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load symbol data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Transform raw JSON data to our domain model.
    /// </summary>
    private static GexTimeline TransformTimeline(RawGexTimeline raw)
    {
        var timeline = raw.Timeline.Select(point => new GexDataPoint
        {
            Date = point.Date,
            Price = point.Price,
            Gex = point.Gex,
            CallGex = point.CallGex,
            PutGex = point.PutGex,
            ZeroGamma = point.ZeroGamma,
            MaxGamma = point.MaxGamma,
            Regime = point.Regime,
            CallOi = point.CallOi,
            PutOi = point.PutOi,
            Contracts = point.Contracts,
            Quality = point.Quality,
            Label = GenerateLabel(point)
        }).ToList();

        return new GexTimeline
        {
            Symbol = raw.Symbol,
            AssetClass = raw.AssetClass,
            DateRange = new DateRange { Start = raw.DateRange.Start, End = raw.DateRange.End },
            Count = raw.Count,
            Timeline = timeline
        };
    }

    /// <summary>
    /// Generate a descriptive label for a data point.
    /// </summary>
    private static string GenerateLabel(RawGexDataPoint point)
    {
        var regime = point.Regime == "NEGATIVE_GAMMA" ? "Short γ" : "Long γ";
        var date = DateTime.Parse(point.Date);
        var month = date.ToString("MMM");
        return $"{month} {date.Year} - {regime}";
    }

    /// <summary>
    /// Get list of asset classes from the index.
    /// </summary>
    public IEnumerable<string> GetAssetClasses()
    {
        if (_index == null)
            return Enumerable.Empty<string>();
        return _index.AssetClasses.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Get symbols for a specific asset class.
    /// </summary>
    public IEnumerable<string> GetSymbolsForClass(string assetClass)
    {
        if (_index?.AssetClasses.TryGetValue(assetClass, out var symbols) == true)
            return symbols.OrderBy(s => s);
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Get all available symbols.
    /// </summary>
    public IEnumerable<string> GetAllSymbols()
    {
        if (_index == null)
            return Enumerable.Empty<string>();
        return _index.Symbols.Select(s => s.Symbol).OrderBy(s => s);
    }

    /// <summary>
    /// Get info for a specific symbol.
    /// </summary>
    public SymbolInfo? GetSymbolInfo(string symbol)
    {
        return _index?.Symbols.FirstOrDefault(s => s.Symbol == symbol);
    }

    #region Raw JSON Models (for deserialization)

    private record RawGexTimeline
    {
        public string Symbol { get; init; } = "";
        public string AssetClass { get; init; } = "";
        public RawDateRange DateRange { get; init; } = new();
        public int Count { get; init; }
        public List<RawGexDataPoint> Timeline { get; init; } = [];
    }

    private record RawDateRange
    {
        public string Start { get; init; } = "";
        public string End { get; init; } = "";
    }

    private record RawGexDataPoint
    {
        public string Date { get; init; } = "";
        public decimal Price { get; init; }
        public decimal Gex { get; init; }
        public decimal CallGex { get; init; }
        public decimal PutGex { get; init; }
        public decimal ZeroGamma { get; init; }
        public decimal MaxGamma { get; init; }
        public string Regime { get; init; } = "";
        public decimal CallOi { get; init; }
        public decimal PutOi { get; init; }
        public int Contracts { get; init; }
        public decimal Quality { get; init; }
    }

    #endregion
}
