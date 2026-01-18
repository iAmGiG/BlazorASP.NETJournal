using System.Text;
using System.Text.Json;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing paper trades with analytics and export capabilities.
/// </summary>
public class PaperTradeService : BaseEntryService<PaperTrade>
{
    public PaperTradeService(LocalStorageService storage)
        : base(storage, StorageKeys.PaperTrades)
    {
    }

    /// <summary>
    /// Search trades by notes content or tags.
    /// </summary>
    public override IEnumerable<PaperTrade> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Entries;

        var lower = query.ToLowerInvariant();
        return Entries.Where(t =>
            (t.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            t.Tags.Any(tag => tag.Contains(lower, StringComparison.OrdinalIgnoreCase)) ||
            t.Direction.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (t.ExitReason?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    /// <summary>
    /// Get all open trades.
    /// </summary>
    public IEnumerable<PaperTrade> GetOpenTrades() => Entries.Where(t => t.IsOpen);

    /// <summary>
    /// Get all closed trades.
    /// </summary>
    public IEnumerable<PaperTrade> GetClosedTrades() => Entries.Where(t => !t.IsOpen);

    /// <summary>
    /// Close an open trade.
    /// </summary>
    public async Task CloseTradeAsync(Guid id, decimal exitPrice, string exitReason)
    {
        var trade = GetById(id);
        if (trade == null || !trade.IsOpen)
            return;

        var closedTrade = trade with
        {
            ExitDate = DateTime.UtcNow,
            ExitPrice = exitPrice,
            ExitReason = exitReason,
            UpdatedAt = DateTime.UtcNow
        };

        await UpdateAsync(closedTrade);
    }

    // === Analytics ===

    /// <summary>
    /// Get summary statistics for all trades.
    /// </summary>
    public TradeSummary GetSummary()
    {
        var closed = GetClosedTrades().ToList();

        return new TradeSummary
        {
            TotalTrades = Entries.Count,
            OpenTrades = Entries.Count(t => t.IsOpen),
            ClosedTrades = closed.Count,
            Wins = closed.Count(t => t.IsWin == true),
            Losses = closed.Count(t => t.IsWin == false),
            WinRate = closed.Count > 0 ? (decimal)closed.Count(t => t.IsWin == true) / closed.Count * 100 : 0,
            TotalPnLPoints = closed.Sum(t => t.PnLPoints ?? 0),
            AveragePnLPercent = closed.Count > 0 ? closed.Average(t => t.PnLPercent ?? 0) : 0,
            LongTrades = Entries.Count(t => t.Direction == TradeDirection.Long),
            ShortTrades = Entries.Count(t => t.Direction == TradeDirection.Short)
        };
    }

    /// <summary>
    /// Get P&L breakdown by pattern tag.
    /// </summary>
    public IEnumerable<PatternStats> GetStatsByPattern()
    {
        var closed = GetClosedTrades().ToList();

        return closed
            .SelectMany(t => t.Tags.Select(tag => new { Tag = tag, Trade = t }))
            .GroupBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PatternStats
            {
                Pattern = g.Key,
                TradeCount = g.Count(),
                Wins = g.Count(x => x.Trade.IsWin == true),
                TotalPnLPoints = g.Sum(x => x.Trade.PnLPoints ?? 0),
                AvgPnLPercent = g.Average(x => x.Trade.PnLPercent ?? 0)
            })
            .OrderByDescending(p => p.TradeCount);
    }

    /// <summary>
    /// Get P&L breakdown by gamma regime.
    /// </summary>
    public RegimeStats GetStatsByRegime()
    {
        var closed = GetClosedTrades().ToList();
        var negativeGamma = closed.Where(t => t.IsNegativeGammaAtCreation == true).ToList();
        var positiveGamma = closed.Where(t => t.IsNegativeGammaAtCreation == false).ToList();

        return new RegimeStats
        {
            NegativeGamma = new RegimeDetail
            {
                TradeCount = negativeGamma.Count,
                Wins = negativeGamma.Count(t => t.IsWin == true),
                TotalPnLPoints = negativeGamma.Sum(t => t.PnLPoints ?? 0),
                AvgPnLPercent = negativeGamma.Count > 0 ? negativeGamma.Average(t => t.PnLPercent ?? 0) : 0
            },
            PositiveGamma = new RegimeDetail
            {
                TradeCount = positiveGamma.Count,
                Wins = positiveGamma.Count(t => t.IsWin == true),
                TotalPnLPoints = positiveGamma.Sum(t => t.PnLPoints ?? 0),
                AvgPnLPercent = positiveGamma.Count > 0 ? positiveGamma.Average(t => t.PnLPercent ?? 0) : 0
            }
        };
    }

    /// <summary>
    /// Get equity curve data (cumulative P&L over time).
    /// </summary>
    public IEnumerable<EquityPoint> GetEquityCurve()
    {
        var closed = GetClosedTrades()
            .OrderBy(t => t.ExitDate)
            .ToList();

        decimal cumulative = 0;
        foreach (var trade in closed)
        {
            cumulative += trade.PnLPoints ?? 0;
            yield return new EquityPoint
            {
                Date = trade.ExitDate!.Value,
                CumulativePnL = cumulative,
                TradeId = trade.Id
            };
        }
    }

    // === Export ===

    /// <summary>
    /// Export trades as JSON.
    /// </summary>
    public string ExportAsJson()
    {
        return JsonSerializer.Serialize(Entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Export trades as CSV.
    /// </summary>
    public string ExportAsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAt,Direction,EntryPrice,TargetPrice,StopLoss,ExitDate,ExitPrice,ExitReason,PnLPoints,PnLPercent,GexAtEntry,IsNegativeGamma,Tags,Notes");

        foreach (var t in Entries)
        {
            var tags = string.Join(";", t.Tags);
            var notes = t.Notes?.Replace("\"", "\"\"") ?? "";
            sb.AppendLine($"{t.Id},{t.CreatedAt:O},{t.Direction},{t.EntryPrice},{t.TargetPrice},{t.StopLoss},{t.ExitDate:O},{t.ExitPrice},{t.ExitReason},{t.PnLPoints},{t.PnLPercent:F2},{t.GexAtCreation},{t.IsNegativeGammaAtCreation},\"{tags}\",\"{notes}\"");
        }

        return sb.ToString();
    }
}

// === Analytics DTOs ===

public record TradeSummary
{
    public int TotalTrades { get; init; }
    public int OpenTrades { get; init; }
    public int ClosedTrades { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public decimal WinRate { get; init; }
    public decimal TotalPnLPoints { get; init; }
    public decimal AveragePnLPercent { get; init; }
    public int LongTrades { get; init; }
    public int ShortTrades { get; init; }
}

public record PatternStats
{
    public required string Pattern { get; init; }
    public int TradeCount { get; init; }
    public int Wins { get; init; }
    public decimal WinRate => TradeCount > 0 ? (decimal)Wins / TradeCount * 100 : 0;
    public decimal TotalPnLPoints { get; init; }
    public decimal AvgPnLPercent { get; init; }
}

public record RegimeStats
{
    public required RegimeDetail NegativeGamma { get; init; }
    public required RegimeDetail PositiveGamma { get; init; }
}

public record RegimeDetail
{
    public int TradeCount { get; init; }
    public int Wins { get; init; }
    public decimal WinRate => TradeCount > 0 ? (decimal)Wins / TradeCount * 100 : 0;
    public decimal TotalPnLPoints { get; init; }
    public decimal AvgPnLPercent { get; init; }
}

public record EquityPoint
{
    public DateTime Date { get; init; }
    public decimal CumulativePnL { get; init; }
    public Guid TradeId { get; init; }
}
