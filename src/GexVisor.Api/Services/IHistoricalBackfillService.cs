// Copyright (c) GexVisor. All rights reserved.

namespace GexVisor.Api.Services;

/// <summary>
/// Service for backfilling historical GEX calculations from options chain data.
/// Handles rate limiting, progress tracking, and resume capability.
/// </summary>
public interface IHistoricalBackfillService
{
    /// <summary>
    /// Start a backfill job for the specified symbols and date range.
    /// </summary>
    /// <param name="symbols">Symbols to backfill (e.g., SPY, QQQ, IWM)</param>
    /// <param name="startDate">Start date for backfill</param>
    /// <param name="endDate">End date for backfill (defaults to today)</param>
    /// <returns>Job ID for tracking progress</returns>
    Task<BackfillJobResult> StartBackfillAsync(
        IEnumerable<string> symbols,
        DateOnly startDate,
        DateOnly? endDate = null);

    /// <summary>
    /// Get the current status of a backfill job.
    /// </summary>
    /// <param name="jobId">Job ID from StartBackfillAsync</param>
    /// <returns>Current job status and progress</returns>
    BackfillStatus? GetStatus(string jobId);

    /// <summary>
    /// Get status of all active and recent backfill jobs.
    /// </summary>
    IReadOnlyList<BackfillStatus> GetAllStatuses();

    /// <summary>
    /// Stop a running backfill job.
    /// </summary>
    /// <param name="jobId">Job ID to stop</param>
    /// <returns>True if job was stopped, false if not found or already complete</returns>
    bool StopBackfill(string jobId);

    /// <summary>
    /// Resume a previously stopped or failed backfill job.
    /// </summary>
    /// <param name="jobId">Job ID to resume</param>
    /// <returns>True if job was resumed, false if not resumable</returns>
    Task<bool> ResumeBackfillAsync(string jobId);
}

/// <summary>
/// Result of starting a backfill job.
/// </summary>
public record BackfillJobResult
{
    public required string JobId { get; init; }
    public required bool Started { get; init; }
    public string? Error { get; init; }

    public static BackfillJobResult Success(string jobId) => new()
    {
        JobId = jobId,
        Started = true
    };

    public static BackfillJobResult Fail(string error) => new()
    {
        JobId = string.Empty,
        Started = false,
        Error = error
    };
}

/// <summary>
/// Status of a backfill job.
/// </summary>
public record BackfillStatus
{
    public required string JobId { get; init; }
    public required BackfillState State { get; init; }
    public required IReadOnlyList<string> Symbols { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    // Progress tracking
    public int TotalDays { get; init; }
    public int CompletedDays { get; init; }
    public int FailedDays { get; init; }
    public double ProgressPercent => TotalDays > 0 ? (double)CompletedDays / TotalDays * 100 : 0;

    // Timing
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime LastActivityAt { get; init; }

    // Rate limiting info
    public int ApiCallsMade { get; init; }
    public int ApiCallsRemaining { get; init; }

    // Current processing
    public string? CurrentSymbol { get; init; }
    public DateOnly? CurrentDate { get; init; }
    public string? LastError { get; init; }

    // Results
    public int GexRecordsStored { get; init; }
}

/// <summary>
/// State of a backfill job.
/// </summary>
public enum BackfillState
{
    /// <summary>Job is queued but not yet started.</summary>
    Pending,

    /// <summary>Job is actively running.</summary>
    Running,

    /// <summary>Job is paused due to rate limiting.</summary>
    RateLimited,

    /// <summary>Job was stopped by user.</summary>
    Stopped,

    /// <summary>Job completed successfully.</summary>
    Completed,

    /// <summary>Job failed with errors.</summary>
    Failed
}
