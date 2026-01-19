using System.Globalization;
using System.Timers;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Manages application state for the GEX visualizer.
/// Provides reactive state updates via events.
/// </summary>
public class GexStateService : IDisposable
{
    private readonly GexState _state = new();
    private List<GexDataPoint> _timeline = [];
    private List<GexDataPoint> _demoTimeline = [];
    private System.Timers.Timer? _simulationTimer;

    public event Action? OnStateChanged;
    public event Action? OnSettingsChanged;

    public GexState State => _state;
    public IReadOnlyList<GexDataPoint> Timeline => _timeline;
    public IReadOnlyList<GexDataPoint> DemoTimeline => _demoTimeline;

    public GexStateService()
    {
        InitializeDemoTimeline();
        // Start at the first data point so charts render on load
        if (_timeline.Count > 0)
            SetCurrentIndex(0);

        // Initialize simulation timer
        _simulationTimer = new System.Timers.Timer();
        _simulationTimer.Elapsed += OnSimulationTick;
        UpdateTimerInterval();
    }

    /// <summary>
    /// Initialize the demo timeline with historical SPY data points.
    /// </summary>
    private void InitializeDemoTimeline()
    {
        _demoTimeline =
        [
            // 2020 - COVID Era
            CreateDemoPoint("2020-03-23", 222m, 3.5m, -0.42m, "COVID Bottom"),
            CreateDemoPoint("2020-06-08", 323m, 4.0m, -0.15m, "V-Shape Recovery"),
            CreateDemoPoint("2020-09-02", 357m, 4.3m, -0.22m, "Tech Bubble Peak"),
            CreateDemoPoint("2020-12-31", 373m, 4.5m, -0.12m, "Year End Rally"),
            // 2021 - Bull Run
            CreateDemoPoint("2021-02-12", 392m, 5.0m, -0.08m, "Meme Stock Era"),
            CreateDemoPoint("2021-05-07", 422m, 5.3m, -0.14m, "Inflation Fears Begin"),
            CreateDemoPoint("2021-09-02", 453m, 5.8m, -0.18m, "0DTE Growth Starts"),
            CreateDemoPoint("2021-12-31", 476m, 6.0m, -0.15m, "ATH Year End"),
            // 2022 - Bear Market
            CreateDemoPoint("2022-01-24", 436m, 6.2m, -0.32m, "Fed Pivot Fears"),
            CreateDemoPoint("2022-06-16", 366m, 6.5m, -0.45m, "Bear Market Low"),
            CreateDemoPoint("2022-08-16", 429m, 6.8m, -0.25m, "Bear Rally"),
            CreateDemoPoint("2022-10-12", 358m, 7.0m, -0.38m, "Retest Lows"),
            CreateDemoPoint("2022-12-30", 384m, 7.2m, -0.28m, "Choppy Year End"),
            // 2023 - Recovery
            CreateDemoPoint("2023-02-02", 418m, 7.5m, -0.20m, "AI Rally Begins"),
            CreateDemoPoint("2023-07-31", 457m, 8.0m, -0.22m, "Summer Melt-Up"),
            CreateDemoPoint("2023-10-27", 411m, 8.2m, -0.35m, "Rate Spike Selloff"),
            CreateDemoPoint("2023-12-28", 479m, 8.5m, -0.18m, "Santa Rally"),
            // 2024 - New Highs
            CreateDemoPoint("2024-03-28", 523m, 9.0m, -0.20m, "Q1 Breakout"),
            CreateDemoPoint("2024-07-16", 565m, 9.5m, -0.25m, "Summer ATH"),
            CreateDemoPoint("2024-11-11", 600m, 10.0m, -0.30m, "Election Rally"),
            // 2025 - Current
            CreateDemoPoint("2025-12-20", 605m, 11.0m, -0.32m, "Current (Today)")
        ];
        _timeline = [.. _demoTimeline];
    }

    private static GexDataPoint CreateDemoPoint(string date, decimal price, decimal oi, decimal tilt, string label)
    {
        var regime = tilt < -0.25m ? "NEGATIVE_GAMMA" : "POSITIVE_GAMMA";
        var zeroGamma = price * (1 + tilt * 0.1m);

        return new GexDataPoint
        {
            Date = DateOnly.Parse(date, CultureInfo.InvariantCulture),
            Price = price,
            Gex = oi * (1 + tilt),
            CallGex = oi * 0.6m,
            PutGex = oi * 0.4m,
            ZeroGamma = zeroGamma,
            MaxGamma = price,
            Regime = regime,
            CallOi = 0.35m,
            PutOi = 0.65m,
            Contracts = (int)(oi * 1000),
            Quality = 1.0m,
            Label = label
        };
    }

