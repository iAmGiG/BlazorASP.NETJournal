using GexVisor.UI.Models;
using System.Text.Json;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for managing research notebook entries.
/// Extends BaseEntryService with notebook-specific functionality.
/// </summary>
public class NotebookService : BaseEntryService<NotebookEntry>
{
    public NotebookService(LocalStorageService storage)
        : base(storage, StorageKeys.NotebookEntries)
    {
    }

    /// <summary>
    /// Get pinned entries first, then recent entries.
    /// </summary>
    public IEnumerable<NotebookEntry> GetOrderedEntries()
    {
        return Entries
            .OrderByDescending(e => e.IsPinned)
            .ThenByDescending(e => e.CreatedAt);
    }

    /// <summary>
    /// Get entries linked to a specific visualizer date.
    /// </summary>
    public IEnumerable<NotebookEntry> GetByLinkedDate(string date)
    {
        return Entries.Where(e => e.LinkedDate == date);
    }

    /// <summary>
    /// Search entries by title and content.
    /// </summary>
    public override IEnumerable<NotebookEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Entries;

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Entries.Where(e =>
            terms.All(term =>
                e.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Content.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase))
            )
        );
    }

    /// <summary>
    /// Toggle pinned status of an entry.
    /// </summary>
    public async Task TogglePinAsync(Guid id)
    {
        var entry = GetById(id);
        if (entry != null)
        {
            var updated = entry with { IsPinned = !entry.IsPinned };
            await UpdateAsync(updated);
        }
    }

    /// <summary>
    /// Export entries as JSON.
    /// </summary>
    public string ExportAsJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(Entries, options);
    }

    /// <summary>
    /// Export entries as Markdown.
    /// </summary>
    public string ExportAsMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Research Notebook Export");
        sb.AppendLine($"*Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var entry in GetOrderedEntries())
        {
            sb.AppendLine($"## {entry.Title}");
            sb.AppendLine();
            sb.AppendLine($"**Date:** {entry.CreatedAt:yyyy-MM-dd HH:mm}");

            if (!string.IsNullOrEmpty(entry.LinkedDate))
                sb.AppendLine($"**Linked to:** {entry.LinkedDate}");

            if (entry.Tags.Count > 0)
                sb.AppendLine($"**Tags:** {string.Join(", ", entry.Tags.Select(t => $"`{t}`"))}");

            if (entry.PriceAtCreation.HasValue)
                sb.AppendLine($"**Price:** ${entry.PriceAtCreation:F2}");

            if (entry.GexAtCreation.HasValue)
                sb.AppendLine($"**GEX:** {entry.GexAtCreation:F2}B");

            sb.AppendLine();
            sb.AppendLine(entry.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
