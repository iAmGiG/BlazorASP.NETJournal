using System.Text;
using System.Text.Json;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing backtest results with comparison and analytics.
/// </summary>
public class BacktestService : BaseEntryService<BacktestResult>
{
    public BacktestService(LocalStorageService storage)
        : base(storage, StorageKeys.BacktestResults)
    {
    }

    /// <summary>
    /// Search backtests by strategy name, description, or notes.
    /// </summary>
    public override IEnumerable<BacktestResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Entries;

        return Entries.Where(r =>
            r.StrategyName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (r.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            r.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Get results ordered by run date (newest first).
    /// </summary>
    public IEnumerable<BacktestResult> GetOrderedResults()
    {
        return Entries.OrderByDescending(r => r.CreatedAt);
    }

    /// <summary>
    /// Get results for a specific strategy name.
    /// </summary>
    public IEnumerable<BacktestResult> GetByStrategy(string strategyName)
    {
        return Entries.Where(r =>
            r.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all unique strategy names.
    /// </summary>
    public IEnumerable<string> GetStrategyNames()
    {
        return Entries
            .Select(r => r.StrategyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n);
    }

    /// <summary>
    /// Toggle selection for comparison.
    /// </summary>
    public async Task ToggleSelectionAsync(Guid id)
    {
        var result = GetById(id);
        if (result == null)
            return;

        var updated = result with { IsSelected = !result.IsSelected };
        await UpdateAsync(updated);
    }

    /// <summary>
    /// Get selected results for comparison.
    /// </summary>
    public IEnumerable<BacktestResult> GetSelectedResults()
    {
        return Entries.Where(r => r.IsSelected);
    }

    /// <summary>
    /// Clear all selections.
    /// </summary>
    public async Task ClearSelectionsAsync()
    {
        var selected = Entries.Where(r => r.IsSelected).ToList();
        foreach (var result in selected)
        {
            var updated = result with { IsSelected = false };
            var index = Entries.FindIndex(e => e.Id == result.Id);
            if (index >= 0)
                Entries[index] = updated;
        }
        await SaveAsync();
    }

    /// <summary>
    /// Get comparison data for selected results.
    /// </summary>
    public BacktestComparison GetComparison()
    {
        return new BacktestComparison
        {
            Results = GetSelectedResults().ToList()
        };
    }

    /// <summary>
    /// Get aggregate statistics across all results.
    /// </summary>
    public BacktestSummary GetSummary()
    {
        if (Entries.Count == 0)
        {
            return new BacktestSummary();
        }

        return new BacktestSummary
        {
            TotalResults = Entries.Count,
            UniqueStrategies = GetStrategyNames().Count(),
            AvgWinRate = Entries.Average(r => r.WinRate),
            AvgReturn = Entries.Average(r => r.TotalReturn),
            BestReturn = Entries.Max(r => r.TotalReturn),
            WorstReturn = Entries.Min(r => r.TotalReturn),
            AvgSharpe = Entries.Where(r => r.SharpeRatio.HasValue)
                              .Select(r => r.SharpeRatio!.Value)
                              .DefaultIfEmpty(0)
                              .Average()
        };
    }

    /// <summary>
    /// Get performance breakdown by gamma regime.
    /// </summary>
    public RegimePerformance GetRegimePerformance()
    {
        var withRegimeData = Entries
            .Where(r => r.ReturnInPositiveGamma.HasValue || r.ReturnInNegativeGamma.HasValue)
            .ToList();

        if (withRegimeData.Count == 0)
        {
            return new RegimePerformance();
        }

        return new RegimePerformance
        {
            AvgPositiveGammaReturn = withRegimeData
                .Where(r => r.ReturnInPositiveGamma.HasValue)
                .Select(r => r.ReturnInPositiveGamma!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            AvgNegativeGammaReturn = withRegimeData
                .Where(r => r.ReturnInNegativeGamma.HasValue)
                .Select(r => r.ReturnInNegativeGamma!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            ResultsWithRegimeData = withRegimeData.Count
        };
    }

    // === Export ===

    /// <summary>
    /// Export results as JSON.
    /// </summary>
    public string ExportAsJson()
    {
        return JsonSerializer.Serialize(Entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Export results as CSV.
    /// </summary>
    public string ExportAsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAt,StrategyName,StartDate,EndDate,TotalTrades,WinningTrades,WinRate,TotalReturn,MaxDrawdown,SharpeRatio,ProfitFactor,ReturnInPositiveGamma,ReturnInNegativeGamma,Tags,Notes");

        foreach (var r in Entries)
        {
            var tags = string.Join(";", r.Tags);
            var notes = r.Notes?.Replace("\"", "\"\"") ?? "";
            sb.AppendLine($"{r.Id},{r.CreatedAt:O},{r.StrategyName},{r.StartDate},{r.EndDate},{r.TotalTrades},{r.WinningTrades},{r.WinRate:F2},{r.TotalReturn:F2},{r.MaxDrawdown:F2},{r.SharpeRatio},{r.ProfitFactor},{r.ReturnInPositiveGamma},{r.ReturnInNegativeGamma},\"{tags}\",\"{notes}\"");
        }

        return sb.ToString();
    }
}

// === DTOs ===

public record BacktestSummary
{
    public int TotalResults { get; init; }
    public int UniqueStrategies { get; init; }
    public decimal AvgWinRate { get; init; }
    public decimal AvgReturn { get; init; }
    public decimal BestReturn { get; init; }
    public decimal WorstReturn { get; init; }
    public decimal AvgSharpe { get; init; }
}

public record RegimePerformance
{
    public decimal AvgPositiveGammaReturn { get; init; }
    public decimal AvgNegativeGammaReturn { get; init; }
    public int ResultsWithRegimeData { get; init; }
}