    /// <summary>
    /// Set the timeline from loaded data.
    /// </summary>
    public void SetTimeline(List<GexDataPoint> timeline)
    {
        _timeline = timeline;
        _state.CurrentIndex = -1;
        NotifyStateChanged();
    }

    /// <summary>
    /// Reset to demo timeline.
    /// </summary>
    public void ResetToDemoTimeline()
    {
        _timeline = [.. _demoTimeline];
        _state.Mode = DataMode.Demo;
        _state.CurrentSymbol = null;
        _state.CurrentIndex = -1;
        NotifyStateChanged();
    }

    /// <summary>
    /// Update the current index and sync state values.
    /// </summary>
    public void SetCurrentIndex(int index)
    {
        if (index < 0 || index >= _timeline.Count)
        {
            _state.CurrentIndex = -1;
            _state.CurrentDataPoint = null;
            return;
        }

        _state.CurrentIndex = index;
        var point = _timeline[index];
        _state.CurrentDataPoint = point;
        _state.Price = point.Price;
        _state.Tilt = CalculateTilt(point);
        _state.OpenInterest = point.Contracts / 1000m;
        UpdateStrikeRange();
        NotifyStateChanged();
    }

    private static decimal CalculateTilt(GexDataPoint point)
    {
        return point.Regime == "NEGATIVE_GAMMA"
            ? -0.15m - (point.PutOi - 0.5m) * 0.5m
            : 0.1m + (point.CallOi - 0.5m) * 0.3m;
    }

    /// <summary>
    /// Step forward in the timeline.
    /// </summary>
    public void StepForward()
    {
        if (_state.CurrentIndex < _timeline.Count - 1)
            SetCurrentIndex(_state.CurrentIndex + 1);
    }

    /// <summary>
    /// Step backward in the timeline.
    /// </summary>
    public void StepBackward()
    {
        if (_state.CurrentIndex > 0)
            SetCurrentIndex(_state.CurrentIndex - 1);
    }

    /// <summary>
    /// Jump to start of timeline.
    /// </summary>
    public void JumpToStart() => SetCurrentIndex(0);

    /// <summary>
    /// Jump to end of timeline.
    /// </summary>
    public void JumpToEnd() => SetCurrentIndex(_timeline.Count - 1);

    /// <summary>
    /// Update strike range based on current price data.
    /// </summary>
    public void UpdateStrikeRange()
    {
        if (_timeline.Count == 0)
            return;

        var (minPrice, maxPrice) = GetPriceRange();
        var padding = (maxPrice - minPrice) * 0.2m;
        var paddedMin = Math.Max(0, minPrice - padding);
        var paddedMax = maxPrice + padding;

        decimal roundUnit = paddedMax switch
        {
            < 50 => 1,
            < 200 => 5,
            _ => 10
        };

        _state.StrikeStart = Math.Floor(paddedMin / roundUnit) * roundUnit;
        _state.StrikeEnd = Math.Ceiling(paddedMax / roundUnit) * roundUnit;

        var range = _state.StrikeEnd - _state.StrikeStart;
        _state.StrikeStep = range switch
        {
            > 500 => 20,
            > 200 => 10,
            > 100 => 5,
            > 50 => 2,
            _ => 1
        };
    }

    private (decimal Min, decimal Max) GetPriceRange()
    {
        if (_timeline.Count == 0)
            return (280, 650);
        return (_timeline.Min(t => t.Price), _timeline.Max(t => t.Price));
    }

    /// <summary>
    /// Update manual parameter values (for slider controls).
    /// </summary>
    public void UpdatePrice(decimal price)
    {
        _state.Price = price;
        NotifyStateChanged();
    }

    public void UpdateOpenInterest(decimal oi)
    {
        _state.OpenInterest = oi;
        NotifyStateChanged();
    }

    public void UpdateTilt(decimal tilt)
    {
        _state.Tilt = tilt;
        NotifyStateChanged();
    }

