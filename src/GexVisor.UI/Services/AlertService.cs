using GexVisor.Core;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for monitoring GEX conditions and triggering alerts.
/// Subscribes to GexStateService and evaluates alert conditions on each update.
/// </summary>
/// <remarks>
/// Thread Safety: This service is NOT thread-safe and assumes execution on a single thread
/// (Blazor UI synchronization context). If IGexStateService fires OnStateChanged from a
/// background thread, callers must marshal to the UI thread via InvokeAsync before
/// invoking this service's event handlers. Concurrent access to CheckConditions(),
/// TryTriggerAlert(), or DismissAlert() will result in race conditions.
/// </remarks>
public class AlertService : IAlertService
{
    private readonly IGexStateService _gexState;
    private readonly ILocalStorageService _localStorage;

    private readonly List<Alert> _activeAlerts = [];
    private readonly List<Alert> _alertHistory = [];
    private readonly Dictionary<AlertType, DateTime> _lastAlertTimes = [];

    private AlertPreferences _preferences = AlertPreferences.Default;
    private bool _isMonitoring;
    private bool _disposed;

    // Previous state for change detection
    private GexRegime? _previousRegime;
    private decimal? _previousSpotPrice;
    private decimal? _previousTotalGex;
    private bool? _previousAboveZeroGamma;

    public event Action<Alert>? OnAlertTriggered;
    public event Action<Guid>? OnAlertDismissed;
    public event Action? OnPreferencesChanged;

    public IReadOnlyList<Alert> ActiveAlerts => _activeAlerts.AsReadOnly();
    public IReadOnlyList<Alert> AlertHistory => _alertHistory.AsReadOnly();
    public AlertPreferences Preferences => _preferences;
    public bool IsMonitoring => _isMonitoring;

