# Architecture Reference

Detailed technical documentation of the GexVisor system architecture.

## Service Inventory

GexVisor has 20 services organized into 5 functional domains.

### Core Services (3)

| Service | Lifetime | Interface | Responsibility |
|---------|----------|-----------|----------------|
| `GexStateService` | Singleton | ❌ None | Central state container for visualization |
| `GexDataService` | Scoped | `IGexDataService` | Load GEX data from JSON files |
| `LocalStorageService` | Scoped | `ILocalStorageService` | Browser localStorage via JS interop |

### Journal Services (6)

All inherit from `BaseEntryService<T>` using the Template Method pattern.

| Service | Storage Key | Entry Type |
|---------|-------------|------------|
| `NotebookService` | `gexvisor.notebook` | Research notes |
| `PaperTradeService` | `gexvisor.paperTrades` | Simulated trades |
| `BacktestService` | `gexvisor.backtests` | Strategy results |
| `AnnotationService` | `gexvisor.annotations` | Pattern annotations |
| `ResearchTaskService` | `gexvisor.tasks` | Kanban tasks |
| `BaseEntryService<T>` | (abstract) | CRUD template |

### GitHub Integration (4)

| Service | Responsibility | Key Feature |
|---------|----------------|-------------|
| `GitHubAuthService` | OAuth Device Flow | Polling with exponential backoff |
| `GitHubProjectService` | GraphQL queries | Project/item fetching |
| `BoardStateService` | Caching layer | 5-minute TTL, concurrency guard |
| `StatusMapper` | Status normalization | Word boundary matching |

### Cross-Asset Analysis (2)

| Service | State | Responsibility |
|---------|-------|----------------|
| `ComparisonService` | Stateful | Symbol selection, parallel loading |
| `ComparisonAnalysisService` | Stateless | Correlation calculations |

### Utilities (2)

| Service | Responsibility |
|---------|----------------|
| `TagService` | Autocomplete, predefined + custom tags |
| `SqliteService` | WASM SQLite queries (sql.js) |

---

## State Management Patterns

### Pattern 1: Reactive Event-Driven

Used by `GexStateService`, `BaseEntryService<T>`

```csharp
public event Action? OnStateChanged;

private void NotifyStateChanged() => OnStateChanged?.Invoke();

public void SetCurrentIndex(int index)
{
    _state = _state with { CurrentIndex = index };
    NotifyStateChanged();
}
```

**Subscribers:** Components subscribe in `OnInitialized()`, call `StateHasChanged()` on event.

### Pattern 2: Lazy Caching with TTL

Used by `BoardStateService`

```csharp
private Dictionary<string, List<GitHubProjectItem>> _itemsCache = new();
private Dictionary<string, DateTime> _cacheTimestamps = new();
private bool _isLoading;

public async Task<List<GitHubProjectItem>> GetItemsAsync(string projectId)
{
    if (IsCacheValid(projectId)) return _itemsCache[projectId];
    if (_isLoading) return _itemsCache.GetValueOrDefault(projectId) ?? [];

    return await RefreshItemsAsync(projectId);
}
```

**TTL:** 5 minutes (`AppConstants.Cache.BoardStateTtlMinutes`)

### Pattern 3: Stateless Pure Calculation

Used by `ComparisonAnalysisService`

```csharp
public CorrelationMetrics CalculateCorrelation(
    AssetComparisonData asset1,
    AssetComparisonData asset2)
{
    // No side effects, no state mutation
    // Result depends only on inputs
    return new CorrelationMetrics { ... };
}
```

**Benefits:** Thread-safe, easily testable, parallelizable.

### Pattern 4: Template Method (Inheritance)

Used by `BaseEntryService<T>` and 5 journal services

