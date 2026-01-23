using System.Globalization;
using System.Text;
using GexVisor.Core;

namespace GexVisor.UI.Components.Charts;

/// <summary>
/// Utility class for exporting chart data to various formats.
/// </summary>
public static class ChartExport
{
    /// <summary>
    /// Export OHLCV bar data to CSV format.
    /// </summary>
    public static string ToCsv(IEnumerable<OhlcvBar> bars, string symbol)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Date,Symbol,Open,High,Low,Close,Volume");

        // Data rows
        foreach (var bar in bars.OrderBy(b => b.Timestamp))
        {
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm},{1},{2:F2},{3:F2},{4:F2},{5:F2},{6}",
                bar.Timestamp,
                symbol.ToUpperInvariant(),
                bar.Open,
                bar.High,
                bar.Low,
                bar.Close,
                bar.Volume));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate a shareable URL with chart state parameters.
    /// </summary>
    public static string BuildShareUrl(string baseUrl, string symbol, string timeframe, DateTime? from = null, DateTime? to = null)
    {
        var sb = new StringBuilder(baseUrl);
        sb.Append(CultureInfo.InvariantCulture, $"?symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}");
        sb.Append(CultureInfo.InvariantCulture, $"&tf={Uri.EscapeDataString(timeframe)}");

        if (from.HasValue)
        {
            sb.Append(CultureInfo.InvariantCulture, $"&from={from.Value:yyyy-MM-dd}");
        }

        if (to.HasValue)
        {
            sb.Append(CultureInfo.InvariantCulture, $"&to={to.Value:yyyy-MM-dd}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parse chart state from URL query parameters.
    /// </summary>
    public static ChartState ParseFromQuery(string? symbol, string? timeframe, string? from, string? to)
    {
        var state = new ChartState
        {
            Symbol = string.IsNullOrWhiteSpace(symbol) ? "SPY" : symbol.ToUpperInvariant(),
            Timeframe = string.IsNullOrWhiteSpace(timeframe) ? "1d" : timeframe
        };

        if (DateTime.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
        {
            state.FromDate = fromDate;
        }

        if (DateTime.TryParse(to, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
        {
            state.ToDate = toDate;
        }

        return state;
    }

    /// <summary>
    /// Generate a filename for CSV export.
    /// </summary>
    public static string GenerateCsvFilename(string symbol, string timeframe)
    {
        return $"{symbol.ToUpperInvariant()}_{timeframe}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
    }
}

/// <summary>
/// Represents chart state for URL sharing.
/// </summary>
public class ChartState
{
    public string Symbol { get; set; } = "SPY";
    public string Timeframe { get; set; } = "1d";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
