using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for monitoring market conditions and triggering alerts.
/// </summary>
public interface IAlertService : IDisposable
{
    /// <summary>Event fired when a new alert is triggered.</summary>
    event Action<Alert>? OnAlertTriggered;

    /// <summary>Event fired when an alert is dismissed.</summary>
    event Action<Guid>? OnAlertDismissed;

    /// <summary>Event fired when preferences change.</summary>
    event Action? OnPreferencesChanged;

    /// <summary>Current active (non-dismissed) alerts.</summary>
    IReadOnlyList<Alert> ActiveAlerts { get; }

    /// <summary>Alert history (including dismissed).</summary>
    IReadOnlyList<Alert> AlertHistory { get; }

    /// <summary>Current alert preferences.</summary>
    AlertPreferences Preferences { get; }

    /// <summary>Whether monitoring is currently active.</summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Start monitoring for alert conditions.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stop monitoring for alert conditions.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Dismiss an alert by ID.
    /// </summary>
    void DismissAlert(Guid alertId);

    /// <summary>
    /// Dismiss all active alerts.
    /// </summary>
    void DismissAllAlerts();

    /// <summary>
    /// Clear alert history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Update alert preferences.
    /// </summary>
    Task UpdatePreferencesAsync(AlertPreferences preferences);

    /// <summary>
    /// Load preferences from storage.
    /// </summary>
    Task LoadPreferencesAsync();

    /// <summary>
    /// Check conditions and trigger alerts if thresholds met.
    /// Normally called automatically during monitoring, but can be called manually.
    /// </summary>
    void CheckConditions();
}
