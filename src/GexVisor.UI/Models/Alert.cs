using GexVisor.Core;

namespace GexVisor.UI.Models;

/// <summary>
/// Types of alerts that can be triggered by the alert system.
/// </summary>
public enum AlertType
{
    /// <summary>Price crossed the zero-gamma level.</summary>
    ZeroGammaCross,

    /// <summary>GEX regime changed (Long ↔ Short gamma).</summary>
    RegimeChange,

    /// <summary>Total GEX exceeded a user-defined threshold.</summary>
    GexThreshold,

    /// <summary>Significant intraday price movement (>1%).</summary>
    PriceMovement
}

/// <summary>
/// Severity levels for alerts to control display styling.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informational alert (blue).</summary>
    Info,

    /// <summary>Warning alert requiring attention (yellow).</summary>
    Warning,

    /// <summary>Critical alert - immediate attention (red).</summary>
    Critical
}

/// <summary>
/// Represents an alert that has been triggered by the monitoring system.
/// </summary>
public record Alert
{
    /// <summary>Unique identifier for this alert instance.</summary>
    public required Guid Id { get; init; }

    /// <summary>Type of alert triggered.</summary>
    public required AlertType Type { get; init; }

    /// <summary>Severity level for display styling.</summary>
    public required AlertSeverity Severity { get; init; }

    /// <summary>Trading symbol that triggered the alert.</summary>
    public required string Symbol { get; init; }

    /// <summary>Human-readable alert title.</summary>
    public required string Title { get; init; }

    /// <summary>Detailed alert message.</summary>
    public required string Message { get; init; }

    /// <summary>When the alert was triggered (UTC).</summary>
    public required DateTime TriggeredAt { get; init; }

    /// <summary>Whether the alert has been dismissed by the user.</summary>
    public bool IsDismissed { get; set; }

    /// <summary>When the alert was dismissed (UTC), if applicable.</summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// Creates a zero-gamma cross alert.
    /// </summary>
    public static Alert ZeroGammaCross(string symbol, decimal spotPrice, decimal zeroGammaLevel, bool crossedAbove)
    {
        var direction = crossedAbove ? "above" : "below";
        return new Alert
        {
            Id = Guid.NewGuid(),
            Type = AlertType.ZeroGammaCross,
            Severity = AlertSeverity.Warning,
            Symbol = symbol,
            Title = $"Zero-Gamma Cross: {symbol}",
            Message = $"{symbol} crossed {direction} zero-gamma level (${zeroGammaLevel:F2}). Current: ${spotPrice:F2}",
            TriggeredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a regime change alert.
    /// </summary>
    public static Alert RegimeChange(string symbol, GexRegime previousRegime, GexRegime newRegime)
    {
        var severity = newRegime == GexRegime.ShortGamma ? AlertSeverity.Critical : AlertSeverity.Warning;

        static string GetRegimeLabel(GexRegime regime) => regime switch
        {
            GexRegime.LongGamma => "Long γ (Dampened)",
            GexRegime.ShortGamma => "Short γ (Amplified)",
            _ => "Neutral"
        };

        var previousLabel = GetRegimeLabel(previousRegime);
        var newLabel = GetRegimeLabel(newRegime);

        return new Alert
        {
            Id = Guid.NewGuid(),
            Type = AlertType.RegimeChange,
            Severity = severity,
            Symbol = symbol,
            Title = $"Regime Shift: {symbol}",
            Message = $"{symbol} shifted from {previousLabel} to {newLabel}. Volatility dynamics changed.",
            TriggeredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a GEX threshold alert.
    /// </summary>
    public static Alert GexThreshold(string symbol, decimal totalGex, decimal threshold, bool exceededAbove)
    {
        var direction = exceededAbove ? "exceeded" : "dropped below";
        return new Alert
        {
            Id = Guid.NewGuid(),
            Type = AlertType.GexThreshold,
            Severity = AlertSeverity.Info,
            Symbol = symbol,
            Title = $"GEX Threshold: {symbol}",
            Message = $"{symbol} Total GEX {direction} ${threshold:F1}B. Current: ${totalGex:F1}B",
            TriggeredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a significant price movement alert.
    /// </summary>
    public static Alert PriceMovement(string symbol, decimal priceChange, decimal percentChange)
    {
        var direction = priceChange > 0 ? "up" : "down";
        return new Alert
        {
            Id = Guid.NewGuid(),
            Type = AlertType.PriceMovement,
            Severity = Math.Abs(percentChange) >= 2 ? AlertSeverity.Warning : AlertSeverity.Info,
            Symbol = symbol,
            Title = $"Price Alert: {symbol}",
            Message = $"{symbol} moved {direction} {Math.Abs(percentChange):F2}% (${Math.Abs(priceChange):F2})",
            TriggeredAt = DateTime.UtcNow
        };
    }
}