```csharp
public abstract class BaseEntryService<T> where T : IEntry
{
    protected readonly ILocalStorageService Storage;
    protected readonly string StorageKey;

    public List<T> Entries { get; protected set; } = [];
    public event Action? OnEntriesChanged;

    // Template methods - inherited by all journal services
    public virtual async Task LoadAsync() { ... }
    public virtual async Task AddAsync(T entry) { ... }
    public virtual async Task UpdateAsync(T entry) { ... }
    public virtual async Task DeleteAsync(Guid id) { ... }
}
```

---

## Dependency Injection Configuration

From `Program.cs`:

```csharp
// Singleton - app-wide state
builder.Services.AddSingleton<GexStateService>();

// Scoped - per page/component tree
builder.Services.AddScoped<IGexDataService, GexDataService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<AnnotationService>();
builder.Services.AddScoped<NotebookService>();
builder.Services.AddScoped<PaperTradeService>();
builder.Services.AddScoped<BacktestService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<ComparisonService>();
builder.Services.AddScoped<ComparisonAnalysisService>();
builder.Services.AddScoped<GitHubAuthService>();
builder.Services.AddScoped<GitHubProjectService>();
builder.Services.AddScoped<BoardStateService>();
builder.Services.AddScoped<StatusMapper>();
builder.Services.AddScoped<SqliteService>();
```

---

## localStorage Keys

All persistence uses browser localStorage via `LocalStorageService`.

| Key | Type | Description |
|-----|------|-------------|
| `gexvisor.settings` | `AppSettings` | Playback speed, axis scales, last symbol |
| `gexvisor.notebook` | `List<NotebookEntry>` | Research notes |
| `gexvisor.paperTrades` | `List<PaperTrade>` | Trade journal |
| `gexvisor.backtests` | `List<BacktestResult>` | Strategy results |
| `gexvisor.annotations` | `List<PatternAnnotation>` | Pattern annotations |
| `gexvisor.tasks` | `List<ResearchTask>` | Local Kanban tasks |
| `gexvisor.customTags` | `List<string>` | User-defined tags |
| `gexvisor.github.auth` | `GitHubAuthState` | OAuth tokens |
| `gexvisor.github.projects` | `GitHubProjectSettings` | Selected project |
| `gexvisor.statusMappings` | `StatusMappingsStore` | Custom status mappings |

---

## Event Catalog

| Service | Event | Fired When | Typical Subscribers |
|---------|-------|------------|---------------------|
| `GexStateService` | `OnStateChanged` | Any state property changes | GexChart, GexHeader, RegimeTimeline |
| `GexStateService` | `OnSettingsChanged` | Axis scale or playback speed changes | GexVisualizer (debounced save) |
| `BaseEntryService<T>` | `OnEntriesChanged` | Add/Update/Delete entry | Journal page components |
| `ComparisonService` | `OnSelectionChanged` | Symbol toggled or loaded | ComparisonDashboard |
| `GitHubAuthService` | `OnAuthStateChanged` | Login/logout | GitHubAuthPanel |
| `GitHubAuthService` | `OnDeviceCodeReceived` | Device flow started | GitHubAuthPanel (show code) |
| `GitHubAuthService` | `OnAuthError` | Auth failure | Error notifications |
| `GitHubProjectService` | `OnProjectsChanged` | Projects fetched | GitHubProjectSelector |
| `BoardStateService` | `OnBoardStateChanged` | Items refreshed | KanbanBoard |

---

## Data Flow Traces

### Load Symbol Timeline

```
1. User selects "SPY" in GexSidebar
2. GexSidebar calls GexDataService.LoadSymbolAsync("spy")
3. GexDataService issues HTTP GET to /data/spy.json
4. JSON deserialized to RawGexTimeline (snake_case)
5. TransformTimeline() converts:
   - Date strings → DateOnly
   - Generates labels ("Mar 2024 - Short γ")
6. Returns GexTimeline to GexSidebar
7. GexSidebar calls GexStateService.SetTimeline(timeline)
8. GexStateService stores _timeline, sets CurrentIndex to 0
9. GexStateService fires OnStateChanged
10. GexChart receives event, calls CalculateBars()
11. GexChart re-renders SVG
```

