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
}
