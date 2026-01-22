using GexVisor.Core;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Interface for the GEX state service providing reactive state management
/// for the visualizer. Enables dependency injection and testability.
/// </summary>
public interface IGexStateService : IDisposable
{
    /// <summary>
    /// Fired when any state property changes.
    /// </summary>
    event Action? OnStateChanged;

    /// <summary>
    /// Fired when settings (axis scales, playback speed) change.
    /// Used for persistence triggers.
    /// </summary>
    event Action? OnSettingsChanged;

    /// <summary>
    /// Current visualization state.
    /// </summary>
    GexState State { get; }

    /// <summary>
    /// Current timeline data points.
    /// </summary>
    IReadOnlyList<GexDataPoint> Timeline { get; }

    /// <summary>
    /// Built-in demo timeline for offline use.
    /// </summary>
    IReadOnlyList<GexDataPoint> DemoTimeline { get; }

    /// <summary>
    /// Set the timeline from loaded data.
    /// </summary>
    void SetTimeline(List<GexDataPoint> timeline);

    /// <summary>
    /// Reset to demo timeline.
    /// </summary>
    void ResetToDemoTimeline();

    /// <summary>
    /// Update the current index and sync state values.
    /// </summary>
    void SetCurrentIndex(int index);

    /// <summary>
    /// Step forward in the timeline.
    /// </summary>
    void StepForward();

    /// <summary>
    /// Step backward in the timeline.
    /// </summary>
    void StepBackward();

    /// <summary>
    /// Jump to start of timeline.
    /// </summary>
    void JumpToStart();

    /// <summary>
    /// Jump to end of timeline.
    /// </summary>
    void JumpToEnd();

    /// <summary>
    /// Update strike range based on current price data.
    /// </summary>
    void UpdateStrikeRange();

    /// <summary>
    /// Update manual price value (for slider controls).
    /// </summary>
    void UpdatePrice(decimal price);

    /// <summary>
    /// Update manual open interest value (for slider controls).
    /// </summary>
    void UpdateOpenInterest(decimal oi);

    /// <summary>
    /// Update manual tilt value (for slider controls).
    /// </summary>
    void UpdateTilt(decimal tilt);

    /// <summary>
    /// Reset axis zoom scales to default.
    /// </summary>
    void ResetView();

    /// <summary>
    /// Adjust Y-axis zoom scale.
    /// </summary>
    void AdjustYAxisScale(decimal delta);

    /// <summary>
    /// Adjust X-axis zoom scale.
    /// </summary>
    void AdjustXAxisScale(decimal delta);

    /// <summary>
    /// Get current settings for persistence.
    /// </summary>
    AppSettings GetSettings();

    /// <summary>
    /// Apply settings from storage.
    /// </summary>
    void ApplySettings(AppSettings settings);

    /// <summary>
    /// Toggle simulation playback.
    /// </summary>
    void ToggleSimulation();

    /// <summary>
    /// Set playback speed (1 = 0.5x, 2 = 1x, 4 = 2x).
    /// </summary>
    void SetPlaybackSpeed(int speed);

    /// <summary>
    /// Set data mode (Demo or Real).
    /// </summary>
    void SetDataMode(DataMode mode);

    /// <summary>
    /// Get regime analysis for the current timeline.
    /// </summary>
    RegimeAnalysisSummary? GetRegimeAnalysis();

    /// <summary>
    /// Whether to use live GEX data from API instead of simulation.
    /// </summary>
    bool UseLiveData { get; set; }

    /// <summary>
    /// Current live GEX calculation result from API.
    /// </summary>
    GexCalculationResult? LiveGexData { get; }

    /// <summary>
    /// Whether live data is currently being fetched.
    /// </summary>
    bool IsLoadingLiveData { get; }

    /// <summary>
    /// Error message from the last failed live data refresh, or null if successful.
    /// </summary>
    string? LiveDataError { get; }

    /// <summary>
    /// Timestamp of the last successful live data refresh.
    /// </summary>
    DateTime? LastLiveDataRefresh { get; }

    /// <summary>
    /// Refresh live GEX data from API for the given symbol.
    /// </summary>
    Task RefreshLiveDataAsync(string symbol);

    /// <summary>
    /// Start periodic polling for live GEX data.
    /// </summary>
    /// <param name="symbol">Symbol to poll for.</param>
    /// <param name="intervalMs">Polling interval in milliseconds (default: 30000).</param>
    void StartLiveDataPolling(string symbol, int intervalMs = 30000);

    /// <summary>
    /// Stop periodic polling for live GEX data.
    /// </summary>
    void StopLiveDataPolling();
}
