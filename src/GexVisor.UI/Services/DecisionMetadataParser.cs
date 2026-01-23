using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.Core;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Parses autotrader log files and extracts decision metadata.
/// Supports multiple log formats and can be extended for custom formats.
/// </summary>
public class DecisionMetadataParser
{
    /// <summary>
    /// Parse JSON-formatted autotrader log and create OptionsLog + TradeDecision pairs.
    /// </summary>
    public (List<OptionsLog> Trades, List<TradeDecision> Decisions) ParseJsonLog(string jsonContent)
    {
        var trades = new List<OptionsLog>();
        var decisions = new List<TradeDecision>();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var logEntries = JsonSerializer.Deserialize<List<AutotraderLogEntry>>(jsonContent, options);
            if (logEntries == null)
            {
                return (trades, decisions);
            }

            foreach (var entry in logEntries)
            {
                var trade = MapToOptionsLog(entry);
                var decision = MapToTradeDecision(entry, trade.Id);

                trades.Add(trade);
                decisions.Add(decision);
            }
        }
        catch (JsonException)
        {
            // Invalid JSON format
            return (trades, decisions);
        }

        return (trades, decisions);
    }

    /// <summary>
    /// Parse CSV-formatted autotrader log.
    /// </summary>
    public (List<OptionsLog> Trades, List<TradeDecision> Decisions) ParseCsvLog(string csvContent)
    {
        var trades = new List<OptionsLog>();
        var decisions = new List<TradeDecision>();

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) // Must have header + at least one row
        {
            return (trades, decisions);
        }

        var header = ParseCsvLine(lines[0]);
        var columnMap = MapCsvColumns(header);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < header.Length)
            {
                continue;
            }

            // Skip rows with invalid required numeric fields
            if (!IsValidCsvRow(values, columnMap))
            {
                continue;
            }

            var trade = MapCsvToOptionsLog(values, columnMap);
            var decision = MapCsvToTradeDecision(values, columnMap, trade.Id);

            trades.Add(trade);
            decisions.Add(decision);
        }

        return (trades, decisions);
    }

    // === JSON Mapping ===

    private OptionsLog MapToOptionsLog(AutotraderLogEntry entry)
    {
        return new OptionsLog
        {
            Id = entry.TradeId ?? Guid.NewGuid(),
            CreatedDate = entry.Timestamp,
            Ticker = entry.Symbol,
            OptionTradeType = ParseTradeType(entry.Action),
            EntryPrice = entry.EntryPrice,
            ExitPrice = entry.ExitPrice,
            Quantity = entry.Quantity,
            ContractMultiplier = entry.ContractMultiplier ?? 100m,
            StrikePrice = entry.Strike,
            ExpirationDate = entry.Expiration,
            Notes = entry.Notes,
            Analysis = entry.Analysis
        };
    }

    private TradeDecision MapToTradeDecision(AutotraderLogEntry entry, Guid tradeId)
    {
        return new TradeDecision
        {
            Id = Guid.NewGuid(),
            TradeId = tradeId,
            ActivePatterns = entry.ActivePatterns ?? [],
            PrimaryTrigger = entry.PrimaryTrigger,
            ConfidenceScore = entry.ConfidenceScore ?? 0,
            SignalStrengths = entry.SignalStrengths ?? new(),
            RegimeType = entry.RegimeType,
            GexLevel = entry.GexLevel,
            IvLevel = entry.IvLevel,
            IsNegativeGamma = entry.IsNegativeGamma,
            SpotPrice = entry.SpotPrice,
            DecisionRationale = entry.Rationale,
            RiskAssessment = entry.RiskAssessment,
            ProfitTarget = entry.ProfitTarget,
            AdditionalContext = entry.AdditionalContext,
            DecisionTime = entry.DecisionTime ?? entry.Timestamp,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static OptionsLog.TradeType ParseTradeType(string? action)
    {
        return action?.ToUpperInvariant() switch
        {
            "BTO" => OptionsLog.TradeType.BTO,
            "BTC" => OptionsLog.TradeType.BTC,
            "STO" => OptionsLog.TradeType.STO,
            "STC" => OptionsLog.TradeType.STC,
            _ => OptionsLog.TradeType.BTO
        };
    }

    // === CSV Mapping ===

    private Dictionary<string, int> MapCsvColumns(string[] header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            map[header[i].Trim()] = i;
        }
        return map;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote - add single quote and skip next character
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    // Toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of field
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add the last field
        values.Add(currentValue.ToString().Trim());

        return values.ToArray();
    }

    private bool IsValidCsvRow(string[] values, Dictionary<string, int> columns)
    {
        // Check if required numeric field EntryPrice is valid
        var entryPriceStr = GetValue(values, columns, "EntryPrice") ?? GetValue(values, columns, "Entry");
        if (!string.IsNullOrWhiteSpace(entryPriceStr) && !decimal.TryParse(entryPriceStr, out _))
        {
            return false; // Invalid EntryPrice, skip row
        }

        return true;
    }

    private OptionsLog MapCsvToOptionsLog(string[] values, Dictionary<string, int> columns)
    {
        return new OptionsLog
        {
            Id = Guid.NewGuid(),
            CreatedDate = GetDateValue(values, columns, "Timestamp") ?? GetDateValue(values, columns, "Date") ?? DateTime.UtcNow,
            Ticker = GetValue(values, columns, "Symbol"),
            OptionTradeType = ParseTradeType(GetValue(values, columns, "Action") ?? GetValue(values, columns, "Type")),
            EntryPrice = GetDecimalValue(values, columns, "EntryPrice") ?? GetDecimalValue(values, columns, "Entry") ?? 0,
            ExitPrice = GetDecimalValue(values, columns, "ExitPrice") ?? GetDecimalValue(values, columns, "Exit"),
            Quantity = GetDecimalValue(values, columns, "Quantity") ?? 1,
            ContractMultiplier = GetDecimalValue(values, columns, "ContractMultiplier") ?? GetDecimalValue(values, columns, "Multiplier") ?? 100m,
            StrikePrice = GetDecimalValue(values, columns, "StrikePrice") ?? GetDecimalValue(values, columns, "Strike") ?? 0,
            ExpirationDate = GetDateValue(values, columns, "Expiration") ?? GetDateValue(values, columns, "ExpirationDate"),
            Notes = GetValue(values, columns, "Notes"),
            Analysis = GetValue(values, columns, "Analysis")
        };
    }

    private TradeDecision MapCsvToTradeDecision(string[] values, Dictionary<string, int> columns, Guid tradeId)
    {
        var patterns = GetValue(values, columns, "Patterns");
        var activePatterns = !string.IsNullOrWhiteSpace(patterns)
            ? patterns.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        return new TradeDecision
        {
            Id = Guid.NewGuid(),
            TradeId = tradeId,
            ActivePatterns = activePatterns,
            PrimaryTrigger = GetValue(values, columns, "Trigger") ?? GetValue(values, columns, "PrimaryTrigger"),
            ConfidenceScore = GetDecimalValue(values, columns, "Confidence") ?? GetDecimalValue(values, columns, "ConfidenceScore") ?? 0,
            RegimeType = GetValue(values, columns, "Regime") ?? GetValue(values, columns, "RegimeType"),
            GexLevel = GetDecimalValue(values, columns, "GEX") ?? GetDecimalValue(values, columns, "GexLevel"),
            IvLevel = GetDecimalValue(values, columns, "IV") ?? GetDecimalValue(values, columns, "IvLevel"),
            SpotPrice = GetDecimalValue(values, columns, "Spot") ?? GetDecimalValue(values, columns, "SpotPrice"),
            DecisionRationale = GetValue(values, columns, "Rationale") ?? GetValue(values, columns, "DecisionRationale"),
            DecisionTime = GetDateValue(values, columns, "Timestamp") ?? GetDateValue(values, columns, "Date") ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string? GetValue(string[] values, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out int index))
        {
            return null;
        }

        return index < values.Length ? values[index] : null;
    }

    private decimal? GetDecimalValue(string[] values, Dictionary<string, int> columns, string columnName)
    {
        var value = GetValue(values, columns, columnName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, out var result) ? result : null;
    }

    private DateTime? GetDateValue(string[] values, Dictionary<string, int> columns, string columnName)
    {
        var value = GetValue(values, columns, columnName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var result) ? result : null;
    }
}

/// <summary>
/// Represents a single autotrader log entry in JSON format.
/// This is the expected input schema for JSON logs.
/// </summary>
public class AutotraderLogEntry
{
    public Guid? TradeId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Symbol { get; set; }
    public string? Action { get; set; } // BTO/BTC/STO/STC
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? ContractMultiplier { get; set; }

    [JsonPropertyName("strikePrice")]
    public decimal Strike { get; set; }

    public DateTime? Expiration { get; set; }
    public string? Notes { get; set; }
    public string? Analysis { get; set; }

    // Decision metadata
    public List<string>? ActivePatterns { get; set; }
    public string? PrimaryTrigger { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public Dictionary<string, decimal>? SignalStrengths { get; set; }
    public string? RegimeType { get; set; }
    public decimal? GexLevel { get; set; }
    public decimal? IvLevel { get; set; }
    public bool? IsNegativeGamma { get; set; }
    public decimal? SpotPrice { get; set; }

    [JsonPropertyName("rationale")]
    public string? Rationale { get; set; }

    public string? RiskAssessment { get; set; }
    public string? ProfitTarget { get; set; }
    public string? AdditionalContext { get; set; }
    public DateTime? DecisionTime { get; set; }
}
