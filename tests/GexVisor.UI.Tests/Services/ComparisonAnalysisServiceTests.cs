// Copyright (c) GexVisor. All rights reserved.

using System.Globalization;
using FluentAssertions;
using GexVisor.UI.Models;
using GexVisor.UI.Services;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for ComparisonAnalysisService statistical calculations.
/// Validates correctness of Pearson correlation, regime alignment, and other metrics.
/// </summary>
public class ComparisonAnalysisServiceTests
{
    private readonly ComparisonAnalysisService _service;

    public ComparisonAnalysisServiceTests()
    {
        _service = new ComparisonAnalysisService();
    }

    [Fact]
    public void CalculatePearsonCorrelation_PerfectPositiveCorrelation_Returns1()
    {
        // Arrange
        var x = new List<decimal> { 1, 2, 3, 4, 5 };
        var y = new List<decimal> { 2, 4, 6, 8, 10 }; // y = 2x

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().BeApproximately(1.0m, 0.001m);
    }

    [Fact]
    public void CalculatePearsonCorrelation_PerfectNegativeCorrelation_ReturnsNegative1()
    {
        // Arrange
        var x = new List<decimal> { 1, 2, 3, 4, 5 };
        var y = new List<decimal> { 10, 8, 6, 4, 2 }; // inverse relationship

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().BeApproximately(-1.0m, 0.001m);
    }

    [Fact]
    public void CalculatePearsonCorrelation_NoCorrelation_ReturnsNearZero()
    {
        // Arrange - random uncorrelated data
        var x = new List<decimal> { 1, 2, 3, 4, 5 };
        var y = new List<decimal> { 3, 1, 4, 2, 5 };

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().BeInRange(-0.5m, 0.5m); // Weak or no correlation
    }

