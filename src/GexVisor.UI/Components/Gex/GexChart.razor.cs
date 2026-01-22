using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace GexVisor.UI.Components.Gex;

public partial class GexChart : IAsyncDisposable
{
    [Inject] private IGexStateService StateService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public bool IsAbsolute { get; set; }
    [Parameter] public string Title { get; set; } = "";

    // Extracted magic numbers to named constants
    private static class ChartConstants
    {
        // Strike range limits (was: Math.Max(10, Math.Min(60, ...)))
        public const int MinStrikeCount = 10;
        public const int MaxStrikeCount = 60;

        // S² baseline for scaling (was: 300m * 300m)
        public const decimal BaselineSpotPrice = 300m;
        public const decimal BaselineSpotSquared = 90000m;

        // Open Interest baseline (was: 5m)
        public const decimal BaselineOpenInterest = 5m;

        // Zero-gamma tilt multiplier (was: 0.05m)
        public const decimal ZeroGammaTiltMultiplier = 0.05m;

        // Visualization scaling (was: 50, 48, 45)
        public const double GammaScaleFactor = 50.0;
        public const double MaxBarWidth = 48.0;
        public const double SaturationThreshold = 45.0;

        // Bar rendering (was: 0.85, 85.0, 0.5)
        public const double DefaultBarOpacity = 0.85;
        public const double BarHeightBase = 85.0;
        public const double MinBarHeight = 0.5;

        // Off-screen culling thresholds (was: -10, 110)
        public const double OffScreenMinY = -10.0;
        public const double OffScreenMaxY = 110.0;

        // Default Y position when range = 0 (was: 50)
        public const double DefaultCenterY = 50.0;

        // Gaussian distribution sigma
        public const double Sigma = 12.0;

        // Wheel event throttle interval in milliseconds
        public const int WheelThrottleMs = 50;
    }

    // Chart grid and axis configuration
    private static class ChartConfiguration
    {
        public static readonly double[] HorizontalGridLines = { 20.0, 40.0, 60.0, 80.0 };
        public static readonly double[] VerticalGridLines = { 25.0, 75.0 };
        public const double MaxGammaValue = 50.0;
        public const double MidGammaValue = 25.0;
    }

    private List<GammaBar> GammaBars { get; set; } = new();
    private List<GridLine> GridLines { get; set; } = new();
    private DotNetObjectReference<GexChart>? _dotNetRef;
    private bool _isDragging;
    private bool _jsInitialized;

    // Wheel event throttling timestamps
    private DateTime _lastChartWheelTime;
    private DateTime _lastYAxisWheelTime;
    private DateTime _lastXAxisWheelTime;

    // Crosshair tracking
    private bool _showCrosshair;
    private double _crosshairX;
    private double _crosshairY;
    private decimal _crosshairPrice;
    private string _crosshairGamma = "";

    // Memoization cache for bar calculations
    private decimal _lastPrice;
    private decimal _lastTilt;
    private decimal _lastOpenInterest;
    private decimal _lastXAxisScale;
    private decimal _lastYAxisScale;
    private decimal _lastStrikeStart;
    private decimal _lastStrikeEnd;
    private decimal _lastStrikeStep;

    private double PriceY => CalculatePriceY();
    private double ZeroGammaY => CalculateZeroGammaY();

    private IEnumerable<string> YAxisLabels => CalculateYAxisLabels();
    private IEnumerable<string> XAxisLabels => CalculateXAxisLabels();

