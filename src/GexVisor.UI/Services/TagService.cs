namespace GexVisor.UI.Services;

/// <summary>
/// Shared service for managing tags across all journal features.
/// Provides common tags, autocomplete suggestions, and tag persistence.
/// </summary>
public class TagService
{
    private readonly ILocalStorageService _storage;
    private HashSet<string> _customTags = [];
    private List<string>? _allTagsCache;

    /// <summary>
    /// Predefined pattern tags from GEX research.
    /// </summary>
    public static readonly IReadOnlyList<string> PatternTags =
    [
        "gamma-flip",
        "negative-gamma",
        "positive-gamma",
        "opex-pinning",
        "dealer-squeeze",
        "vol-expansion",
        "vol-compression",
        "support-test",
        "resistance-test",
        "trend-continuation"
    ];

    /// <summary>
    /// Predefined category tags.
    /// </summary>
    public static readonly IReadOnlyList<string> CategoryTags =
    [
        "hypothesis",
        "observation",
        "validated",
        "invalidated",
        "needs-review",
        "important"
    ];

    /// <summary>
    /// All available tags (predefined + custom).
    /// Cached to avoid repeated concatenation, distinct, and sorting operations.
    /// </summary>
    public IEnumerable<string> AllTags
    {
        get
        {
            if (_allTagsCache == null)
            {
                _allTagsCache = PatternTags
                    .Concat(CategoryTags)
                    .Concat(_customTags)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t)
                    .ToList();
            }
            return _allTagsCache;
        }
    }

    public TagService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Load custom tags from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<List<string>>(StorageKeys.CustomTags);
        _customTags = stored?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        _allTagsCache = null; // Invalidate cache when custom tags change
    }

    /// <summary>
    /// Add a custom tag.
    /// </summary>
    public async Task AddCustomTagAsync(string tag)
    {
        var normalized = NormalizeTag(tag);
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        if (_customTags.Add(normalized))
        {
            _allTagsCache = null; // Invalidate cache when tags change
            await SaveAsync();
        }
    }

    /// <summary>
    /// Remove a custom tag (predefined tags cannot be removed).
    /// </summary>
    public async Task RemoveCustomTagAsync(string tag)
    {
        if (_customTags.Remove(tag))
        {
            _allTagsCache = null; // Invalidate cache when tags change
            await SaveAsync();
        }
    }

    /// <summary>
    /// Get tag suggestions based on partial input.
    /// </summary>
    public IEnumerable<string> GetSuggestions(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial))
        {
            return AllTags.Take(10);
        }

        return AllTags
            .Where(t => t.Contains(partial, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => !t.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .ThenBy(t => t)
            .Take(10);
    }

    /// <summary>
    /// Check if a tag is predefined (vs custom).
    /// </summary>
    public bool IsPredefinedTag(string tag)
    {
        return PatternTags.Contains(tag, StringComparer.OrdinalIgnoreCase) ||
               CategoryTags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalize a tag (lowercase, trim, replace spaces with hyphens).
    /// </summary>
    public static string NormalizeTag(string tag)
    {
        return tag.Trim().ToLowerInvariant().Replace(' ', '-');
    }

    private async Task SaveAsync()
    {
        await _storage.SetAsync(StorageKeys.CustomTags, _customTags.ToList());
    }
}
