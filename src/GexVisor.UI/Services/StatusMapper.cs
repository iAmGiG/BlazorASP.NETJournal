using System.Text.Json.Serialization;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for normalizing GitHub project status names into standard columns.
/// Enables unified view across multiple projects with different status naming conventions.
/// </summary>
public class StatusMapper
{
    private readonly LocalStorageService _storage;

    private const string StorageKey = "gexvisor.statusMappings";

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
    /// Custom per-project mappings (projectId -> rawStatus -> normalizedStatus).
    /// </summary>
    private Dictionary<string, Dictionary<string, string>> _customMappings = new();

    public StatusMapper(LocalStorageService storage)
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
            return NormalizedStatus.Backlog;

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
    /// </summary>
    public string InferStatus(string rawStatus)
    {
        var lower = rawStatus.ToLowerInvariant().Trim();

        foreach (var (normalizedStatus, patterns) in DefaultInferenceRules)
        {
            foreach (var pattern in patterns)
            {
                // Exact match or contains
                if (lower == pattern || lower.Contains(pattern))
                {
                    return normalizedStatus;
                }
            }
        }

        // Default to Backlog for unknown statuses
        return NormalizedStatus.Backlog;
    }

    /// <summary>
    /// Get the inferred status and confidence level.
    /// </summary>
    public (string NormalizedStatus, bool IsExactMatch, bool IsCustom) MapStatusWithConfidence(string? projectId, string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return (NormalizedStatus.Backlog, false, false);

        // Check custom mapping first
        if (!string.IsNullOrEmpty(projectId)
            && _customMappings.TryGetValue(projectId, out var projectMappings)
            && projectMappings.TryGetValue(rawStatus, out var customMapping))
        {
            return (customMapping, true, true);
        }

        // Check inference rules
        var lower = rawStatus.ToLowerInvariant().Trim();

        foreach (var (normalizedStatus, patterns) in DefaultInferenceRules)
        {
            foreach (var pattern in patterns)
            {
                if (lower == pattern)
                {
                    return (normalizedStatus, true, false);
                }
                if (lower.Contains(pattern))
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
