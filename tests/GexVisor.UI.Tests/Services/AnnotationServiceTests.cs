// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for AnnotationService CRUD operations and validation statistics.
/// Tests localStorage persistence, export functionality, and validation tracking.
/// </summary>
public class AnnotationServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredAnnotations_LoadsFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var storedAnnotations = new List<PatternAnnotation>
        {
            CreateAnnotation("gamma_flip_pos", "2024-01-01"),
            CreateAnnotation("opex_pinning", "2024-01-02"),
        };
        mockStorage.Setup(x => x.GetAsync<List<PatternAnnotation>>(AppConstants.Storage.Annotations))
            .ReturnsAsync(storedAnnotations);
        var service = new AnnotationService(mockStorage.Object);

        // Act
        await service.LoadAsync();

        // Assert
        service.Annotations.Should().HaveCount(2);
        service.Annotations[0].PatternType.Should().Be("gamma_flip_pos");
    }

    [Fact]
    public async Task LoadAsync_WithNullStorage_InitializesEmptyList()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.GetAsync<List<PatternAnnotation>>(AppConstants.Storage.Annotations))
            .ReturnsAsync((List<PatternAnnotation>?)null);
        var service = new AnnotationService(mockStorage.Object);

        // Act
        await service.LoadAsync();

        // Assert
        service.Annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_FiresOnAnnotationsChanged()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        mockStorage.Setup(x => x.GetAsync<List<PatternAnnotation>>(AppConstants.Storage.Annotations))
            .ReturnsAsync(new List<PatternAnnotation>());
        var service = new AnnotationService(mockStorage.Object);
        var eventFired = false;
        service.OnAnnotationsChanged += () => eventFired = true;

        // Act
        await service.LoadAsync();

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_AddsAnnotationAndSorts()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();

        // Add annotations out of order
        await service.AddAsync(CreateAnnotation("opex_pinning", "2024-01-03"));
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01"));
        await service.AddAsync(CreateAnnotation("dealer_squeeze", "2024-01-02"));

        // Assert
        service.Annotations.Should().HaveCount(3);
        service.Annotations[0].StartDate.Should().Be("2024-01-01");
        service.Annotations[1].StartDate.Should().Be("2024-01-02");
        service.Annotations[2].StartDate.Should().Be("2024-01-03");
    }

    [Fact]
    public async Task AddAsync_SavesToStorage()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();

        // Act
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01");
        await service.AddAsync(annotation);

        // Assert
        mockStorage.Verify(
            x => x.SetAsync(AppConstants.Storage.Annotations, It.IsAny<List<PatternAnnotation>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingAnnotation()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01");
        await service.LoadAsync();
        await service.AddAsync(annotation);

        // Act
        var updated = annotation with { Notes = "Updated notes", Confidence = "high" };
        await service.UpdateAsync(updated);

        // Assert
        var result = service.GetById(annotation.Id);
        result.Should().NotBeNull();
        result!.Notes.Should().Be("Updated notes");
        result.Confidence.Should().Be("high");
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentId_DoesNothing()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01"));

        // Act
        var nonexistent = CreateAnnotation("opex_pinning", "2024-01-02") with { Id = Guid.NewGuid() };
        await service.UpdateAsync(nonexistent);

        // Assert
        service.Annotations.Should().HaveCount(1);
        mockStorage.Verify(
            x => x.SetAsync(AppConstants.Storage.Annotations, It.IsAny<List<PatternAnnotation>>()),
            Times.Once); // Only the AddAsync save, not UpdateAsync
    }

    [Fact]
    public async Task DeleteAsync_RemovesAnnotation()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01");
        await service.LoadAsync();
        await service.AddAsync(annotation);

        // Act
        await service.DeleteAsync(annotation.Id);

        // Assert
        service.Annotations.Should().BeEmpty();
        service.GetById(annotation.Id).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_SavesAfterDelete()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01");
        await service.LoadAsync();
        await service.AddAsync(annotation);

        // Act
        await service.DeleteAsync(annotation.Id);

        // Assert
        mockStorage.Verify(
            x => x.SetAsync(AppConstants.Storage.Annotations, It.IsAny<List<PatternAnnotation>>()),
            Times.Exactly(2)); // Once for Add, once for Delete
    }

    [Fact]
    public async Task GetById_ReturnsAnnotationIfExists()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01");
        await service.LoadAsync();
        await service.AddAsync(annotation);

        // Act
        var result = service.GetById(annotation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(annotation.Id);
    }

    [Fact]
    public void GetById_ReturnsNullIfNotExists()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);

        // Act
        var result = service.GetById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForDate_ReturnsSingleDateAnnotations()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01"));
        await service.AddAsync(CreateAnnotation("opex_pinning", "2024-01-02"));

        // Act
        var results = service.GetForDate("2024-01-01");

        // Assert
        results.Should().HaveCount(1);
        results.First().StartDate.Should().Be("2024-01-01");
    }

    [Fact]
    public async Task GetForDate_ReturnsRangeAnnotations()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();

        // Add range annotation (2024-01-01 to 2024-01-05) - IsRange is calculated from EndDate
        var rangeAnnotation = CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            EndDate = "2024-01-05",
        };
        await service.AddAsync(rangeAnnotation);

        // Assert IsRange is calculated correctly
        rangeAnnotation.IsRange.Should().BeTrue();

        // Act & Assert - Date within range
        service.GetForDate("2024-01-03").Should().HaveCount(1);

        // Act & Assert - Start date
        service.GetForDate("2024-01-01").Should().HaveCount(1);

        // Act & Assert - End date
        service.GetForDate("2024-01-05").Should().HaveCount(1);

        // Act & Assert - Outside range
        service.GetForDate("2024-01-06").Should().BeEmpty();
    }

    [Fact]
    public async Task ExportAsJson_ReturnsValidJson()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01"));

        // Act
        var json = service.ExportAsJson();

        // Assert
        json.Should().Contain("gamma_flip_pos");
        json.Should().Contain("2024-01-01");
        json.Should().Contain("startDate"); // camelCase
    }

    [Fact]
    public async Task ExportAsCsv_ReturnsValidCsv()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            Notes = "Test note",
        };
        await service.AddAsync(annotation);

        // Act
        var csv = service.ExportAsCsv();

        // Assert
        csv.Should().Contain("id,start_date,end_date,pattern_type");
        csv.Should().Contain("gamma_flip_pos");
        csv.Should().Contain("2024-01-01");
    }

    [Fact]
    public async Task ExportAsCsv_EscapesSpecialCharacters()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        var annotation = CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            Notes = "Has, comma and \"quote\"",
        };
        await service.AddAsync(annotation);

        // Act
        var csv = service.ExportAsCsv();

        // Assert
        csv.Should().Contain("\"Has, comma and \"\"quote\"\"\"");
    }

    [Fact]
    public async Task ExportObfuscated_NormalizesValues()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01"));

        // Act
        var json = service.ExportObfuscated();

        // Assert
        json.Should().Contain("dayOffset");
        json.Should().Contain("durationDays");
        json.Should().Contain("priceNormalized");
        json.Should().NotContain("2024-01-01"); // Dates should be obfuscated
    }

    [Fact]
    public async Task GetValidationStats_CalculatesPerPatternStats()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();

        // Add annotations with outcomes
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            Outcome = AnnotationOutcomes.Confirmed,
            Taxonomy = "MECH",
        });
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-02") with
        {
            Outcome = AnnotationOutcomes.Invalidated,
            Taxonomy = "MECH",
        });
        await service.AddAsync(CreateAnnotation("opex_pinning", "2024-01-03") with
        {
            Outcome = AnnotationOutcomes.Confirmed,
            Taxonomy = "PROB",
        });

        // Act
        var stats = service.GetValidationStats();

        // Assert
        stats.Should().NotBeEmpty();
        var gammaFlipStats = stats.FirstOrDefault(s => s.PatternType == "gamma_flip_pos");
        gammaFlipStats.Should().NotBeNull();
        gammaFlipStats!.TotalAnnotations.Should().Be(2);
        gammaFlipStats.ConfirmedCount.Should().Be(1);
        gammaFlipStats.InvalidatedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatsForPattern_ReturnsCorrectPatternStats()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();
        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            Outcome = AnnotationOutcomes.Confirmed,
        });

        // Act
        var stats = service.GetStatsForPattern("gamma_flip_pos");

        // Assert
        stats.Should().NotBeNull();
        stats!.PatternType.Should().Be("gamma_flip_pos");
        stats.TotalAnnotations.Should().Be(1);
    }

    [Fact]
    public async Task GetValidationSummary_CalculatesOverallMetrics()
    {
        // Arrange
        var mockStorage = new Mock<ILocalStorageService>();
        var service = new AnnotationService(mockStorage.Object);
        await service.LoadAsync();

        await service.AddAsync(CreateAnnotation("gamma_flip_pos", "2024-01-01") with
        {
            Outcome = AnnotationOutcomes.Confirmed,
        });
        await service.AddAsync(CreateAnnotation("opex_pinning", "2024-01-02") with
        {
            Outcome = AnnotationOutcomes.Confirmed,
        });
        await service.AddAsync(CreateAnnotation("dealer_squeeze", "2024-01-03") with
        {
            Outcome = AnnotationOutcomes.Invalidated,
        });
        await service.AddAsync(CreateAnnotation("negative_gamma_regime", "2024-01-04") with
        {
            Outcome = AnnotationOutcomes.Pending,
        });

        // Act
        var summary = service.GetValidationSummary();

        // Assert
        summary.TotalAnnotations.Should().Be(4);
        summary.ConfirmedCount.Should().Be(2);
        summary.InvalidatedCount.Should().Be(1);
        summary.PendingCount.Should().Be(1);
        summary.OverallWinRate.Should().Be(66.7); // 2/(2+1) * 100 = 66.7%
    }

    private static PatternAnnotation CreateAnnotation(string patternType, string startDate)
    {
        return new PatternAnnotation
        {
            Id = Guid.NewGuid(),
            StartDate = startDate,
            PatternType = patternType,
            Taxonomy = "MECH",
            Confidence = "medium",
            PriceAtAnnotation = 450m,
            GexAtAnnotation = 1000m,
            IsNegativeGamma = false,
        };
    }
}
