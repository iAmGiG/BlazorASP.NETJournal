namespace GexVisor.UI.Models;

/// <summary>
/// Represents a hypothetical paper trade for tracking GEX-based trading strategies.
/// </summary>
public record PaperTrade : ContextualEntry
{
    /// <summary>Trade direction: "Long" or "Short".</summary>
    public required string Direction { get; init; }

    /// <summary>Entry price for the trade.</summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>Optional target price for profit taking.</summary>
    public decimal? TargetPrice { get; init; }

    /// <summary>Optional stop loss price.</summary>
    public decimal? StopLoss { get; init; }

    /// <summary>Notes about the trade setup or reasoning.</summary>
    public string? Notes { get; init; }

    // Exit fields
    /// <summary>Date when the trade was closed.</summary>
    public DateTime? ExitDate { get; init; }

    /// <summary>Exit price when the trade was closed.</summary>
    public decimal? ExitPrice { get; init; }

    /// <summary>Reason for exit: "Target", "Stop", "Manual", "Time".</summary>
    public string? ExitReason { get; init; }

    // Computed properties
    /// <summary>Whether the trade is still open.</summary>
    public bool IsOpen => ExitDate == null;

    /// <summary>P&L in price points (null if trade is open).</summary>
    public decimal? PnLPoints => ExitPrice.HasValue
        ? (ExitPrice.Value - EntryPrice) * (Direction == "Long" ? 1 : -1)
        : null;

    /// <summary>P&L as a percentage (null if trade is open).</summary>
    public decimal? PnLPercent => PnLPoints.HasValue && EntryPrice != 0
        ? PnLPoints.Value / EntryPrice * 100
        : null;

    /// <summary>Whether the trade was profitable.</summary>
    public bool? IsWin => PnLPoints.HasValue ? PnLPoints.Value > 0 : null;

    /// <summary>Current unrealized P&L based on a given current price.</summary>
    public decimal GetUnrealizedPnL(decimal currentPrice)
    {
        return (currentPrice - EntryPrice) * (Direction == "Long" ? 1 : -1);
    }

    /// <summary>Current unrealized P&L percentage based on a given current price.</summary>
    public decimal GetUnrealizedPnLPercent(decimal currentPrice)
    {
        return EntryPrice != 0 ? GetUnrealizedPnL(currentPrice) / EntryPrice * 100 : 0;
    }
}

/// <summary>
/// Direction constants for paper trades.
/// </summary>
public static class TradeDirection
{
    public const string Long = "Long";
    public const string Short = "Short";
}

/// <summary>
/// Exit reason constants for paper trades.
/// </summary>
public static class ExitReason
{
    public const string Target = "Target";
    public const string Stop = "Stop";
    public const string Manual = "Manual";
    public const string Time = "Time";
}
