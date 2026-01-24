// Copyright (c) GexVisor. All rights reserved.

using System.Text.Json;
using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Models;
using GexVisor.UI.Services;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for GexExportService CSV and JSON export functionality.
/// </summary>
public class GexExportServiceTests
{
    private readonly GexExportService _service = new();

    [Fact]
    public void ExportTimelineToCsv_CreatesValidCsvWithHeader()
    {
        // Arrange
        var timeline = CreateTestTimeline(3);

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(4); // Header + 3 data rows
        lines[0].Should().StartWith("Date,Symbol,Price,GEX");
    }

    [Fact]
    public void ExportTimelineToCsv_IncludesAllFields()
    {
        // Arrange
        var timeline = CreateTestTimeline(1);

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var headerFields = lines[0].Trim().Split(',');
        headerFields.Should().Contain("Date");
        headerFields.Should().Contain("Symbol");
        headerFields.Should().Contain("Price");
        headerFields.Should().Contain("GEX");
        headerFields.Should().Contain("CallGEX");
        headerFields.Should().Contain("PutGEX");
        headerFields.Should().Contain("ZeroGamma");
        headerFields.Should().Contain("Regime");
        headerFields.Should().Contain("Label");
    }

    [Fact]
    public void ExportTimelineToCsv_EscapesCommasInLabels()
    {
        // Arrange
        var timeline = new List<GexDataPoint>
        {
            CreateDataPoint(label: "Label, with comma"),
        };

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        csv.Should().Contain("\"Label, with comma\"");
    }

    [Fact]
    public void ExportTimelineToCsv_EscapesQuotesInLabels()
    {
        // Arrange
        var timeline = new List<GexDataPoint>
        {
            CreateDataPoint(label: "Label with \"quotes\""),
        };

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        csv.Should().Contain("\"Label with \"\"quotes\"\"\"");
    }

