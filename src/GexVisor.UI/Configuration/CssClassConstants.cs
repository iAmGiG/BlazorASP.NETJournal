namespace GexVisor.UI.Configuration;

/// <summary>
/// Centralized CSS class name constants used across components.
/// Eliminates magic strings and ensures consistency.
/// </summary>
public static class CssClassConstants
{
    /// <summary>
    /// Component state classes for interactive elements.
    /// </summary>
    public static class State
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
        public const string Featured = "featured";
        public const string Live = "live";
        public const string Stale = "stale";
        public const string Cached = "cached";
    }

    /// <summary>
    /// GEX regime indicator classes for gamma exposure states.
    /// </summary>
    public static class Regime
    {
        public const string Positive = "positive";
        public const string Negative = "negative";
        public const string Neutral = "neutral";
    }

    /// <summary>
    /// Color modifier classes for theming.
    /// </summary>
    public static class Color
    {
        public const string Cyan = "cyan";
        public const string Red = "red";
        public const string Yellow = "yellow";
        public const string Purple = "purple";
        public const string Green = "green";
        public const string Blue = "blue";
    }

    /// <summary>
    /// Layout and structure classes for common UI patterns.
    /// </summary>
    public static class Layout
    {
        public const string PanelHeader = "panel-header";
        public const string PanelTitle = "panel-title";
        public const string MetricCard = "metric-card";
        public const string MetricLabel = "metric-label";
        public const string MetricValue = "metric-value";
        public const string DataOverlay = "data-overlay";
        public const string DataRow = "data-row";
        public const string DataVal = "data-val";
    }

    /// <summary>
    /// Correlation strength indicator classes.
    /// </summary>
    public static class Correlation
    {
        public const string Strong = "strong";
        public const string Moderate = "moderate";
        public const string Weak = "weak";
    }
}
