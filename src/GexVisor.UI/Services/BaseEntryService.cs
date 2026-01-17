using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Generic CRUD service for entry-based data with localStorage persistence.
/// Provides common operations for all journal-type features.
/// </summary>
/// <typeparam name="T">Entry type implementing IEntry</typeparam>
public abstract class BaseEntryService<T> where T : class, IEntry
{
    protected readonly LocalStorageService Storage;
    protected readonly string StorageKey;
    protected List<T> Entries = [];

    public event Action? OnEntriesChanged;

    public IReadOnlyList<T> All => Entries;
    public int Count => Entries.Count;

    protected BaseEntryService(LocalStorageService storage, string storageKey)
    {
        Storage = storage;
        StorageKey = storageKey;
    }

    /// <summary>
    /// Load entries from localStorage.
    /// </summary>
    public virtual async Task LoadAsync()
    {
        var stored = await Storage.GetAsync<List<T>>(StorageKey);
        Entries = stored ?? [];
        OnEntriesChanged?.Invoke();
    }

    /// <summary>
    /// Save entries to localStorage.
    /// </summary>
    protected virtual async Task SaveAsync()
    {
        await Storage.SetAsync(StorageKey, Entries);
        OnEntriesChanged?.Invoke();
    }

    /// <summary>
    /// Add a new entry.
    /// </summary>
    public virtual async Task AddAsync(T entry)
    {
        Entries.Insert(0, entry); // New entries at top
        await SaveAsync();
    }

    /// <summary>
    /// Update an existing entry.
    /// </summary>
    public virtual async Task UpdateAsync(T updated)
    {
        var index = Entries.FindIndex(e => e.Id == updated.Id);
        if (index >= 0)
        {
            Entries[index] = updated;
            await SaveAsync();
        }
    }

    /// <summary>
    /// Delete an entry by ID.
    /// </summary>
    public virtual async Task DeleteAsync(Guid id)
    {
        Entries.RemoveAll(e => e.Id == id);
        await SaveAsync();
    }

    /// <summary>
    /// Get an entry by ID.
    /// </summary>
    public T? GetById(Guid id) => Entries.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Filter entries by tags (any match).
    /// </summary>
    public IEnumerable<T> FilterByTags(IEnumerable<string> tags)
    {
        var tagSet = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Entries.Where(e => e.Tags.Any(t => tagSet.Contains(t)));
    }

    /// <summary>
    /// Filter entries by date range.
    /// </summary>
    public IEnumerable<T> FilterByDateRange(DateTime start, DateTime end)
    {
        return Entries.Where(e => e.CreatedAt >= start && e.CreatedAt <= end);
    }

    /// <summary>
    /// Search entries by text (implementation varies by entry type).
    /// </summary>
    public abstract IEnumerable<T> Search(string query);

    /// <summary>
    /// Get all unique tags across entries.
    /// </summary>
    public IEnumerable<string> GetAllTags()
    {
        return Entries
            .SelectMany(e => e.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t);
    }

    /// <summary>
    /// Clear all entries.
    /// </summary>
    public async Task ClearAsync()
    {
        Entries.Clear();
        await SaveAsync();
    }
}