    [Fact]
    public void CalculatePearsonCorrelation_EmptyData_ReturnsZero()
    {
        // Arrange
        var x = new List<decimal>();
        var y = new List<decimal>();

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculatePearsonCorrelation_MismatchedLengths_ReturnsZero()
    {
        // Arrange
        var x = new List<decimal> { 1, 2, 3 };
        var y = new List<decimal> { 1, 2 };

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculatePearsonCorrelation_ConstantData_ReturnsZero()
    {
        // Arrange - no variance
        var x = new List<decimal> { 5, 5, 5, 5, 5 };
        var y = new List<decimal> { 1, 2, 3, 4, 5 };

        // Act
        var result = _service.CalculatePearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0); // No variance in x means no correlation
    }

    [Fact]
    public void CalculateCorrelation_WithOverlappingDates_CalculatesMetrics()
    {
        // Arrange
        var asset1 = CreateTestAsset(
            "SPY",
            ("2024-01-01", 450m, 1000m, "POSITIVE_GAMMA"),
            ("2024-01-02", 455m, 1100m, "POSITIVE_GAMMA"),
            ("2024-01-03", 460m, 1200m, "POSITIVE_GAMMA"));

        var asset2 = CreateTestAsset(
            "QQQ",
            ("2024-01-01", 380m, 800m, "POSITIVE_GAMMA"),
            ("2024-01-02", 385m, 850m, "POSITIVE_GAMMA"),
            ("2024-01-03", 390m, 900m, "POSITIVE_GAMMA"));

        // Act
        var result = _service.CalculateCorrelation(asset1, asset2);

        // Assert
        result.Should().NotBeNull();
        result.Symbol1.Should().Be("SPY");
        result.Symbol2.Should().Be("QQQ");
        result.PriceCorrelation.Should().BeApproximately(1.0m, 0.01m); // Perfect correlation
        result.GexCorrelation.Should().BeApproximately(1.0m, 0.01m);
        result.RegimeAlignment.Should().Be(100); // All same regime
    }

    [Fact]
    public void GenerateSummary_WithMultipleAssets_CalculatesPairwiseCorrelations()
    {
        // Arrange
        var assets = new List<AssetComparisonData>
        {
            CreateTestAsset("SPY", ("2024-01-01", 450m, 1000m, "POSITIVE_GAMMA")),
            CreateTestAsset("QQQ", ("2024-01-01", 380m, 800m, "POSITIVE_GAMMA")),
            CreateTestAsset("IWM", ("2024-01-01", 200m, 500m, "NEGATIVE_GAMMA")),
        };

        // Act
        var result = _service.GenerateSummary(assets);

        // Assert
        result.Should().NotBeNull();
        result.Assets.Should().HaveCount(3);
        result.Correlations.Should().HaveCount(3); // C(3,2) = 3 pairs
        result.DivergenceEvents.Should().HaveCount(1); // IWM has different regime
    }

    [Fact]
    public void FindDivergenceEvents_WithDivergentRegimes_FindsEvents()
    {
        // Arrange
        var assets = new List<AssetComparisonData>
        {
            CreateTestAsset(
                "SPY",
                ("2024-01-01", 450m, 1000m, "POSITIVE_GAMMA"),
                ("2024-01-02", 455m, -500m, "NEGATIVE_GAMMA")),
            CreateTestAsset(
                "QQQ",
                ("2024-01-01", 380m, -800m, "NEGATIVE_GAMMA"),
                ("2024-01-02", 385m, 850m, "POSITIVE_GAMMA")),
        };

        // Act
        var result = _service.FindDivergenceEvents(assets);

        // Assert
        result.Should().HaveCount(2); // Both days have divergence
        result[0].AssetRegimes.Should().HaveCount(2);
        result[0].AssetRegimes["SPY"].Should().Be(GammaRegime.Positive);
        result[0].AssetRegimes["QQQ"].Should().Be(GammaRegime.Negative);
    }

    [Fact]
    public void FindDivergenceEvents_WithAlignedRegimes_FindsNoEvents()
    {
        // Arrange
        var assets = new List<AssetComparisonData>
        {
            CreateTestAsset("SPY", ("2024-01-01", 450m, 1000m, "POSITIVE_GAMMA")),
            CreateTestAsset("QQQ", ("2024-01-01", 380m, 800m, "POSITIVE_GAMMA")),
        };

        // Act
        var result = _service.FindDivergenceEvents(assets);

        // Assert
        result.Should().BeEmpty(); // No divergence
    }

    [Fact]
    public void FindDivergenceEvents_WithIndexAndStock_SetsDispersionFlag()
    {
        // Arrange
        var assets = new List<AssetComparisonData>
        {
            CreateTestAssetWithClass("SPY", "Index", ("2024-01-01", 450m, 1000m, "POSITIVE_GAMMA")),
            CreateTestAssetWithClass("AAPL", "Stock", ("2024-01-01", 180m, -500m, "NEGATIVE_GAMMA")),
        };

        // Act
        var result = _service.FindDivergenceEvents(assets);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsDispersionOpportunity.Should().BeTrue();
    }

    private static AssetComparisonData CreateTestAsset(string symbol, params (string date, decimal price, decimal gex, string regime)[] dataPoints)
    {
        return CreateTestAssetWithClass(symbol, "Stock", dataPoints);
    }

    private static AssetComparisonData CreateTestAssetWithClass(string symbol, string assetClass, params (string date, decimal price, decimal gex, string regime)[] dataPoints)
    {
        var timeline = dataPoints.Select(d => new GexDataPoint
        {
            Date = DateOnly.Parse(d.date, CultureInfo.InvariantCulture),
            Price = d.price,
            Gex = d.gex,
            CallGex = d.gex > 0 ? d.gex : 0,
            PutGex = d.gex < 0 ? Math.Abs(d.gex) : 0,
            ZeroGamma = d.price * 0.98m,
            MaxGamma = d.price * 1.02m,
            Regime = d.regime,
            CallOi = 10000,
            PutOi = 10000,
            Contracts = 20000,
            Quality = 0.95m,
        }).ToList();

        var regimeAnalysis = AnalyzeRegimes(timeline);

        return new AssetComparisonData
        {
            Symbol = symbol,
            Timeline = new GexTimeline
            {
                Symbol = symbol,
                AssetClass = assetClass,
                Count = timeline.Count,
                DateRange = new DateRange
                {
                    Start = DateOnly.Parse(dataPoints[0].date, CultureInfo.InvariantCulture),
                    End = DateOnly.Parse(dataPoints[^1].date, CultureInfo.InvariantCulture),
                },
                Timeline = timeline,
            },
            RegimeAnalysis = regimeAnalysis,
        };
    }

    private static RegimeAnalysisSummary AnalyzeRegimes(List<GexDataPoint> timeline)
    {
        var segments = new List<RegimeSegment>();
        var transitions = new List<RegimeTransition>();

        if (timeline.Count == 0)
        {
            return new RegimeAnalysisSummary
            {
                Symbol = "TEST",
                DateRange = "2024-01-01 → 2024-01-01",
                TotalDays = 0,
                Segments = segments,
                Transitions = transitions,
            };
        }

        var currentRegime = timeline[0].Regime;
        var segmentStart = timeline[0].Date;
        var segmentDays = 1;

        for (int i = 1; i < timeline.Count; i++)
        {
            if (timeline[i].Regime != currentRegime)
            {
                // End current segment
                segments.Add(new RegimeSegment
                {
                    Regime = currentRegime == "POSITIVE_GAMMA" ? GammaRegime.Positive : GammaRegime.Negative,
                    StartDate = segmentStart,
                    EndDate = timeline[i - 1].Date,
                    DurationDays = segmentDays,
                });

                // Add transition
                transitions.Add(new RegimeTransition
                {
                    Date = timeline[i].Date,
                    FromRegime = currentRegime == "POSITIVE_GAMMA" ? GammaRegime.Positive : GammaRegime.Negative,
                    ToRegime = timeline[i].Regime == "POSITIVE_GAMMA" ? GammaRegime.Positive : GammaRegime.Negative,
                    DaysInPreviousRegime = segmentDays,
                });

                // Start new segment
                currentRegime = timeline[i].Regime;
                segmentStart = timeline[i].Date;
                segmentDays = 1;
            }
            else
            {
                segmentDays++;
            }
        }

        // Add final segment
        segments.Add(new RegimeSegment
        {
            Regime = currentRegime == "POSITIVE_GAMMA" ? GammaRegime.Positive : GammaRegime.Negative,
            StartDate = segmentStart,
            EndDate = timeline[^1].Date,
            DurationDays = segmentDays,
        });

        return new RegimeAnalysisSummary
        {
            Symbol = "TEST",
            DateRange = $"{timeline[0].Date} → {timeline[^1].Date}",
            TotalDays = timeline.Count,
            Segments = segments,
            Transitions = transitions,
        };
    }
}
