using System.Globalization;
using System.Text;
using System.Text.Json;
using GexVisor.Core;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing trade logs from autotrader imports with analytics and export capabilities.
/// </summary>
public class TradeLogService
{
    private readonly ILocalStorageService _storage;
    private readonly string _storageKey = StorageKeys.TradeLogs;
    private List<OptionsLog> _trades = [];

    public event Action? OnTradesChanged;

    public IReadOnlyList<OptionsLog> AllTrades => _trades;
    public int Count => _trades.Count;

    public TradeLogService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    // === CRUD Operations ===

    /// <summary>
    /// Load trades from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<List<OptionsLog>>(_storageKey);
        _trades = stored ?? [];
        OnTradesChanged?.Invoke();
    }

    /// <summary>
    /// Save trades to localStorage.
    /// </summary>
    private async Task SaveAsync()
    {
        await _storage.SetAsync(_storageKey, _trades);
        OnTradesChanged?.Invoke();
    }

    /// <summary>
    /// Add a new trade.
    /// </summary>
    public async Task AddAsync(OptionsLog trade)
    {
        _trades.Insert(0, trade); // New trades at top
        await SaveAsync();
    }

    /// <summary>
    /// Add multiple trades (bulk import).
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<OptionsLog> trades)
    {
        _trades.InsertRange(0, trades);
        await SaveAsync();
    }

    /// <summary>
    /// Update an existing trade.
    /// </summary>
    public async Task UpdateAsync(OptionsLog updated)
    {
        var index = _trades.FindIndex(t => t.Id == updated.Id);
        if (index >= 0)
        {
            _trades[index] = updated;
            await SaveAsync();
        }
    }

    /// <summary>
    /// Delete a trade by ID.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        _trades.RemoveAll(t => t.Id == id);
        await SaveAsync();
    }

    /// <summary>
    /// Get a trade by ID.
    /// </summary>
    public OptionsLog? GetById(Guid id) => _trades.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Clear all trades.
    /// </summary>
    public async Task ClearAsync()
    {
        _trades.Clear();
        await SaveAsync();
    }

    // === Query Methods ===

    /// <summary>
    /// Get all trades sorted by creation date (newest first).
    /// </summary>
    public IEnumerable<OptionsLog> GetAllTrades()
    {
        return _trades.Where(t => t.CreatedDate != null)
            .OrderByDescending(t => t.CreatedDate!.Value);
    }

    /// <summary>
    /// Get trades by date range.
    /// </summary>
    public IEnumerable<OptionsLog> GetTradesByDateRange(DateTime start, DateTime end)
    {
        return _trades.Where(t => t.CreatedDate >= start && t.CreatedDate <= end)
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Get trades by symbol/ticker.
    /// </summary>
    public IEnumerable<OptionsLog> GetTradesBySymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return [];

        return _trades.Where(t => t.Ticker != null &&
            t.Ticker.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Get trades by type (BTO/STC/STO/BTC).
    /// </summary>
    public IEnumerable<OptionsLog> GetTradesByType(OptionsLog.TradeType type)
    {
        return _trades.Where(t => t.OptionTradeType == type)
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Get open trades (no exit price).
    /// </summary>
    public IEnumerable<OptionsLog> GetOpenTrades()
    {
        return _trades.Where(t => !t.ExitPrice.HasValue)
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Get closed trades (has exit price).
    /// </summary>
    public IEnumerable<OptionsLog> GetClosedTrades()
    {
        return _trades.Where(t => t.ExitPrice.HasValue)
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Filter trades with P&L in range (only closed trades).
    /// </summary>
    public IEnumerable<OptionsLog> FilterByPnLRange(decimal? minPnL, decimal? maxPnL)
    {
        var closedTrades = GetClosedTrades();

        if (minPnL.HasValue)
            closedTrades = closedTrades.Where(t => t.CalculatePnL() >= minPnL.Value);

        if (maxPnL.HasValue)
            closedTrades = closedTrades.Where(t => t.CalculatePnL() <= maxPnL.Value);

        return closedTrades;
    }

    /// <summary>
    /// Search trades by ticker, notes, or analysis.
    /// </summary>
    public IEnumerable<OptionsLog> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _trades;

        var lowerQuery = query.ToLowerInvariant();
        return _trades.Where(t =>
            (t.Ticker?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (t.Notes?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (t.Analysis?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(t => t.CreatedDate);
    }

    /// <summary>
    /// Get all unique symbols/tickers.
    /// </summary>
    public IEnumerable<string> GetAllSymbols()
    {
        return _trades.Where(t => !string.IsNullOrWhiteSpace(t.Ticker))
            .Select(t => t.Ticker!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s);
    }

    // === Analytics ===

    /// <summary>
    /// Calculate summary statistics for trades.
    /// </summary>
    public TradeLogSummary CalculateSummary(IEnumerable<OptionsLog>? trades = null)
    {
        var tradestoAnalyze = (trades ?? _trades).ToList();
        var closedTrades = tradestoAnalyze.Where(t => t.ExitPrice.HasValue).ToList();

        var totalPnL = closedTrades.Sum(t => t.CalculatePnL() ?? 0);
        var winningTrades = closedTrades.Where(t => (t.CalculatePnL() ?? 0) > 0).ToList();
        var losingTrades = closedTrades.Where(t => (t.CalculatePnL() ?? 0) < 0).ToList();

        return new TradeLogSummary
        {
            TotalTrades = tradestoAnalyze.Count,
            OpenTrades = tradestoAnalyze.Count(t => !t.ExitPrice.HasValue),
            ClosedTrades = closedTrades.Count,
            WinningTrades = winningTrades.Count,
            LosingTrades = losingTrades.Count,
            WinRate = closedTrades.Count > 0
                ? (decimal)winningTrades.Count / closedTrades.Count * 100
                : 0,
            TotalPnL = totalPnL,
            AveragePnL = closedTrades.Count > 0
                ? totalPnL / closedTrades.Count
                : 0,
            BestTrade = closedTrades.Count > 0
                ? closedTrades.Max(t => t.CalculatePnL() ?? 0)
                : 0,
            WorstTrade = closedTrades.Count > 0
                ? closedTrades.Min(t => t.CalculatePnL() ?? 0)
                : 0
        };
    }

    // === Export ===

    /// <summary>
    /// Export trades to JSON format.
    /// </summary>
    public string ExportToJson(IEnumerable<OptionsLog>? trades = null)
    {
        var tradesToExport = trades ?? _trades;
        return JsonSerializer.Serialize(tradesToExport, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Export trades to CSV format.
    /// </summary>
    public string ExportToCsv(IEnumerable<OptionsLog>? trades = null)
    {
        var tradesToExport = (trades ?? _trades).ToList();
        if (!tradesToExport.Any())
            return string.Empty;

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("Date,Symbol,Type,Entry,Exit,Quantity,Multiplier,Strike,Expiration,P&L,Notes,Analysis");

        // Rows
        foreach (var trade in tradesToExport.OrderBy(t => t.CreatedDate))
        {
            var date = trade.CreatedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            var symbol = EscapeCsv(trade.Ticker ?? "");
            var type = trade.OptionTradeType.ToString();
            var entry = trade.EntryPrice.ToString("F2", CultureInfo.InvariantCulture);
            var exit = trade.ExitPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? "";
            var quantity = trade.Quantity.ToString("F0", CultureInfo.InvariantCulture);
            var multiplier = trade.ContractMultiplier.ToString("F0", CultureInfo.InvariantCulture);
            var strike = trade.StrikePrice.ToString("F2", CultureInfo.InvariantCulture);
            var expiration = trade.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            var pnl = (trade.CalculatePnL() ?? 0).ToString("F2", CultureInfo.InvariantCulture);
            var notes = EscapeCsv(trade.Notes ?? "");
            var analysis = EscapeCsv(trade.Analysis ?? "");

            csv.AppendLine($"{date},{symbol},{type},{entry},{exit},{quantity},{multiplier},{strike},{expiration},{pnl},{notes},{analysis}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

/// <summary>
/// Summary statistics for a set of trades.
/// </summary>
public class TradeLogSummary
{
    public int TotalTrades { get; set; }
    public int OpenTrades { get; set; }
    public int ClosedTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal AveragePnL { get; set; }
    public decimal BestTrade { get; set; }
    public decimal WorstTrade { get; set; }
}
