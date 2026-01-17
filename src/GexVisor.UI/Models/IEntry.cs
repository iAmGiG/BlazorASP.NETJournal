namespace GexVisor.UI.Models;

/// <summary>
/// Common interface for all entry-based data models (notebook entries, trades, tasks, etc.)
/// Provides shared properties for CRUD operations and filtering.
/// </summary>
public interface IEntry
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
    IReadOnlyList<string> Tags { get; }
}

/// <summary>
/// Base record implementing IEntry with common properties.
/// All journal-type entries should inherit from this.
/// </summary>
public abstract record BaseEntry : IEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public List<string> Tags { get; init; } = [];

    IReadOnlyList<string> IEntry.Tags => Tags;
}

/// <summary>
/// Entry with visualizer context (date, price, GEX values).
/// Used for entries linked to specific points in the GEX timeline.
/// </summary>
public abstract record ContextualEntry : BaseEntry
{
    /// <summary>Linked date in the visualizer timeline (YYYY-MM-DD).</summary>
    public string? LinkedDate { get; init; }

    /// <summary>Price at the time of entry creation.</summary>
    public decimal? PriceAtCreation { get; init; }

    /// <summary>GEX value at the time of entry creation.</summary>
    public decimal? GexAtCreation { get; init; }

    /// <summary>Gamma regime at the time of entry creation.</summary>
    public bool? IsNegativeGammaAtCreation { get; init; }
}
