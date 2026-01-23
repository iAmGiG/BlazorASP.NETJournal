using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for normalizing GitHub project status names into standard columns.
/// Enables unified view across multiple projects with different status naming conventions.
/// </summary>
public class StatusMapper
{
    private readonly ILocalStorageService _storage;

    private const string StorageKey = "gexvisor.statusMappings";

    // Compiled regex for performance - used in hot path (status inference)
    private static readonly Regex HyphenUnderscoreNormalizer = new(@"[-_]+", RegexOptions.Compiled);

    /// <summary>
    /// Standard normalized status columns.
    /// </summary>
    public static class NormalizedStatus
    {
        public const string Backlog = "Backlog";
        public const string InProgress = "In Progress";
        public const string Done = "Done";

        public static readonly string[] All = [Backlog, InProgress, Done];
    }

    /// <summary>
    /// Default inference rules for common status naming patterns.
    /// Keys are normalized status names, values are patterns to match (lowercase).
    /// </summary>
    private static readonly Dictionary<string, string[]> DefaultInferenceRules = new()
    {
        [NormalizedStatus.Backlog] = [
            "todo", "to do", "to-do",
            "backlog", "icebox", "inbox",
            "open", "new", "pending",
            "not started", "waiting", "queued",
            "triage", "needs triage"
        ],
        [NormalizedStatus.InProgress] = [
            "in progress", "in-progress", "inprogress",
            "doing", "active", "working",
            "in review", "in-review", "review",
            "in development", "developing",
            "started", "wip", "blocked"
        ],
        [NormalizedStatus.Done] = [
            "done", "completed", "complete",
            "closed", "resolved", "fixed",
            "shipped", "released", "deployed",
            "finished", "merged", "won't fix", "wontfix"
        ]
    };

    /// <summary>
    /// Pre-normalized patterns with hyphens/underscores replaced with spaces.
    /// Avoids repeated regex operations in hot path.
    /// </summary>
    private static readonly Dictionary<string, (string Pattern, string NormalizedPattern)[]> NormalizedDefaultPatterns;

    static StatusMapper()
    {
        // Pre-normalize all patterns to avoid repeated regex operations
        NormalizedDefaultPatterns = new Dictionary<string, (string, string)[]>();

        foreach (var (status, patterns) in DefaultInferenceRules)
        {
            NormalizedDefaultPatterns[status] = patterns
                .Select(p => (p, HyphenUnderscoreNormalizer.Replace(p, " ")))
                .ToArray();
        }
    }

    /// <summary>
    /// Custom per-project mappings (projectId -> rawStatus -> normalizedStatus).
    /// </summary>
    private Dictionary<string, Dictionary<string, string>> _customMappings = new();

    public StatusMapper(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Load custom mappings from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<StatusMappingsStore>(StorageKey);
        if (stored?.ProjectMappings != null)
        {
            _customMappings = stored.ProjectMappings;
        }
    }

    /// <summary>
    /// Map a raw status to a normalized status.
    /// Uses custom mapping first, then falls back to inference rules.
    /// </summary>
    /// <param name="projectId">The GitHub project ID</param>
    /// <param name="rawStatus">The raw status string from GitHub</param>
    /// <returns>Normalized status (Backlog, In Progress, or Done)</returns>
    public string MapStatus(string? projectId, string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return NormalizedStatus.Backlog;
        }

        // Check custom mapping first
        if (!string.IsNullOrEmpty(projectId)
            && _customMappings.TryGetValue(projectId, out var projectMappings)
            && projectMappings.TryGetValue(rawStatus, out var customMapping))
        {
            return customMapping;
        }

