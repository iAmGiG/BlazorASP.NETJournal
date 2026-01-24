namespace GexVisor.UI.Configuration;

/// <summary>
/// Application-wide constants for configuration values, thresholds, and limits.
/// Centralizes hardcoded values to enable easier maintenance and testing.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// GitHub API configuration constants.
    /// </summary>
    public static class GitHub
    {
        /// <summary>
        /// Maximum number of projects to fetch from GitHub (GraphQL first: limit).
        /// </summary>
        public const int MaxProjectsPerQuery = 20;

        /// <summary>
        /// Maximum number of project items to fetch (GraphQL first: limit).
        /// </summary>
        public const int MaxItemsPerProject = 100;

        /// <summary>
        /// Maximum number of fields to fetch per project.
        /// </summary>
        public const int MaxFieldsPerProject = 20;

        /// <summary>
        /// Maximum number of labels to fetch per issue.
        /// </summary>
        public const int MaxLabelsPerIssue = 10;

        /// <summary>
        /// Maximum number of field values to fetch per item.
        /// </summary>
        public const int MaxFieldValuesPerItem = 10;

        /// <summary>
        /// Polling interval backoff increment in seconds (when GitHub requests slow-down).
        /// </summary>
        public const int PollingBackoffIncrement = 5;

        /// <summary>
        /// Standard field name for status in GitHub Projects v2.
        /// Used in GraphQL queries and field value parsing.
        /// </summary>
        public const string StatusFieldName = "status";

        /// <summary>
        /// Fallback label for items with no status assigned.
        /// </summary>
        public const string NoStatusLabel = "(No Status)";

        /// <summary>
        /// GitHub issue state indicating a closed item.
        /// </summary>
        public const string ClosedState = "CLOSED";
    }

    /// <summary>
    /// Cache expiry durations.
    /// </summary>
    public static class Cache
    {
        /// <summary>
        /// Board state cache expiry duration in minutes.
        /// </summary>
        public const int BoardStateCacheMinutes = 5;
    }

    /// <summary>
    /// UI display limits.
    /// </summary>
    public static class UI
    {
        /// <summary>
        /// Maximum number of tag suggestions to display in autocomplete.
        /// </summary>
        public const int MaxTagSuggestions = 10;
    }

    /// <summary>
    /// Pattern validation thresholds (from gex-llm-patterns research methodology).
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Minimum sample size for statistical significance.
        /// Below this threshold, pattern is considered "Unvalidated" or "Partial".
        /// </summary>
        public const int MinimumSampleSize = 30;

        /// <summary>
        /// Minimum samples required to move from "Unvalidated" to "Partial" status.
        /// </summary>
        public const int MinimumPartialSamples = 10;

        /// <summary>
        /// Minimum win rate (percentage) required for pattern validation.
        /// Patterns with n>=30 and win rate &lt; this threshold are marked "Failed".
        /// </summary>
        public const double MinimumWinRatePercent = 60.0;
    }

    /// <summary>
    /// Financial analysis constants.
    /// </summary>
    public static class Finance
    {
        /// <summary>
        /// Standard number of trading days per year for volatility annualization.
        /// Used in volatility calculations (σ_annual = σ_daily * √252).
        /// </summary>
        public const int TradingDaysPerYear = 252;

        /// <summary>
        /// Default contract multiplier for standard equity options.
        /// Can be overridden for Mini-Options (10) or Futures Options (50).
        /// </summary>
        public const decimal DefaultContractMultiplier = 100m;
    }

    /// <summary>
    /// Cross-asset correlation analysis constants.
    /// </summary>
    public static class Correlation
    {
        /// <summary>
        /// Maximum days between regime flips to consider them correlated.
        /// Used in regime flip correlation calculation.
        /// </summary>
        public const int RegimeFlipCorrelationWindow = 5;

        /// <summary>
        /// Correlation strength threshold for "strong" classification.
        /// Absolute correlation >= this value is considered strong.
        /// </summary>
        public const decimal StrongCorrelationThreshold = 0.7m;

        /// <summary>
        /// Correlation strength threshold for "moderate" classification.
        /// Absolute correlation >= this value (but &lt; strong) is moderate.
        /// </summary>
        public const decimal ModerateCorrelationThreshold = 0.4m;

        /// <summary>
        /// Correlation values below this threshold are considered "weak".
        /// </summary>
        // Weak is anything below ModerateCorrelationThreshold

        /// <summary>
        /// Asset class identifier for index instruments (SPY, SPX, QQQ, etc.).
        /// Used to distinguish index vs single-stock for dispersion analysis.
        /// </summary>
        public const string IndexAssetClass = "Index";
    }

    /// <summary>
    /// Data format constants for consistent parsing and display.
    /// </summary>
    public static class DataFormat
    {
        /// <summary>
        /// Standard ISO 8601 date format (yyyy-MM-dd) used throughout the application.
        /// Required for string-based date comparisons and timeline alignment.
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";
    }

    /// <summary>
    /// Keyboard shortcut definitions for UI components.
    /// Centralized to ensure consistency between sidebar and help overlay.
    /// </summary>
    public static class Keyboard
    {
        /// <summary>
        /// Represents a single keyboard shortcut with descriptions for different contexts.
        /// </summary>
        public record ShortcutItem(string Key, string ShortDesc, string LongDesc);

        /// <summary>
        /// Represents a section of related keyboard shortcuts.
        /// </summary>
        public record ShortcutSection(string Title, ShortcutItem[] Items);

        /// <summary>
        /// Detailed shortcut data organized by section (for help overlay).
        /// </summary>
        public static readonly ShortcutSection[] Sections =
        [
            new("Playback",
            [
                new("Space", "Play/Pause", "Play / Pause simulation"),
                new("←", "Step", "Step backward one data point"),
                new("→", "Step", "Step forward one data point"),
            ]),
            new("Navigation",
            [
                new("↑", "Jump Year", "Jump to next year"),
                new("↓", "Jump Year", "Jump to previous year"),
                new("Home", "Start/End", "Jump to start of timeline"),
                new("End", "Start/End", "Jump to end of timeline"),
            ]),
            new("View",
            [
                new("R", "Reset Zoom", "Reset axis zoom to default"),
                new("F", "Fullscreen", "Toggle fullscreen mode"),
                new("?", "Help", "Show this help overlay"),
            ])
        ];

        /// <summary>
        /// Compact shortcut data with grouped keys (for sidebar display).
        /// </summary>
        public static readonly (string Keys, string Action)[] SidebarShortcuts =
        [
            ("Space", "Play/Pause"),
            ("← →", "Step"),
            ("↑ ↓", "Jump Year"),
            ("Home/End", "Start/End"),
            ("R", "Reset Zoom"),
            ("F", "Fullscreen")
        ];
    }

    /// <summary>
    /// GEX calculation and volatility regime constants.
    /// </summary>
    public static class Gex
    {
        /// <summary>
        /// Volatility multiplier for short gamma regime (amplified moves).
        /// Dealers are short gamma, so hedging activity adds to price moves.
        /// </summary>
        public const decimal ShortGammaVolatilityMultiplier = 1.5m;

        /// <summary>
        /// Volatility multiplier for long gamma regime (dampened moves).
        /// Dealers are long gamma, so hedging activity opposes price moves.
        /// </summary>
        public const decimal LongGammaVolatilityMultiplier = 0.7m;

        /// <summary>
        /// Threshold for trillion display in GEX formatting (in billions).
        /// </summary>
        public const decimal TrillionThreshold = 1000m;

        /// <summary>
        /// Threshold for billion display in GEX formatting (in billions).
        /// </summary>
        public const decimal BillionThreshold = 1m;

        /// <summary>
        /// Conversion factor for millions display in GEX formatting.
        /// </summary>
        public const decimal MillionConversionFactor = 1000m;
    }

    /// <summary>
    /// UI widget and component sizing constants.
    /// </summary>
    public static class UiSizing
    {
        /// <summary>
        /// Default MiniRadar widget size in pixels.
        /// </summary>
        public const int MiniRadarSize = 280;

        /// <summary>
        /// Mobile-optimized MiniRadar max width in pixels.
        /// </summary>
        public const int MiniRadarMobileMaxWidth = 240;

        /// <summary>
        /// Default candlestick chart height in pixels.
        /// </summary>
        public const int CandlestickChartHeight = 350;

        /// <summary>
        /// Mobile candlestick chart height in pixels.
        /// </summary>
        public const int CandlestickChartHeightMobile = 280;

        /// <summary>
        /// Settings save debounce delay in milliseconds.
        /// </summary>
        public const int SettingsSaveDebounceMs = 500;

        /// <summary>
        /// Number of bars in persistence meter visualization.
        /// </summary>
        public const int PersistenceMeterBarCount = 10;

        /// <summary>
        /// Default sparkline chart width in pixels.
        /// </summary>
        public const int SparklineWidth = 200;

        /// <summary>
        /// Default sparkline chart height in pixels.
        /// </summary>
        public const int SparklineHeight = 60;

        /// <summary>
        /// Default sparkline chart padding in pixels.
        /// </summary>
        public const int SparklinePadding = 4;
    }

    /// <summary>
    /// Range input limits for GEX simulation controls.
    /// </summary>
    public static class GexRanges
    {
        /// <summary>
        /// Open Interest range minimum (log scale).
        /// </summary>
        public const decimal OpenInterestMin = 3m;

        /// <summary>
        /// Open Interest range maximum (log scale).
        /// </summary>
        public const decimal OpenInterestMax = 12m;

        /// <summary>
        /// Open Interest step increment.
        /// </summary>
        public const decimal OpenInterestStep = 0.1m;

        /// <summary>
        /// Tilt range minimum (put-heavy).
        /// </summary>
        public const decimal TiltMin = -0.5m;

        /// <summary>
        /// Tilt range maximum (call-heavy).
        /// </summary>
        public const decimal TiltMax = 0.5m;

        /// <summary>
        /// Tilt step increment.
        /// </summary>
        public const decimal TiltStep = 0.01m;
    }

    /// <summary>
    /// Comparison dashboard selection limits.
    /// </summary>
    public static class ComparisonLimits
    {
        /// <summary>
        /// Minimum assets required for cross-asset comparison.
        /// </summary>
        public const int MinAssets = 2;

        /// <summary>
        /// Maximum assets allowed for cross-asset comparison.
        /// </summary>
        public const int MaxAssets = 4;
    }

    /// <summary>
    /// Chart styling constants for ApexCharts and SVG visualizations.
    /// </summary>
    public static class ChartColors
    {
        /// <summary>
        /// Upward/bullish candlestick color (green).
        /// </summary>
        public const string CandlestickUpward = "#00c853";

        /// <summary>
        /// Downward/bearish candlestick color (red).
        /// </summary>
        public const string CandlestickDownward = "#ff5252";

        /// <summary>
        /// Long/buy trade marker color (green).
        /// </summary>
        public const string LongColor = "#00c853";

        /// <summary>
        /// Short/sell trade marker color (red).
        /// </summary>
        public const string ShortColor = "#ff5252";

        /// <summary>
        /// Chart grid border color (subtle white).
        /// </summary>
        public const string GridBorder = "rgba(255, 255, 255, 0.1)";

        /// <summary>
        /// Axis label text color.
        /// </summary>
        public const string AxisLabel = "#888";

        /// <summary>
        /// Axis border/tick color.
        /// </summary>
        public const string AxisBorder = "rgba(255, 255, 255, 0.2)";

        /// <summary>
        /// Transparent background for chart areas.
        /// </summary>
        public const string Transparent = "transparent";

        /// <summary>
        /// White stroke color for markers.
        /// </summary>
        public const string MarkerStroke = "#ffffff";
    }

    /// <summary>
    /// Alert system configuration constants.
    /// </summary>
    public static class Alerts
    {
        /// <summary>
        /// Polling interval for alert checking in milliseconds.
        /// </summary>
        public const int CheckIntervalMs = 5000;

        /// <summary>
        /// Default cooldown between same-type alerts in seconds.
        /// Prevents alert spam during volatile periods.
        /// </summary>
        public const int DefaultCooldownSeconds = 300;

        /// <summary>
        /// Default auto-dismiss delay for toasts in seconds.
        /// </summary>
        public const int DefaultAutoDismissSeconds = 30;

        /// <summary>
        /// Maximum concurrent toasts displayed at once.
        /// </summary>
        public const int MaxVisibleToasts = 5;

        /// <summary>
        /// Maximum alerts retained in history.
        /// </summary>
        public const int MaxHistoryCount = 50;

        /// <summary>
        /// Default upper GEX threshold in billions.
        /// </summary>
        public const decimal DefaultGexUpperThreshold = 5.0m;

        /// <summary>
        /// Default lower GEX threshold in billions.
        /// </summary>
        public const decimal DefaultGexLowerThreshold = -2.0m;

        /// <summary>
        /// Default price movement percentage threshold.
        /// </summary>
        public const decimal DefaultPriceMovementThreshold = 1.0m;

        /// <summary>
        /// Toast animation duration in milliseconds.
        /// </summary>
        public const int ToastAnimationMs = 300;
    }
}
