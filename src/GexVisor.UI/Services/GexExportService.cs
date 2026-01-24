using System.Globalization;
using System.Text;
using System.Text.Json;
using GexVisor.Core;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Implementation of GEX data export to CSV and JSON formats.
/// </summary>
public class GexExportService : IGexExportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ExportTimelineToCsv(IEnumerable<GexDataPoint> timeline, string symbol)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Date,Symbol,Price,GEX,CallGEX,PutGEX,ZeroGamma,MaxGamma,Regime,CallOI,PutOI,Contracts,Quality,RegimeDays,Label");

        foreach (var point in timeline)
        {
            sb.AppendLine(string.Join(",",
                point.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EscapeCsv(symbol),
                point.Price,
                point.Gex,
                point.CallGex,
                point.PutGex,
                point.ZeroGamma,
                point.MaxGamma,
                EscapeCsv(point.Regime),
                point.CallOi,
                point.PutOi,
                point.Contracts,
                point.Quality,
                point.RegimeDays,
                EscapeCsv(point.Label)));
        }

        return sb.ToString();
    }

    public string ExportTimelineToJson(IEnumerable<GexDataPoint> timeline, string symbol)
    {
        var export = new
        {
            Symbol = symbol,
            ExportedAt = DateTime.UtcNow,
            DataPoints = timeline.Select(p => new
            {
                Date = p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                p.Price,
                Gex = p.Gex,
                CallGex = p.CallGex,
                PutGex = p.PutGex,
                ZeroGamma = p.ZeroGamma,
                MaxGamma = p.MaxGamma,
                p.Regime,
                CallOi = p.CallOi,
                PutOi = p.PutOi,
                p.Contracts,
                p.Quality,
                p.RegimeDays,
                p.Label,
                p.IsNegativeGamma,
                PriceRelativeToZeroGamma = p.PriceRelativeToZeroGamma
            }).ToList()
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }

    public string ExportStrikeGammasToCsv(IEnumerable<StrikeGamma> strikes, string symbol, decimal spotPrice)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Strike,CallGEX,PutGEX,NetGEX,Contracts,OpenInterest,DistanceFromSpot");

        foreach (var strike in strikes.OrderBy(s => s.StrikePrice))
        {
            var distanceFromSpot = strike.StrikePrice - spotPrice;
            sb.AppendLine(string.Join(",",
                strike.StrikePrice,
                strike.CallGex,
                strike.PutGex,
                strike.NetGex,
                strike.ContractsCount,
                strike.TotalOpenInterest,
                distanceFromSpot));
        }

        return sb.ToString();
    }

    public string ExportStrikeGammasToJson(IEnumerable<StrikeGamma> strikes, string symbol, decimal spotPrice)
    {
        var export = new
        {
            Symbol = symbol,
            SpotPrice = spotPrice,
            ExportedAt = DateTime.UtcNow,
            Strikes = strikes.OrderBy(s => s.StrikePrice).Select(s => new
            {
                Strike = s.StrikePrice,
                CallGex = s.CallGex,
                PutGex = s.PutGex,
                NetGex = s.NetGex,
                Contracts = s.ContractsCount,
                OpenInterest = s.TotalOpenInterest,
                DistanceFromSpot = s.StrikePrice - spotPrice
            }).ToList()
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }

    /// <summary>
    /// Escape a value for CSV output. Handles commas, quotes, and newlines.
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