### Cross-Asset Comparison

```
1. User selects SPY, QQQ, NVDA checkboxes
2. User clicks "Compare" button
3. ComparisonDashboard calls ComparisonService.LoadSymbolsAsync([...])
4. ComparisonService clears _loadedAssets (⚠️ race condition risk)
5. ComparisonService issues 3 parallel GexDataService.LoadSymbolAsync() calls
6. Results collected via Task.WhenAll
7. For each: Creates AssetComparisonData with RegimeAnalysis
8. ComparisonService fires OnSelectionChanged
9. ComparisonDashboard calls ComparisonAnalysisService.GenerateSummary()
10. GenerateSummary calculates:
    - Pairwise correlations (3 pairs)
    - Regime divergence events
    - Dispersion score
11. Returns CrossAssetSummary
12. Dashboard renders CorrelationMatrix and RegimeDivergenceList
```

### Journal Entry Creation

```
1. User fills form in ResearchNotebook
2. User clicks "Save"
3. Page calls NotebookService.AddAsync(entry)
4. NotebookService (via BaseEntryService) inserts at position 0
5. BaseEntryService calls SaveAsync()
6. SaveAsync calls LocalStorageService.SetAsync(key, entries)
7. LocalStorageService invokes JS: localStorage.setItem(key, json)
8. (⚠️ No error handling - silent failure possible)
9. BaseEntryService fires OnEntriesChanged
10. ResearchNotebook re-renders entry list
```

---

## Performance Considerations

### Known Hotspots

| Location | Issue | Impact | Mitigation |
|----------|-------|--------|------------|
| `GexChart.CalculateBars()` | Runs every state change | CPU during playback | Memoization (#139) |
| `ComparisonAnalysisService` | O(n²) pairwise correlation | Slow for many assets | Limited to 4 assets |
| `StatusMapper.MapStatus()` | Called per item render | Hot path | Static compiled regex |

### Caching Strategies

| Service | Cache | TTL | Invalidation |
|---------|-------|-----|--------------|
| `GexDataService` | `_index` | Session | None (load once) |
| `BoardStateService` | `_itemsCache` | 5 minutes | Manual refresh |
| `TagService` | `_allTagsCache` | Until modified | On add/remove tag |
| `RegimeAnalysis` | In component | Until timeline changes | On new data load |

---

## Known Architecture Issues

Tracked in GitHub issues:

| Issue | Title | Priority |
|-------|-------|----------|
| #134 | Extract IGexStateService interface | High |
| #135 | Add GitHub pagination | Low |
| #136 | LocalStorageService silent errors | Medium |
| #137 | ComparisonService race condition | Medium |
| #138 | Demo data staleness | Low |
| #139 | GexChart recalculation performance | Low |

---

## Testing Notes

### Easy to Test (Stateless)

- `ComparisonAnalysisService` - Pure functions
- `StatusMapper` - Static methods with deterministic output
- Domain models - Immutable records

### Hard to Test (Requires Mocking)

- `GexStateService` - Timer-based simulation, requires careful mocking
- `BoardStateService` - Complex cache lifecycle
- `GitHubAuthService` - Polling loop, external API

### Interface Coverage

| Service | Has Interface | Mockable |
|---------|---------------|----------|
| GexDataService | ✅ IGexDataService | Yes |
| LocalStorageService | ✅ ILocalStorageService | Yes |
| GexStateService | ✅ IGexStateService | Yes |
| ComparisonAnalysisService | ❌ None | Yes (stateless) |

---

## Related Documentation

- [diagrams.md](diagrams.md) - Visual Mermaid diagrams
- [state-diagrams.md](state-diagrams.md) - ASCII state machines
- [github-integration.md](github-integration.md) - GitHub API details
- [adr/](adr/) - Architecture Decision Records
