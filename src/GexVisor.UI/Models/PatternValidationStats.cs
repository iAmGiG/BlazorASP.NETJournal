using GexVisor.UI.Configuration;

namespace GexVisor.UI.Models;

/// <summary>
/// Statistics for pattern validation progress tracking.
/// Supports the research methodology from gex-llm-patterns validation.
/// </summary>
public record PatternValidationStats
{
    public required string PatternType { get; init; }
    public required string PatternLabel { get; init; }

    // Counts by outcome
    public int TotalAnnotations { get; init; }
    public int ConfirmedCount { get; init; }
    public int InvalidatedCount { get; init; }
    public int PendingCount { get; init; }

    // Calculated metrics
    public int CompletedCount => ConfirmedCount + InvalidatedCount;

    /// <summary>
    /// Win rate as percentage (confirmed / completed).
    /// Returns null if no completed annotations.
    /// </summary>
    public double? WinRate => CompletedCount > 0
        ? Math.Round((double)ConfirmedCount / CompletedCount * 100, 1)
        : null;

    /// <summary>
    /// Progress toward minimum sample size for statistical significance.
    /// </summary>
    public double SampleProgress => Math.Min(100, (double)CompletedCount / AppConstants.Validation.MinimumSampleSize * 100);

    /// <summary>
    /// Validation status based on research criteria.
    /// Requires: minimum sample size AND minimum win rate threshold.
    /// </summary>
    public ValidationStatus Status
    {
        get
        {
            if (CompletedCount < AppConstants.Validation.MinimumPartialSamples)
                return ValidationStatus.Unvalidated;

            if (CompletedCount < AppConstants.Validation.MinimumSampleSize)
                return ValidationStatus.Partial;

            // Have sufficient sample - check win rate
            return WinRate >= AppConstants.Validation.MinimumWinRatePercent
                ? ValidationStatus.Validated
                : ValidationStatus.Failed;
        }
    }

    /// <summary>
    /// Number of additional samples needed to reach minimum sample size.
    /// </summary>
    public int SamplesNeeded => Math.Max(0, AppConstants.Validation.MinimumSampleSize - CompletedCount);

    /// <summary>
    /// Breakdown by taxonomy (MECH/PROB/NARR).
    /// </summary>
    public Dictionary<string, int> TaxonomyBreakdown { get; init; } = [];

    /// <summary>
    /// Breakdown by confidence level.
    /// </summary>
    public Dictionary<string, int> ConfidenceBreakdown { get; init; } = [];
}

/// <summary>
/// Validation status categories based on research methodology.
/// </summary>
public enum ValidationStatus
{
    /// <summary>Fewer than 10 samples - insufficient data.</summary>
    Unvalidated,

    /// <summary>10-29 samples - in progress toward validation.</summary>
    Partial,

    /// <summary>n>=30 AND win rate >=60% - pattern validated.</summary>
    Validated,

    /// <summary>n>=30 BUT win rate &lt;60% - pattern failed validation.</summary>
    Failed
}

/// <summary>
/// Extension methods for ValidationStatus display.
/// </summary>
public static class ValidationStatusExtensions
{
    public static string ToLabel(this ValidationStatus status) => status switch
    {
        ValidationStatus.Unvalidated => "Unvalidated",
        ValidationStatus.Partial => "In Progress",
        ValidationStatus.Validated => "Validated",
        ValidationStatus.Failed => "Failed",
        _ => "Unknown"
    };

    public static string ToCssClass(this ValidationStatus status) => status switch
    {
        ValidationStatus.Unvalidated => "status-unvalidated",
        ValidationStatus.Partial => "status-partial",
        ValidationStatus.Validated => "status-validated",
        ValidationStatus.Failed => "status-failed",
        _ => ""
    };
}

/// <summary>
/// Summary statistics across all pattern validation.
/// </summary>
public record ValidationSummary
{
    public int TotalAnnotations { get; init; }
    public int ConfirmedCount { get; init; }
    public int InvalidatedCount { get; init; }
    public int PendingCount { get; init; }
    public int CompletedCount => ConfirmedCount + InvalidatedCount;

    public int ValidatedPatterns { get; init; }
    public int PartialPatterns { get; init; }
    public int UnvalidatedPatterns { get; init; }
    public int FailedPatterns { get; init; }

    public double? OverallWinRate { get; init; }
}