        // Fall back to inference rules
        return InferStatus(rawStatus);
    }

    /// <summary>
    /// Infer normalized status from raw status using default rules.
    /// Uses word boundary matching to avoid false positives (e.g., "not started" matching "started").
    /// </summary>
    public string InferStatus(string rawStatus)
    {
        var lower = rawStatus.ToLowerInvariant().Trim();
        // Normalize once before loop to avoid repeated regex operations
        var normalizedText = HyphenUnderscoreNormalizer.Replace(lower, " ");

        foreach (var (normalizedStatus, patterns) in NormalizedDefaultPatterns)
        {
            foreach (var (pattern, normalizedPattern) in patterns)
            {
                // Exact match has highest priority
                if (lower == pattern)
                {
                    return normalizedStatus;
                }

                // Word boundary match: pattern must be complete word or phrase
                if (MatchesWithWordBoundary(normalizedText, normalizedPattern))
                {
                    return normalizedStatus;
                }
            }
        }

        // Default to Backlog for unknown statuses
        return NormalizedStatus.Backlog;
    }

    /// <summary>
    /// Check if pattern matches as a complete word/phrase, not as substring.
    /// Examples:
    /// - "not started" contains "started" but MatchesWithWordBoundary returns false
    /// - "in progress" contains "progress" but MatchesWithWordBoundary returns false
    /// - "in-progress" matches "in progress" by normalizing hyphens/spaces
    /// Optimized to use pre-normalized patterns to avoid allocations in hot path.
    /// Expects both normalizedText and normalizedPattern to have hyphens/underscores replaced with spaces.
    /// </summary>
    private bool MatchesWithWordBoundary(string normalizedText, string normalizedPattern)
    {
        // Simple word boundary check: pattern must be surrounded by word boundaries
        // More efficient than Regex for most cases
        var patternIndex = normalizedText.IndexOf(normalizedPattern, StringComparison.Ordinal);
        if (patternIndex == -1)
        {
            return false;
        }

        // Check start boundary (pattern is at start OR preceded by space)
        var isStartBoundary = patternIndex == 0 || char.IsWhiteSpace(normalizedText[patternIndex - 1]);

        // Check end boundary (pattern is at end OR followed by space)
        var patternEnd = patternIndex + normalizedPattern.Length;
        var isEndBoundary = patternEnd == normalizedText.Length || char.IsWhiteSpace(normalizedText[patternEnd]);

        return isStartBoundary && isEndBoundary;
    }

    /// <summary>
    /// Get the inferred status and confidence level.
    /// Returns confidence indicators: exact match (high) vs. word boundary match (medium).
    /// </summary>
    public (string NormalizedStatus, bool IsExactMatch, bool IsCustom) MapStatusWithConfidence(string? projectId, string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return (NormalizedStatus.Backlog, false, false);
        }

        // Check custom mapping first
        if (!string.IsNullOrEmpty(projectId)
            && _customMappings.TryGetValue(projectId, out var projectMappings)
            && projectMappings.TryGetValue(rawStatus, out var customMapping))
        {
            return (customMapping, true, true);
        }

        // Check inference rules
        var lower = rawStatus.ToLowerInvariant().Trim();
        // Normalize once before loop to avoid repeated regex operations
        var normalizedText = HyphenUnderscoreNormalizer.Replace(lower, " ");

        foreach (var (normalizedStatus, patterns) in NormalizedDefaultPatterns)
        {
            foreach (var (pattern, normalizedPattern) in patterns)
            {
                if (lower == pattern)
                {
                    return (normalizedStatus, true, false);
                }
                if (MatchesWithWordBoundary(normalizedText, normalizedPattern))
                {
                    return (normalizedStatus, false, false);
                }
            }
        }

        return (NormalizedStatus.Backlog, false, false);
    }

    /// <summary>
    /// Set a custom mapping for a specific project and status.
    /// </summary>
    public async Task SetCustomMappingAsync(string projectId, string rawStatus, string normalizedStatus)
    {
        if (!_customMappings.ContainsKey(projectId))
        {
            _customMappings[projectId] = new Dictionary<string, string>();
        }

        _customMappings[projectId][rawStatus] = normalizedStatus;
        await SaveAsync();
    }

    /// <summary>
    /// Remove a custom mapping.
    /// </summary>
    public async Task RemoveCustomMappingAsync(string projectId, string rawStatus)
    {
        if (_customMappings.TryGetValue(projectId, out var projectMappings))
        {
            projectMappings.Remove(rawStatus);
            if (projectMappings.Count == 0)
            {
                _customMappings.Remove(projectId);
            }
            await SaveAsync();
        }
    }

    /// <summary>
    /// Get all custom mappings for a project.
    /// </summary>
    public Dictionary<string, string> GetCustomMappings(string projectId)
    {
        return _customMappings.TryGetValue(projectId, out var mappings)
            ? new Dictionary<string, string>(mappings)
            : new();
    }

    /// <summary>
    /// Check if a raw status has a custom mapping.
    /// </summary>
    public bool HasCustomMapping(string projectId, string rawStatus)
    {
        return _customMappings.TryGetValue(projectId, out var mappings)
            && mappings.ContainsKey(rawStatus);
    }

    /// <summary>
    /// Get suggested mappings for a list of raw statuses.
    /// Useful for showing users what the inference engine will do.
    /// </summary>
    public List<StatusMappingSuggestion> GetSuggestedMappings(string projectId, IEnumerable<string> rawStatuses)
    {
        var suggestions = new List<StatusMappingSuggestion>();

        foreach (var rawStatus in rawStatuses.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var (normalized, isExact, isCustom) = MapStatusWithConfidence(projectId, rawStatus);

            suggestions.Add(new StatusMappingSuggestion
            {
                RawStatus = rawStatus,
                SuggestedMapping = normalized,
                IsExactMatch = isExact,
                IsCustomMapping = isCustom,
                CanOverride = !isCustom
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Clear all custom mappings for a project.
    /// </summary>
    public async Task ClearProjectMappingsAsync(string projectId)
    {
        _customMappings.Remove(projectId);
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        var store = new StatusMappingsStore
        {
            ProjectMappings = _customMappings
        };
        await _storage.SetAsync(StorageKey, store);
    }
}

/// <summary>
/// Storage model for status mappings.
/// </summary>
internal class StatusMappingsStore
{
    [JsonPropertyName("projectMappings")]
    public Dictionary<string, Dictionary<string, string>> ProjectMappings { get; set; } = new();
}

/// <summary>
/// Suggestion for status mapping with confidence info.
/// </summary>
public class StatusMappingSuggestion
{
    public string RawStatus { get; set; } = "";
    public string SuggestedMapping { get; set; } = "";
    public bool IsExactMatch { get; set; }
    public bool IsCustomMapping { get; set; }
    public bool CanOverride { get; set; }
}
