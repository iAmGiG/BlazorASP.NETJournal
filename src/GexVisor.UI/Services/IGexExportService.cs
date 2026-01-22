using GexVisor.Core;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for exporting GEX data to CSV and JSON formats.
/// </summary>
public interface IGexExportService
{
    /// <summary>
    /// Export timeline data to CSV format.
    /// </summary>
    string ExportTimelineToCsv(IEnumerable<GexDataPoint> timeline, string symbol);

    /// <summary>
    /// Export timeline data to JSON format.
    /// </summary>
    string ExportTimelineToJson(IEnumerable<GexDataPoint> timeline, string symbol);

    /// <summary>
    /// Export strike-level gamma data to CSV format.
    /// </summary>
    string ExportStrikeGammasToCsv(IEnumerable<StrikeGamma> strikes, string symbol, decimal spotPrice);

    /// <summary>
    /// Export strike-level gamma data to JSON format.
    /// </summary>
    string ExportStrikeGammasToJson(IEnumerable<StrikeGamma> strikes, string symbol, decimal spotPrice);
}
