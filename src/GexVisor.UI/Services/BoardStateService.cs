using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing GitHub Kanban board state with caching.
/// </summary>
public class BoardStateService
{
    private readonly GitHubProjectService _projectService;
    private readonly ILocalStorageService _storage;

    public event Action? OnBoardStateChanged;

    private Dictionary<string, List<GitHubProjectItem>> _itemsCache = new();
    private Dictionary<string, DateTime> _cacheTimestamps = new();
    private bool _isLoading;
    private string? _lastError;

    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public bool IsLoading => _isLoading;
    public string? LastError => _lastError;

    public BoardStateService(GitHubProjectService projectService, LocalStorageService storage)
    {
        _projectService = projectService;
        _storage = storage;
    }

    /// <summary>
    /// Get items for the selected project, using cache if available.
    /// </summary>
    public async Task<List<GitHubProjectItem>> GetItemsAsync(bool forceRefresh = false)
    {
        var project = _projectService.SelectedProject;
        if (project == null)
            return new();

        var projectId = project.Id;

        // Check cache validity
        if (!forceRefresh && _itemsCache.TryGetValue(projectId, out var cached))
        {
            if (_cacheTimestamps.TryGetValue(projectId, out var timestamp)
                && DateTime.UtcNow - timestamp < CacheExpiry)
            {
                return cached;
            }
        }

        // Fetch fresh data
        return await RefreshItemsAsync();
    }

    /// <summary>
    /// Force refresh items from GitHub.
    /// </summary>
    public async Task<List<GitHubProjectItem>> RefreshItemsAsync()
    {
        var project = _projectService.SelectedProject;
        if (project == null)
            return new();

        _isLoading = true;
        _lastError = null;
        OnBoardStateChanged?.Invoke();

        try
        {
            var items = await _projectService.FetchProjectItemsAsync();

            // Update cache
            _itemsCache[project.Id] = items;
            _cacheTimestamps[project.Id] = DateTime.UtcNow;

            _isLoading = false;
            OnBoardStateChanged?.Invoke();

            return items;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _isLoading = false;
            OnBoardStateChanged?.Invoke();
            return new();
        }
    }

    /// <summary>
    /// Get items grouped by status column.
    /// </summary>
    public Dictionary<string, List<GitHubProjectItem>> GetItemsByStatus(List<GitHubProjectItem> items)
    {
        var project = _projectService.SelectedProject;
        if (project?.StatusField == null)
            return new();

        var result = new Dictionary<string, List<GitHubProjectItem>>();

        // Initialize all columns from the status field options
        foreach (var option in project.StatusField.Options)
        {
            result[option.Name] = new List<GitHubProjectItem>();
        }

        // Add a catch-all for items with no status or unrecognized status
        result["(No Status)"] = new List<GitHubProjectItem>();

        // Group items by their status
        foreach (var item in items)
        {
            var status = item.Status ?? "(No Status)";
            if (result.ContainsKey(status))
            {
                result[status].Add(item);
            }
            else
            {
                result["(No Status)"].Add(item);
            }
        }

        // Remove empty "(No Status)" column if not needed
        if (result["(No Status)"].Count == 0)
        {
            result.Remove("(No Status)");
        }

        return result;
    }

    /// <summary>
    /// Get status field options for the selected project.
    /// </summary>
    public List<GitHubStatusOption> GetStatusOptions()
    {
        return _projectService.SelectedProject?.StatusField?.Options ?? new();
    }

    /// <summary>
    /// Check if board data is available for the selected project.
    /// </summary>
    public bool HasCachedData()
    {
        var project = _projectService.SelectedProject;
        if (project == null)
            return false;
        return _itemsCache.ContainsKey(project.Id);
    }

    /// <summary>
    /// Clear cache for all projects or a specific project.
    /// </summary>
    public void ClearCache(string? projectId = null)
    {
        if (projectId != null)
        {
            _itemsCache.Remove(projectId);
            _cacheTimestamps.Remove(projectId);
        }
        else
        {
            _itemsCache.Clear();
            _cacheTimestamps.Clear();
        }
        OnBoardStateChanged?.Invoke();
    }

    /// <summary>
    /// Get stats for the board.
    /// </summary>
    public BoardStats GetStats(List<GitHubProjectItem> items)
    {
        var project = _projectService.SelectedProject;
        return new BoardStats
        {
            TotalItems = items.Count,
            ColumnCount = project?.StatusField?.Options.Count ?? 0,
            OpenItems = items.Count(i => i.State?.ToUpper() != "CLOSED"),
            ClosedItems = items.Count(i => i.State?.ToUpper() == "CLOSED"),
            LastRefresh = _cacheTimestamps.TryGetValue(project?.Id ?? "", out var ts) ? ts : null
        };
    }
}

/// <summary>
/// Statistics for the GitHub board.
/// </summary>
public class BoardStats
{
    public int TotalItems { get; set; }
    public int ColumnCount { get; set; }
    public int OpenItems { get; set; }
    public int ClosedItems { get; set; }
    public DateTime? LastRefresh { get; set; }
}