    public AlertService(IGexStateService gexState, ILocalStorageService localStorage)
    {
        _gexState = gexState;
        _localStorage = localStorage;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
        {
            return;
        }

        _isMonitoring = true;
        _gexState.OnStateChanged += OnGexStateChanged;

        // Initialize previous state from current data
        if (_gexState.LiveGexData is { } data)
        {
            _previousRegime = data.Regime;
            _previousSpotPrice = data.SpotPrice;
            _previousTotalGex = data.TotalGex;
            _previousAboveZeroGamma = data.ZeroGammaLevel.HasValue
                ? data.SpotPrice > data.ZeroGammaLevel.Value
                : null;
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _gexState.OnStateChanged -= OnGexStateChanged;
    }

    private void OnGexStateChanged()
    {
        if (!_isMonitoring || !_preferences.AlertsEnabled)
        {
            return;
        }

        if (!_gexState.UseLiveData || _gexState.LiveGexData is null)
        {
            return;
        }

        CheckConditions();
    }

    public void CheckConditions()
    {
        if (_gexState.LiveGexData is not { } data)
        {
            return;
        }

        var symbol = data.Symbol;

        // Check regime change
        if (_preferences.RegimeChangeEnabled && _previousRegime.HasValue)
        {
            if (data.Regime != _previousRegime.Value && data.Regime != GexRegime.Neutral)
            {
                TryTriggerAlert(Alert.RegimeChange(symbol, _previousRegime.Value, data.Regime));
            }
        }

        // Check zero-gamma cross
        if (_preferences.ZeroGammaCrossEnabled && data.ZeroGammaLevel.HasValue && _previousAboveZeroGamma.HasValue)
        {
            var currentlyAbove = data.SpotPrice > data.ZeroGammaLevel.Value;
            if (currentlyAbove != _previousAboveZeroGamma.Value)
            {
                TryTriggerAlert(Alert.ZeroGammaCross(symbol, data.SpotPrice, data.ZeroGammaLevel.Value, currentlyAbove));
            }
        }

        // Check GEX threshold
        if (_preferences.GexThresholdEnabled && _previousTotalGex.HasValue)
        {
            // Upper threshold breach
            if (data.TotalGex >= _preferences.GexUpperThreshold && _previousTotalGex.Value < _preferences.GexUpperThreshold)
            {
                TryTriggerAlert(Alert.GexThreshold(symbol, data.TotalGex, _preferences.GexUpperThreshold, exceededAbove: true));
            }
            // Lower threshold breach
            if (data.TotalGex <= _preferences.GexLowerThreshold && _previousTotalGex.Value > _preferences.GexLowerThreshold)
            {
                TryTriggerAlert(Alert.GexThreshold(symbol, data.TotalGex, _preferences.GexLowerThreshold, exceededAbove: false));
            }
        }

        // Check price movement (intraday)
        if (_preferences.PriceMovementEnabled && _previousSpotPrice.HasValue && _previousSpotPrice.Value > 0)
        {
            var percentChange = ((data.SpotPrice - _previousSpotPrice.Value) / _previousSpotPrice.Value) * 100;
            if (Math.Abs(percentChange) >= _preferences.PriceMovementThreshold)
            {
                var priceChange = data.SpotPrice - _previousSpotPrice.Value;
                TryTriggerAlert(Alert.PriceMovement(symbol, priceChange, percentChange));
            }
        }

        // Update previous state
        _previousRegime = data.Regime;
        _previousSpotPrice = data.SpotPrice;
        _previousTotalGex = data.TotalGex;
        _previousAboveZeroGamma = data.ZeroGammaLevel.HasValue
            ? data.SpotPrice > data.ZeroGammaLevel.Value
            : null;
    }

    private void TryTriggerAlert(Alert alert)
    {
        // Check cooldown
        if (_lastAlertTimes.TryGetValue(alert.Type, out var lastTime))
        {
            var cooldown = TimeSpan.FromSeconds(_preferences.AlertCooldownSeconds);
            if (DateTime.UtcNow - lastTime < cooldown)
            {
                return;
            }
        }

        // Add to active and history
        _activeAlerts.Add(alert);
        _alertHistory.Insert(0, alert);

        // Trim history
        while (_alertHistory.Count > _preferences.MaxHistoryCount)
        {
            _alertHistory.RemoveAt(_alertHistory.Count - 1);
        }

        // Trim active alerts
        while (_activeAlerts.Count > AppConstants.Alerts.MaxVisibleToasts)
        {
            var oldest = _activeAlerts[0];
            oldest.IsDismissed = true;
            oldest.DismissedAt = DateTime.UtcNow;
            _activeAlerts.RemoveAt(0);
        }

        // Update cooldown
        _lastAlertTimes[alert.Type] = DateTime.UtcNow;

        // Fire event
        OnAlertTriggered?.Invoke(alert);
    }

    public void DismissAlert(Guid alertId)
    {
        var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
        if (alert is null)
        {
            return;
        }

        alert.IsDismissed = true;
        alert.DismissedAt = DateTime.UtcNow;
        _activeAlerts.Remove(alert);

        OnAlertDismissed?.Invoke(alertId);
    }

    public void DismissAllAlerts()
    {
        foreach (var alert in _activeAlerts.ToList())
        {
            alert.IsDismissed = true;
            alert.DismissedAt = DateTime.UtcNow;
        }
        _activeAlerts.Clear();
    }

    public void ClearHistory()
    {
        _alertHistory.Clear();
    }

    public async Task UpdatePreferencesAsync(AlertPreferences preferences)
    {
        _preferences = preferences;
        await _localStorage.SetAsync(AppConstants.Storage.AlertPreferences, preferences);
        OnPreferencesChanged?.Invoke();
    }

    public async Task LoadPreferencesAsync()
    {
        var stored = await _localStorage.GetAsync<AlertPreferences>(AppConstants.Storage.AlertPreferences);
        _preferences = stored ?? AlertPreferences.Default;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        StopMonitoring();
        _activeAlerts.Clear();
        _alertHistory.Clear();
    }
}
