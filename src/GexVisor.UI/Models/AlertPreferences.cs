using GexVisor.UI.Configuration;

namespace GexVisor.UI.Models;

/// <summary>
/// User preferences for alert configuration.
/// Persisted to localStorage for persistence across sessions.
/// </summary>
public record AlertPreferences
{
    /// <summary>Whether alerts are enabled globally.</summary>
    public bool AlertsEnabled { get; init; } = true;

    /// <summary>Enable zero-gamma cross alerts.</summary>
    public bool ZeroGammaCrossEnabled { get; init; } = true;

    /// <summary>Enable regime change alerts.</summary>
    public bool RegimeChangeEnabled { get; init; } = true;

    /// <summary>Enable GEX threshold alerts.</summary>
    public bool GexThresholdEnabled { get; init; } = true;

    /// <summary>Enable price movement alerts.</summary>
    public bool PriceMovementEnabled { get; init; } = true;

    /// <summary>Upper GEX threshold in billions (alert when exceeded).</summary>
    public decimal GexUpperThreshold { get; init; } = AppConstants.Alerts.DefaultGexUpperThreshold;

    /// <summary>Lower GEX threshold in billions (alert when dropped below).</summary>
    public decimal GexLowerThreshold { get; init; } = AppConstants.Alerts.DefaultGexLowerThreshold;

    /// <summary>Price movement percentage threshold for alerts.</summary>
    public decimal PriceMovementThreshold { get; init; } = AppConstants.Alerts.DefaultPriceMovementThreshold;

    /// <summary>Cooldown period between alerts of same type (seconds).</summary>
    public int AlertCooldownSeconds { get; init; } = AppConstants.Alerts.DefaultCooldownSeconds;

    /// <summary>Auto-dismiss alerts after this many seconds (0 = manual dismiss only).</summary>
    public int AutoDismissSeconds { get; init; } = AppConstants.Alerts.DefaultAutoDismissSeconds;

    /// <summary>Maximum alerts to keep in history.</summary>
    public int MaxHistoryCount { get; init; } = AppConstants.Alerts.MaxHistoryCount;

    /// <summary>
    /// Creates default preferences.
    /// </summary>
    public static AlertPreferences Default => new();
}