    [Fact]
    public void ExportTimelineToCsv_HandlesEmptyTimeline()
    {
        // Arrange
        var timeline = new List<GexDataPoint>();

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1); // Header only
    }

    [Fact]
    public void ExportTimelineToCsv_FormatsDateCorrectly()
    {
        // Arrange
        var timeline = new List<GexDataPoint>
        {
            CreateDataPoint(date: new DateOnly(2024, 3, 15)),
        };

        // Act
        var csv = _service.ExportTimelineToCsv(timeline, "SPY");

        // Assert
        csv.Should().Contain("2024-03-15");
    }

    [Fact]
    public void ExportTimelineToJson_CreatesValidJson()
    {
        // Arrange
        var timeline = CreateTestTimeline(2);

        // Act
        var json = _service.ExportTimelineToJson(timeline, "SPY");

        // Assert
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void ExportTimelineToJson_IncludesSymbol()
    {
        // Arrange
        var timeline = CreateTestTimeline(1);

        // Act
        var json = _service.ExportTimelineToJson(timeline, "QQQ");

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("symbol").GetString().Should().Be("QQQ");
    }

    [Fact]
    public void ExportTimelineToJson_IncludesExportTimestamp()
    {
        // Arrange
        var timeline = CreateTestTimeline(1);

        // Act
        var json = _service.ExportTimelineToJson(timeline, "SPY");

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("exportedAt", out _).Should().BeTrue();
    }

    [Fact]
    public void ExportTimelineToJson_IncludesAllDataPoints()
    {
        // Arrange
        var timeline = CreateTestTimeline(5);

        // Act
        var json = _service.ExportTimelineToJson(timeline, "SPY");

        // Assert
        using var doc = JsonDocument.Parse(json);
        var dataPoints = doc.RootElement.GetProperty("dataPoints");
        dataPoints.GetArrayLength().Should().Be(5);
    }

    [Fact]
    public void ExportTimelineToJson_IncludesDerivedFields()
    {
        // Arrange
        var timeline = new List<GexDataPoint>
        {
            CreateDataPoint(regime: "NEGATIVE_GAMMA"),
        };

        // Act
        var json = _service.ExportTimelineToJson(timeline, "SPY");

        // Assert
        using var doc = JsonDocument.Parse(json);
        var point = doc.RootElement.GetProperty("dataPoints")[0];
        point.GetProperty("isNegativeGamma").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ExportStrikeGammasToCsv_CreatesValidCsvWithHeader()
    {
        // Arrange
        var strikes = CreateTestStrikes(5);

        // Act
        var csv = _service.ExportStrikeGammasToCsv(strikes, "SPY", 450m);

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(6); // Header + 5 strikes
        lines[0].Should().StartWith("Strike,CallGEX,PutGEX");
    }

    [Fact]
    public void ExportStrikeGammasToCsv_OrdersByStrikePrice()
    {
        // Arrange
        var strikes = new List<StrikeGamma>
        {
            new() { StrikePrice = 460m, CallGex = 1m, PutGex = 1m },
            new() { StrikePrice = 440m, CallGex = 1m, PutGex = 1m },
            new() { StrikePrice = 450m, CallGex = 1m, PutGex = 1m },
        };

        // Act
        var csv = _service.ExportStrikeGammasToCsv(strikes, "SPY", 450m);

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[1].Should().StartWith("440,");
        lines[2].Should().StartWith("450,");
        lines[3].Should().StartWith("460,");
    }

    [Fact]
    public void ExportStrikeGammasToCsv_CalculatesDistanceFromSpot()
    {
        // Arrange
        var strikes = new List<StrikeGamma>
        {
            new() { StrikePrice = 460m, CallGex = 1m, PutGex = 1m },
        };

        // Act
        var csv = _service.ExportStrikeGammasToCsv(strikes, "SPY", 450m);

        // Assert
        csv.Should().Contain("10"); // 460 - 450 = 10
    }

    [Fact]
    public void ExportStrikeGammasToJson_CreatesValidJson()
    {
        // Arrange
        var strikes = CreateTestStrikes(3);

        // Act
        var json = _service.ExportStrikeGammasToJson(strikes, "SPY", 450m);

        // Assert
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void ExportStrikeGammasToJson_IncludesSpotPrice()
    {
        // Arrange
        var strikes = CreateTestStrikes(1);

        // Act
        var json = _service.ExportStrikeGammasToJson(strikes, "SPY", 453.50m);

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("spotPrice").GetDecimal().Should().Be(453.50m);
    }

    [Fact]
    public void ExportStrikeGammasToJson_IncludesNetGex()
    {
        // Arrange
        var strikes = new List<StrikeGamma>
        {
            new() { StrikePrice = 450m, CallGex = 10m, PutGex = 3m },
        };

        // Act
        var json = _service.ExportStrikeGammasToJson(strikes, "SPY", 450m);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var strike = doc.RootElement.GetProperty("strikes")[0];
        strike.GetProperty("netGex").GetDecimal().Should().Be(7m); // 10 - 3
    }

    private static List<GexDataPoint> CreateTestTimeline(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateDataPoint(date: new DateOnly(2024, 1, 1).AddDays(i)))
            .ToList();
    }

    private static GexDataPoint CreateDataPoint(
        DateOnly? date = null,
        decimal price = 450m,
        string regime = "POSITIVE_GAMMA",
        string? label = null)
    {
        return new GexDataPoint
        {
            Date = date ?? new DateOnly(2024, 1, 15),
            Price = price,
            Gex = 5.5m,
            CallGex = 8.0m,
            PutGex = 2.5m,
            ZeroGamma = 445m,
            MaxGamma = 455m,
            Regime = regime,
            CallOi = 1000000m,
            PutOi = 800000m,
            Contracts = 50000,
            Quality = 0.95m,
            RegimeDays = 5,
            Label = label,
        };
    }

    private static List<StrikeGamma> CreateTestStrikes(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new StrikeGamma
            {
                StrikePrice = 440m + (i * 5m),
                CallGex = 2m + (i * 0.5m),
                PutGex = 1m + (i * 0.3m),
                ContractsCount = 1000 + (i * 100),
                TotalOpenInterest = 50000m + (i * 5000m),
            })
            .ToList();
    }
}
