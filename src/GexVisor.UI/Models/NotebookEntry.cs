namespace GexVisor.UI.Models;

/// <summary>
/// A research notebook entry for documenting observations while exploring GEX regimes.
/// </summary>
public record NotebookEntry : ContextualEntry
{
    /// <summary>Entry title/summary.</summary>
    public required string Title { get; init; }

    /// <summary>Entry content in markdown format.</summary>
    public string Content { get; init; } = "";

    /// <summary>Whether this entry is pinned to the top.</summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Creates a new notebook entry with current visualizer context.
    /// </summary>
    public static NotebookEntry Create(
        string title,
        string content,
        IEnumerable<string>? tags = null,
        string? linkedDate = null,
        decimal? price = null,
        decimal? gex = null,
        bool? isNegativeGamma = null)
    {
        return new NotebookEntry
        {
            Title = title,
            Content = content,
            Tags = tags?.ToList() ?? [],
            LinkedDate = linkedDate,
            PriceAtCreation = price,
            GexAtCreation = gex,
            IsNegativeGammaAtCreation = isNegativeGamma
        };
    }
}
