using GexVisor.UI.Configuration;

namespace GexVisor.UI.Utilities;

/// <summary>
/// Shared utilities for correlation analysis display.
/// </summary>
public static class CorrelationHelpers
{
    /// <summary>
    /// Get CSS class for correlation strength indicator based on absolute correlation value.
    /// </summary>
    /// <param name="correlation">Correlation coefficient (-1 to 1)</param>
    /// <returns>CSS class name: "strong", "moderate", or "weak"</returns>
    public static string GetCssClass(decimal correlation)
    {
        return Math.Abs(correlation) switch
        {
            >= AppConstants.Correlation.StrongCorrelationThreshold => "strong",
            >= AppConstants.Correlation.ModerateCorrelationThreshold => "moderate",
            _ => "weak"
        };
    }

    /// <summary>
    /// Get correlation strength label for display.
    /// </summary>
    /// <param name="correlation">Correlation coefficient (-1 to 1)</param>
    /// <returns>Human-readable label: "Strong", "Moderate", or "Weak"</returns>
    public static string GetStrengthLabel(decimal correlation)
    {
        return Math.Abs(correlation) switch
        {
            >= AppConstants.Correlation.StrongCorrelationThreshold => "Strong",
            >= AppConstants.Correlation.ModerateCorrelationThreshold => "Moderate",
            _ => "Weak"
        };
    }
}
