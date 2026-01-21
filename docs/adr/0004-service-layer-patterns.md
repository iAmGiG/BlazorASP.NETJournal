# ADR-0004: Service Layer Design Patterns

## Status

Accepted (2022-06, evolved over time)

## Context

GexVisor has 22 services across 5 functional domains. Different domains have different requirements:

- Journal services: CRUD operations on entries (Create, Read, Update, Delete)
- State services: Singleton state with event notifications
- GitHub services: API calls with caching (5-minute TTL)
- Comparison services: Stateless calculations (correlation, divergence)
- Utility services: Specialized tasks (tags, SQLite, JSON parsing)

## Decision

Use mixed patterns based on domain requirements:

1. **Template Method Pattern** (Journal services)
2. **Singleton State with Events** (GexStateService)
3. **TTL Caching** (BoardStateService)
4. **Stateless Pure Functions** (ComparisonAnalysisService)

## Pattern Details

### Pattern 1: Template Method (BaseEntryService\<T\>)

**Services Using This:**

- NotebookService
- PaperTradeService
- BacktestService
- AnnotationService
- ResearchTaskService

**Implementation:**

```csharp
// BaseEntryService.cs (simplified)
public abstract class BaseEntryService<T> where T : IEntry
{
    protected readonly ILocalStorageService _localStorage;
    protected readonly string _storageKey;
    protected List<T> _entries = new();

    public event Action? OnStateChanged;

    // Template methods (can be overridden)
    public virtual async Task<IEnumerable<T>> LoadAsync()
    {
        var json = await _localStorage.GetItemAsync<string>(_storageKey);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }

    public virtual async Task SaveAsync(IEnumerable<T> entries)
    {
        var json = JsonSerializer.Serialize(entries);
        await _localStorage.SetItemAsync(_storageKey, json);
        OnStateChanged?.Invoke();
    }

    // Concrete methods (inherited by all)
    public async Task AddAsync(T entry) { /*...*/ }
    public async Task UpdateAsync(Guid id, T entry) { /*...*/ }
    public async Task DeleteAsync(Guid id) { /*...*/ }
}
```

**Rationale:**

- DRY: Avoid duplicate CRUD logic in 6 services
- Consistency: All journal services behave identically
- Extensibility: Override `LoadAsync/SaveAsync` for custom logic (e.g., TradeLogService)

### Pattern 2: Singleton State with Events

**Services Using This:**

- GexStateService (Singleton)
- ComparisonService (Scoped, but stateful within scope)

**Implementation:**

```csharp
// GexStateService.cs
public class GexStateService
{
    private GexState _state = new();

    public event Action? OnStateChanged;

    public GexState State => _state;

    public void UpdateState(GexState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke();
    }
}
```

**Components Subscribe:**

```csharp
@implements IDisposable

protected override void OnInitialized()
{
    GexState.OnStateChanged += StateHasChanged;
}

public void Dispose()
{
    GexState.OnStateChanged -= StateHasChanged;
}
```

**Rationale:**

- Reactive UI: Components auto-update when state changes
- Centralized state: Avoid prop drilling
- Event-driven: Decouples state source from consumers

### Pattern 3: TTL Caching (BoardStateService)

**Implementation:**

```csharp
private DateTime? _lastFetch;
private const int CACHE_TTL_MINUTES = 5;

public async Task<List<GitHubProject>> FetchProjectsAsync()
{
    if (_projects != null && _lastFetch.HasValue &&
        DateTime.UtcNow - _lastFetch.Value < TimeSpan.FromMinutes(CACHE_TTL_MINUTES))
    {
        return _projects; // Return cached
    }

    // Fetch from API
    _projects = await _githubService.GetProjectsAsync();
    _lastFetch = DateTime.UtcNow;
    return _projects;
}
```

**Rationale:**

- Reduce API calls: GitHub rate limits (5000/hour)
- Improve performance: Cached responses instant
- Concurrency guard: Prevent thundering herd

### Pattern 4: Stateless Pure Functions

**Services Using This:**

- ComparisonAnalysisService
- DecisionMetadataParser
- StatusMapper

**Example:**

```csharp
public class ComparisonAnalysisService
{
    // No state, no DI dependencies
    public double CalculateCorrelation(List<double> values1, List<double> values2)
    {
        // Pure function: same inputs = same output
        // No side effects
    }
}
```

**Rationale:**

- Testability: No mocking required
- Thread-safe: No shared state
- Predictable: No side effects

## Consequences

### Positive

- Pattern per domain: Each domain uses optimal pattern
- Well-tested: Template Method reduces bugs
- Clear responsibilities: Easy to understand service purpose

### Negative

- Inconsistency: New devs must learn 4 patterns
- Abstraction overhead: BaseEntryService\<T\> adds indirection

## Alternatives Considered

### 1. Repository Pattern (with interfaces for all services)

- **Pros**: Testability, dependency inversion
- **Cons**: Overkill for simple CRUD, creates interface explosion
- **Rejected**: Template Method provides enough abstraction

### 2. Redux-Style State Management (single global store)

- **Pros**: Predictable state updates, time-travel debugging
- **Cons**: Boilerplate (actions, reducers), unnecessary for small app
- **Rejected**: Event-driven state simpler for GexVisor's needs

## References

- Template Method Pattern: https://refactoring.guru/design-patterns/template-method
- Related: [ADR-0006: State Management](0006-state-management.md)