    protected override void OnInitialized()
    {
        StateService.OnStateChanged += OnStateChanged;
        CalculateGridLines();
        CalculateBars();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_jsInitialized)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("GexInterop.initAxisDrag", _dotNetRef);
            _jsInitialized = true;
        }
    }

    private void OnStateChanged()
    {
        CalculateBars();
        StateHasChanged();
    }

    private void CalculateGridLines()
    {
        var lines = new List<GridLine>();

        foreach (var y in ChartConfiguration.HorizontalGridLines)
        {
            lines.Add(new GridLine
            {
                X1 = 0,
                Y1 = y,
                X2 = 100,
                Y2 = y,
                LineType = "horizontal"
            });
        }

        foreach (var x in ChartConfiguration.VerticalGridLines)
        {
            lines.Add(new GridLine
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = 100,
                LineType = "vertical"
            });
        }

        GridLines = lines;
    }

    private void CalculateBars()
    {
        var state = StateService.State;

        // Memoization: Skip recalculation if values haven't changed
        if (GammaBars.Count > 0 &&
            _lastPrice == state.Price &&
            _lastTilt == state.Tilt &&
            _lastOpenInterest == state.OpenInterest &&
            _lastXAxisScale == state.XAxisScale &&
            _lastYAxisScale == state.YAxisScale &&
            _lastStrikeStart == state.StrikeStart &&
            _lastStrikeEnd == state.StrikeEnd &&
            _lastStrikeStep == state.StrikeStep)
        {
            return;
        }

        // Cache current values for next comparison
        _lastPrice = state.Price;
        _lastTilt = state.Tilt;
        _lastOpenInterest = state.OpenInterest;
        _lastXAxisScale = state.XAxisScale;
        _lastYAxisScale = state.YAxisScale;
        _lastStrikeStart = state.StrikeStart;
        _lastStrikeEnd = state.StrikeEnd;
        _lastStrikeStep = state.StrikeStep;

        var bars = new List<GammaBar>();

        var strikeStart = state.StrikeStart;
        var strikeEnd = state.StrikeEnd;
        var strikeStep = state.StrikeStep;

        // Calculate strike count using named constants
        var strikeCount = (int)Math.Max(ChartConstants.MinStrikeCount,
            Math.Min(ChartConstants.MaxStrikeCount, (strikeEnd - strikeStart) / strikeStep));

        var priceRange = strikeEnd - strikeStart;
        var centerIndex = priceRange > 0
            ? (int)((state.Price - strikeStart) / priceRange * strikeCount)
            : strikeCount / 2;

        // Zero gamma level using named constant
        var zeroGamma = state.Price * (1 + Math.Abs(state.Tilt) * ChartConstants.ZeroGammaTiltMultiplier);
        var zeroIndex = priceRange > 0
            ? (int)((zeroGamma - strikeStart) / priceRange * strikeCount)
            : centerIndex;

        var centerYPercent = 100.0 - ((double)centerIndex / strikeCount) * 100.0;

        // S² scaling using named constants
        var currentS2 = state.Price * state.Price;
        var priceFactor = currentS2 / ChartConstants.BaselineSpotSquared;
        var oiFactor = state.OpenInterest / ChartConstants.BaselineOpenInterest;

        for (int i = 0; i < strikeCount; i++)
        {
            var dist = Math.Abs(i - centerIndex);

            // Gaussian distribution for gamma simulation
            var sigma = ChartConstants.Sigma;
            var gammaRaw = Math.Exp(-(dist * dist) / (2.0 * (sigma / 2.0) * (sigma / 2.0)));
            var netGamma = gammaRaw * (double)state.Tilt;

            // Calculate value using named constant
            double val;
            if (IsAbsolute)
            {
                val = netGamma * ChartConstants.GammaScaleFactor * (double)priceFactor * (double)oiFactor;
            }
            else
            {
                val = netGamma * ChartConstants.GammaScaleFactor;
            }

            var baseY = 100.0 - ((double)i / strikeCount) * 100.0;
            var scaledY = centerYPercent + (baseY - centerYPercent) * (double)state.YAxisScale;

            var scaledVal = val * (double)state.XAxisScale;
            var width = Math.Min(Math.Abs(scaledVal), ChartConstants.MaxBarWidth);

            var x = val < 0 ? 50 - width : 50;
            var colorClass = val < 0 ? "bar-negative" : "bar-positive";

            // Saturation warning using named constant
            if (IsAbsolute && Math.Abs(val) > ChartConstants.SaturationThreshold / (double)state.XAxisScale)
            {
                colorClass = "bar-saturated";
            }

            // Off-screen culling using named constants
            var opacity = (scaledY < ChartConstants.OffScreenMinY || scaledY > ChartConstants.OffScreenMaxY)
                ? 0.0
                : ChartConstants.DefaultBarOpacity;

            var barHeight = Math.Max(ChartConstants.MinBarHeight,
                (ChartConstants.BarHeightBase / strikeCount) * (double)state.YAxisScale);

            bars.Add(new GammaBar
            {
                X = x,
                Y = scaledY,
                Width = width,
                Height = barHeight,
                ColorClass = colorClass,
                Opacity = opacity
            });
        }

        GammaBars = bars;
    }

    private double CalculatePriceY()
    {
        var state = StateService.State;
        var strikeStart = state.StrikeStart;
        var strikeEnd = state.StrikeEnd;
        var priceRange = strikeEnd - strikeStart;

        if (priceRange <= 0) return ChartConstants.DefaultCenterY;

        var strikeCount = (int)Math.Max(ChartConstants.MinStrikeCount,
            Math.Min(ChartConstants.MaxStrikeCount, priceRange / state.StrikeStep));
        var centerIndex = (int)((state.Price - strikeStart) / priceRange * strikeCount);
        return Math.Max(0, Math.Min(100, 100.0 - ((double)centerIndex / strikeCount) * 100.0));
    }

    private double CalculateZeroGammaY()
    {
        var state = StateService.State;
        var strikeStart = state.StrikeStart;
        var strikeEnd = state.StrikeEnd;
        var priceRange = strikeEnd - strikeStart;

        if (priceRange <= 0) return ChartConstants.DefaultCenterY;

        var zeroGamma = state.Price * (1 + Math.Abs(state.Tilt) * ChartConstants.ZeroGammaTiltMultiplier);
        var strikeCount = (int)Math.Max(ChartConstants.MinStrikeCount,
            Math.Min(ChartConstants.MaxStrikeCount, priceRange / state.StrikeStep));
        var zeroIndex = (int)((zeroGamma - strikeStart) / priceRange * strikeCount);
        var centerIndex = (int)((state.Price - strikeStart) / priceRange * strikeCount);

        var centerYPercent = 100.0 - ((double)centerIndex / strikeCount) * 100.0;
        var zeroYBase = 100.0 - ((double)zeroIndex / strikeCount) * 100.0;
        var zeroY = centerYPercent + (zeroYBase - centerYPercent) * (double)state.YAxisScale;

        return Math.Max(0, Math.Min(100, zeroY));
    }

    private IEnumerable<string> CalculateYAxisLabels()
    {
        var state = StateService.State;
        var strikeStart = state.StrikeStart;
        var strikeEnd = state.StrikeEnd;
        var range = strikeEnd - strikeStart;
        var step = range / 4;

        return new[]
        {
            $"${strikeEnd:F0}",
            $"${strikeEnd - step:F0}",
            $"${strikeStart + range / 2:F0}",
            $"${strikeStart + step:F0}",
            $"${strikeStart:F0}"
        };
    }

    private IEnumerable<string> CalculateXAxisLabels()
    {
        var max = ChartConfiguration.MaxGammaValue;
        var mid = ChartConfiguration.MidGammaValue;

        if (IsAbsolute)
        {
            return new[] { $"-${max}B", $"-${mid}B", "0 (γ)", $"+${mid}B", $"+${max}B" };
        }
        return new[] { $"-{max}", $"-{mid}", "0 (γ)", $"+{mid}", $"+{max}" };
    }

    // --- Interactive Controls (Wheel) ---

    private void OnChartWheel(WheelEventArgs e)
    {
        if (StateService.State.IsSimulating) return;

        // Throttle wheel events
        var now = DateTime.UtcNow;
        if ((now - _lastChartWheelTime).TotalMilliseconds < ChartConstants.WheelThrottleMs)
            return;
        _lastChartWheelTime = now;

        var step = e.ShiftKey ? 10m : 2m;
        var delta = e.DeltaY > 0 ? -step : step;

        if (StateService.State.InvertScroll)
            delta = -delta;

        var newPrice = StateService.State.Price + delta;
        newPrice = Math.Max(StateService.State.StrikeStart, Math.Min(StateService.State.StrikeEnd, newPrice));
        StateService.UpdatePrice(newPrice);
    }

    private void OnYAxisWheel(WheelEventArgs e)
    {
        // Throttle wheel events
        var now = DateTime.UtcNow;
        if ((now - _lastYAxisWheelTime).TotalMilliseconds < ChartConstants.WheelThrottleMs)
            return;
        _lastYAxisWheelTime = now;

        var delta = e.DeltaY > 0 ? 0.1m : -0.1m;
        StateService.AdjustYAxisScale(delta);
    }

    private void OnXAxisWheel(WheelEventArgs e)
    {
        // Throttle wheel events
        var now = DateTime.UtcNow;
        if ((now - _lastXAxisWheelTime).TotalMilliseconds < ChartConstants.WheelThrottleMs)
            return;
        _lastXAxisWheelTime = now;

        var delta = e.DeltaY > 0 ? 0.1m : -0.1m;
        StateService.AdjustXAxisScale(delta);
    }

    private void OnYAxisDoubleClick() => StateService.ResetView();
    private void OnXAxisDoubleClick() => StateService.ResetView();

    // --- Zoom Button Controls ---

    private void OnZoomIn()
    {
        StateService.AdjustXAxisScale(0.2m);
        StateService.AdjustYAxisScale(0.2m);
    }

    private void OnZoomOut()
    {
        StateService.AdjustXAxisScale(-0.2m);
        StateService.AdjustYAxisScale(-0.2m);
    }

    private void OnResetZoom() => StateService.ResetView();

    // --- Crosshair Controls ---

    private void OnChartMouseMove(MouseEventArgs e)
    {
        if (_isDragging) return;

        // Use OffsetX/OffsetY which are relative to the target element
        // Estimate chart area dimensions (SVG is most of the container minus x-axis ~30px)
        // For percentage, we approximate based on typical layout
        var estimatedWidth = 400.0; // Will be overridden by actual position
        var estimatedHeight = 350.0;

        // Convert offset to percentage (0-100)
        // OffsetX/OffsetY are in pixels relative to the element
        _crosshairX = Math.Max(0, Math.Min(100, (e.OffsetX / estimatedWidth) * 100));
        _crosshairY = Math.Max(0, Math.Min(85, (e.OffsetY / estimatedHeight) * 100)); // Cap at 85% to account for x-axis

        var state = StateService.State;
        var strikeRange = state.StrikeEnd - state.StrikeStart;

        // Y position maps to price (inverted - top is high price)
        var pricePercent = 1.0 - (_crosshairY / 85.0); // 85% is the chart area
        _crosshairPrice = state.StrikeStart + (decimal)pricePercent * strikeRange;

        // X position maps to gamma value
        var gammaPercent = (_crosshairX / 100.0) - 0.5; // -0.5 to +0.5
        var gammaValue = gammaPercent * 2 * ChartConfiguration.MaxGammaValue;
        _crosshairGamma = IsAbsolute
            ? $"{gammaValue:+0.0;-0.0;0}B"
            : $"{gammaValue:+0.0;-0.0;0}";

        _showCrosshair = true;
        StateHasChanged();
    }

    private void OnChartMouseLeave(MouseEventArgs e)
    {
        _showCrosshair = false;
        StateHasChanged();
    }

    // --- Interactive Controls (Drag) ---

    private async Task OnYAxisMouseDown(MouseEventArgs e)
    {
        _isDragging = true;
        await JS.InvokeVoidAsync("GexInterop.startYAxisDrag", e.ClientY, (double)StateService.State.YAxisScale);
    }

    private async Task OnXAxisMouseDown(MouseEventArgs e)
    {
        _isDragging = true;
        await JS.InvokeVoidAsync("GexInterop.startXAxisDrag", e.ClientX, (double)StateService.State.XAxisScale);
    }

    private async Task OnChartMouseDown(MouseEventArgs e)
    {
        if (StateService.State.IsSimulating) return;
        _isDragging = true;
        await JS.InvokeVoidAsync("GexInterop.startChartDrag", e.ClientY, (double)StateService.State.Price);
    }

    [JSInvokable]
    public void OnYAxisDrag(double newScale)
    {
        StateService.State.YAxisScale = (decimal)newScale;
        StateService.AdjustYAxisScale(0);
    }

    [JSInvokable]
    public void OnXAxisDrag(double newScale)
    {
        StateService.State.XAxisScale = (decimal)newScale;
        StateService.AdjustXAxisScale(0);
    }

    [JSInvokable]
    public void OnChartDrag(double newPrice)
    {
        var clampedPrice = Math.Max((double)StateService.State.StrikeStart,
                                    Math.Min((double)StateService.State.StrikeEnd, newPrice));
        StateService.UpdatePrice((decimal)clampedPrice);
    }

    [JSInvokable]
    public void OnDragEnd()
    {
        _isDragging = false;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        StateService.OnStateChanged -= OnStateChanged;
        if (_jsInitialized)
        {
            try
            {
                await JS.InvokeVoidAsync("GexInterop.disposeAxisDrag");
            }
            catch (JSDisconnectedException) { }
        }
        _dotNetRef?.Dispose();
    }

    private record GammaBar
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public string ColorClass { get; init; } = "";
        public double Opacity { get; init; }
    }

    private record GridLine
    {
        public double X1 { get; init; }
        public double Y1 { get; init; }
        public double X2 { get; init; }
        public double Y2 { get; init; }
        public string LineType { get; init; } = "";
    }
}
