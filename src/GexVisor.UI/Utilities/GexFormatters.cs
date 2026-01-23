namespace GexVisor.UI.Utilities;

/// <summary>
/// Shared formatting utilities for GEX display values.
/// </summary>
public static class GexFormatters
{
    /// <summary>
    /// Format GEX value with appropriate unit suffix (T/B/M).
    /// </summary>
    /// <param name="gex">GEX value in billions</param>
    /// <returns>Formatted GEX string with unit suffix (e.g., "$1.5B", "$150M", "$2.3T")</returns>
    public static string FormatGex(decimal gex) => gex switch
    {
        >= 1000 => $"${gex / 1000:F1}T",  // Trillions
        >= 1 => $"${gex:F1}B",            // Billions
        _ => $"${gex * 1000:F0}M"         // Millions
    };
}
