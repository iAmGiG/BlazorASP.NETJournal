using GexVisor.UI.Models;
using System.Text.Json;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing pattern annotations with localStorage persistence.
/// Provides CRUD operations and export functionality.
/// </summary>
public class AnnotationService
{
    private readonly ILocalStorageService _storage;
    private List<PatternAnnotation> _annotations = [];

    public event Action? OnAnnotationsChanged;

    public IReadOnlyList<PatternAnnotation> Annotations => _annotations;

    public AnnotationService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Load annotations from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<List<PatternAnnotation>>(StorageKeys.Annotations);
        _annotations = stored ?? [];
        OnAnnotationsChanged?.Invoke();
    }

    /// <summary>
    /// Save annotations to localStorage.
    /// </summary>
    private async Task SaveAsync()
    {
        await _storage.SetAsync(StorageKeys.Annotations, _annotations);
        OnAnnotationsChanged?.Invoke();
    }

    /// <summary>
    /// Add a new annotation.
    /// </summary>
    public async Task AddAsync(PatternAnnotation annotation)
    {
        _annotations.Add(annotation);
        _annotations = [.. _annotations.OrderBy(a => a.StartDate)];
        await SaveAsync();
    }

    /// <summary>
    /// Update an existing annotation.
    /// </summary>
    public async Task UpdateAsync(PatternAnnotation updated)
    {
        var index = _annotations.FindIndex(a => a.Id == updated.Id);
        if (index >= 0)
        {
            _annotations[index] = updated;
            _annotations = [.. _annotations.OrderBy(a => a.StartDate)];
            await SaveAsync();
        }
    }

    /// <summary>
    /// Delete an annotation by ID.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        _annotations.RemoveAll(a => a.Id == id);
        await SaveAsync();
    }

    /// <summary>
    /// Get an annotation by ID.
    /// </summary>
    public PatternAnnotation? GetById(Guid id) => _annotations.FirstOrDefault(a => a.Id == id);

    /// <summary>
    /// Get annotations for a specific date.
    /// </summary>
    public IEnumerable<PatternAnnotation> GetForDate(string date)
    {
        return _annotations.Where(a =>
            a.StartDate == date ||
            (a.IsRange && string.Compare(a.StartDate, date) <= 0 &&
             string.Compare(a.EndDate!, date) >= 0));
    }

    /// <summary>
    /// Export annotations as JSON for LLM training.
    /// </summary>
    public string ExportAsJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(_annotations, options);
    }

    /// <summary>
    /// Export annotations as CSV for analysis.
    /// </summary>
    public string ExportAsCsv()
    {
        var lines = new List<string>
        {
            "id,start_date,end_date,pattern_type,taxonomy,confidence,notes,price,gex,is_negative_gamma,outcome,price_move"
        };

        foreach (var a in _annotations)
        {
            var fields = new[]
            {
                a.Id.ToString(),
                a.StartDate,
                a.EndDate ?? "",
                a.PatternType,
                a.Taxonomy,
                a.Confidence,
                EscapeCsv(a.Notes ?? ""),
                a.PriceAtAnnotation.ToString("F2"),
                a.GexAtAnnotation.ToString("F4"),
                a.IsNegativeGamma.ToString().ToLower(),
                a.Outcome ?? "",
                a.PriceMove?.ToString("F2") ?? ""
            };
            lines.Add(string.Join(",", fields));
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Export annotations in obfuscated format (strips dates, uses T+0, T+1, etc.)
    /// Prevents LLM memorization of specific dates.
    /// </summary>
    public string ExportObfuscated()
    {
        var obfuscated = new List<object>();

        foreach (var a in _annotations)
        {
            obfuscated.Add(new
            {
                id = a.Id,
                dayOffset = 0, // T+0 for start
                durationDays = a.IsRange ? GetDaysBetween(a.StartDate, a.EndDate!) : 1,
                patternType = a.PatternType,
                taxonomy = a.Taxonomy,
                confidence = a.Confidence,
                notes = a.Notes,
                priceNormalized = 100m, // Normalize to 100
                gexNormalized = a.GexAtAnnotation / Math.Max(1, Math.Abs(a.GexAtAnnotation)),
                isNegativeGamma = a.IsNegativeGamma,
                outcome = a.Outcome,
                priceMovePercent = a.PriceMove.HasValue && a.PriceAtAnnotation > 0
                    ? a.PriceMove.Value / a.PriceAtAnnotation * 100
                    : (decimal?)null
            });
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(obfuscated, options);
    }

    private static int GetDaysBetween(string startDate, string endDate)
    {
        if (DateOnly.TryParse(startDate, out var start) && DateOnly.TryParse(endDate, out var end))
        {
            return end.DayNumber - start.DayNumber + 1;
        }
        return 1;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    /// <summary>
    /// Get validation statistics for all pattern types.
    /// </summary>
    public IReadOnlyList<PatternValidationStats> GetValidationStats()
    {
        var stats = new List<PatternValidationStats>();

        foreach (var (code, label) in PatternTypes.All)
        {
            var patternAnnotations = _annotations.Where(a => a.PatternType == code).ToList();

            if (patternAnnotations.Count == 0 && code == PatternTypes.Other)
                continue;

            var taxonomyBreakdown = patternAnnotations
                .GroupBy(a => a.Taxonomy)
                .ToDictionary(g => g.Key, g => g.Count());

            var confidenceBreakdown = patternAnnotations
                .GroupBy(a => a.Confidence)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.Add(new PatternValidationStats
            {
                PatternType = code,
                PatternLabel = label,
                TotalAnnotations = patternAnnotations.Count,
                ConfirmedCount = patternAnnotations.Count(a => a.Outcome == AnnotationOutcomes.Confirmed),
                InvalidatedCount = patternAnnotations.Count(a => a.Outcome == AnnotationOutcomes.Invalidated),
                PendingCount = patternAnnotations.Count(a => a.Outcome == AnnotationOutcomes.Pending || a.Outcome == null),
                TaxonomyBreakdown = taxonomyBreakdown,
                ConfidenceBreakdown = confidenceBreakdown
            });
        }

        return stats;
    }

    /// <summary>
    /// Get validation statistics for a specific pattern type.
    /// </summary>
    public PatternValidationStats? GetStatsForPattern(string patternType)
    {
        return GetValidationStats().FirstOrDefault(s => s.PatternType == patternType);
    }

    /// <summary>
    /// Get overall validation summary across all patterns.
    /// </summary>
    public ValidationSummary GetValidationSummary()
    {
        var stats = GetValidationStats();
        var total = _annotations.Count;
        var confirmed = _annotations.Count(a => a.Outcome == AnnotationOutcomes.Confirmed);
        var invalidated = _annotations.Count(a => a.Outcome == AnnotationOutcomes.Invalidated);
        var pending = _annotations.Count(a => a.Outcome == AnnotationOutcomes.Pending || a.Outcome == null);

        return new ValidationSummary
        {
            TotalAnnotations = total,
            ConfirmedCount = confirmed,
            InvalidatedCount = invalidated,
            PendingCount = pending,
            ValidatedPatterns = stats.Count(s => s.Status == ValidationStatus.Validated),
            PartialPatterns = stats.Count(s => s.Status == ValidationStatus.Partial),
            UnvalidatedPatterns = stats.Count(s => s.Status == ValidationStatus.Unvalidated),
            FailedPatterns = stats.Count(s => s.Status == ValidationStatus.Failed),
            OverallWinRate = confirmed + invalidated > 0
                ? Math.Round((double)confirmed / (confirmed + invalidated) * 100, 1)
                : null
        };
    }
}