    /// <summary>
    /// Reset axis zoom scales to default.
    /// </summary>
    public void ResetView()
    {
        _state.YAxisScale = 1.0m;
        _state.XAxisScale = 1.0m;
        NotifyStateChanged();
    }

    /// <summary>
    /// Adjust Y-axis zoom scale.
    /// </summary>
    public void AdjustYAxisScale(decimal delta)
    {
        _state.YAxisScale = Math.Max(0.2m, Math.Min(3.0m, _state.YAxisScale + delta));
        OnSettingsChanged?.Invoke();
        NotifyStateChanged();
    }

    /// <summary>
    /// Adjust X-axis zoom scale.
    /// </summary>
    public void AdjustXAxisScale(decimal delta)
    {
        _state.XAxisScale = Math.Max(0.3m, Math.Min(3.0m, _state.XAxisScale + delta));
        OnSettingsChanged?.Invoke();
        NotifyStateChanged();
    }

    /// <summary>
    /// Get current settings for persistence.
    /// </summary>
    public AppSettings GetSettings()
    {
        return new AppSettings
        {
            PlaybackSpeed = _state.PlaybackSpeed,
            YAxisScale = _state.YAxisScale,
            XAxisScale = _state.XAxisScale,
            LastSymbol = _state.CurrentSymbol,
            LastDataMode = _state.Mode
        };
    }

    /// <summary>
    /// Apply settings from storage.
    /// </summary>
    public void ApplySettings(AppSettings settings)
    {
        _state.PlaybackSpeed = settings.PlaybackSpeed;
        _state.YAxisScale = settings.YAxisScale;
        _state.XAxisScale = settings.XAxisScale;
        _state.Mode = settings.LastDataMode;
        UpdateTimerInterval();
        NotifyStateChanged();
    }

    /// <summary>
    /// Toggle simulation playback.
    /// </summary>
    public void ToggleSimulation()
    {
        _state.IsSimulating = !_state.IsSimulating;

        if (_state.IsSimulating)
        {
            // If at end, wrap to start
            if (_state.CurrentIndex >= _timeline.Count - 1)
                SetCurrentIndex(0);
            _simulationTimer?.Start();
        }
        else
        {
            _simulationTimer?.Stop();
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Set playback speed (1 = 0.5x, 2 = 1x, 4 = 2x).
    /// </summary>
    public void SetPlaybackSpeed(int speed)
    {
        _state.PlaybackSpeed = speed;
        UpdateTimerInterval();
        OnSettingsChanged?.Invoke();
        NotifyStateChanged();
    }

    /// <summary>
    /// Set data mode (Demo or Real).
    /// </summary>
    public void SetDataMode(DataMode mode)
    {
        _state.Mode = mode;
        if (mode == DataMode.Demo)
            ResetToDemoTimeline();
        NotifyStateChanged();
    }

    private void UpdateTimerInterval()
    {
        if (_simulationTimer == null)
            return;

        // Base interval of 1000ms at 1x speed
        // Speed 1 = 0.5x (2000ms), Speed 2 = 1x (1000ms), Speed 4 = 2x (500ms)
        var baseInterval = 1000.0;
        _simulationTimer.Interval = baseInterval / (_state.PlaybackSpeed / 2.0);
    }

    private void OnSimulationTick(object? sender, ElapsedEventArgs e)
    {
        if (!_state.IsSimulating)
            return;

        if (_state.CurrentIndex < _timeline.Count - 1)
        {
            SetCurrentIndex(_state.CurrentIndex + 1);
        }
        else
        {
            // Stop at end
            _state.IsSimulating = false;
            _simulationTimer?.Stop();
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    /// <summary>
    /// Get regime analysis for the current timeline.
    /// </summary>
    public RegimeAnalysisSummary? GetRegimeAnalysis()
    {
        if (_timeline.Count == 0)
            return null;

        // Build a temporary GexTimeline to use its AnalyzeRegimes method
        var timeline = new GexTimeline
        {
            Symbol = _state.CurrentSymbol ?? "Demo",
            AssetClass = "Index",
            DateRange = new DateRange
            {
                Start = _timeline.First().Date,
                End = _timeline.Last().Date
            },
            Count = _timeline.Count,
            Timeline = _timeline
        };

        return timeline.AnalyzeRegimes();
    }

    public void Dispose()
    {
        _simulationTimer?.Stop();
        _simulationTimer?.Dispose();
    }
}
