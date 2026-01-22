# Architecture Reference

Detailed technical documentation of the GexVisor system architecture.

## Service Inventory

GexVisor has 27 services organized into 6 functional domains.

### Core Services (3)

| Service | Lifetime | Interface | Responsibility |
|---------|----------|-----------|----------------|
| `GexStateService` | Singleton | ✅ `IGexStateService` | Central state container for visualization |
| `GexDataService` | Scoped | ✅ `IGexDataService` | Load GEX data from JSON, provide asset class queries |
| `LocalStorageService` | Scoped | ✅ `ILocalStorageService` | Browser localStorage via JS interop |

**IGexDataService Interface Methods:**
- `GetAssetClasses()` - List unique asset classes from index
- `GetSymbolsForClass(string assetClass)` - Get symbols for specific asset class
- `GetSymbolInfo(string symbol)` - Get metadata for specific symbol

### Journal Services (7)

All inherit from `BaseEntryService<T>` using the Template Method pattern.

| Service | Storage Key | Entry Type |
|---------|-------------|------------|
| `NotebookService` | `gexvisor.notebook` | Research notes |
| `PaperTradeService` | `gexvisor.paperTrades` | Simulated trades |
| `TradeLogService` | `gexvisor.tradeLogs` | Trade journal with decision metadata |
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

### Utilities (4)

| Service | Responsibility |
|---------|----------------|
| `TagService` | Autocomplete, predefined + custom tags |
| `SqliteService` | WASM SQLite queries (sql.js) |
| `DecisionMetadataParser` | Parse autotrader logs (JSON/CSV), extract decision metadata |
| `TaskPersistenceService` | JSON file persistence for ToDo tasks |

### Live Data Services (7)

| Service | Lifetime | Interface | Responsibility |
|---------|----------|-----------|----------------|
| `ApiConfigService` | Singleton | ✅ `IApiConfigService` | API key management (config.json + env vars) |
| `MarketDataService` | Singleton | ✅ `IMarketDataService` | Quote/bar fetching with provider fallback |
| `OptionsChainService` | Singleton | ✅ `IOptionsChainService` | Options chain fetching from Alpha Vantage |
| `GexCalculationService` | Singleton | ✅ `IGexCalculationService` | GEX calculation engine |
| `SqliteCacheService` | Singleton | ✅ `ICacheService` | SQLite-based persistent cache |
| `MarketDataCacheService` | Singleton | - | Quote/bar caching with TTL |
| `OptionsChainCacheService` | Singleton | - | Options chain caching with TTL |

**Provider Fallback Strategy:**

1. `MarketDataService` tries providers in order: Alpaca → Finnhub → Polygon
2. Returns first successful response with provider attribution
3. Rate limiting: Alpha Vantage = 5 calls/min, others = 60 calls/min

**GEX Calculation Formula:**

```
GEX = gamma × OI × 100 × S²
```

Where S = spot price. Net GEX = Call GEX - Put GEX.

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
builder.Services.AddScoped<TradeLogService>();
builder.Services.AddScoped<DecisionMetadataParser>();

// Live market data services (Epic #145)
builder.Services.AddSingleton<IApiConfigService, ApiConfigService>();
builder.Services.AddSingleton<ICacheService>(sp => new SqliteCacheService(".cache/gexvisor.db"));
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IOptionsChainService, OptionsChainService>();
builder.Services.AddSingleton<IGexCalculationService, GexCalculationService>();
```

---

## GEX Calculation Pipeline

### Overview

The GEX (Gamma Exposure) calculation service converts raw options chain data into actionable market metrics.

### Architecture

```text
OptionsChainService → GexCalculationService → GexChart UI
         ↓                     ↓                    ↓
   Alpha Vantage          Formulas              Visualization
   (Greeks, OI)        (Σ gamma × OI × S²)      (Strike bars)
```

### Core Formula

```
GEX_strike = gamma × open_interest × 100 × spot_price²
Total_GEX = Σ(call_GEX) - Σ(put_GEX)
```

### Regime Classification

- **Long Gamma**: Net GEX > +10% (dealers short gamma, dampens volatility)
- **Short Gamma**: Net GEX < -10% (dealers long gamma, amplifies volatility)
- **Neutral**: Net GEX within ±10%

### Zero-Gamma Level

Interpolated strike price where net GEX crosses zero. Acts as pivot point for dealer hedging behavior.

### Implementation

- **Service**: `IGexCalculationService` in `GexVisor.Api/Services/`
- **Models**: `GexData.cs` (GexRegime, StrikeGamma, GexCalculationResult)
- **API Endpoint**: `GET /api/gex/{symbol}`
- **Reference**: Ported from `autogen-trader` Python implementation

### Code-Behind Pattern (Blazor)

**Magic Numbers Refactoring**: Complex components like GexChart.razor now use code-behind pattern:

```
GexChart.razor         (Markup only)
GexChart.razor.cs      (Logic + Constants)
GexChart.razor.css     (Styles)
```

**Benefits**:
- Testable calculation logic
- Named constants instead of magic numbers
- Better IntelliSense/navigation
- Separation of concerns

**See**: [ADR 0007](adr/0007-blazor-code-behind-pattern.md) for decision rationale

### AppConstants Centralization

Magic numbers and configuration values are centralized in `Configuration/AppConstants.cs`:

| Nested Class | Purpose | Example Constants |
|--------------|---------|-------------------|
| `GitHub` | API limits | MaxProjectsPerQuery, MaxItemsPerProject |
| `Cache` | TTL durations | BoardStateCacheMinutes |
| `UI` | Display limits | MaxTagSuggestions |
| `Validation` | Pattern thresholds | MinimumSampleSize, MinimumWinRatePercent |
| `Finance` | Trading constants | TradingDaysPerYear, DefaultContractMultiplier |
| `Correlation` | Analysis thresholds | StrongCorrelationThreshold |
| `DataFormat` | Parsing formats | DateFormat ("yyyy-MM-dd") |
| `Keyboard` | UI shortcuts | Sections[], SidebarShortcuts[] |

**Keyboard Shortcuts Pattern**:
```csharp
// Two collections for different display contexts
public static class Keyboard
{
    // Detailed overlay (11 items in 3 sections)
    public static readonly ShortcutSection[] Sections = [...];

    // Compact sidebar (6 grouped rows)
    public static readonly (string Keys, string Action)[] SidebarShortcuts = [...];
}
```

---

## localStorage Keys

All persistence uses browser localStorage via `LocalStorageService`.

| Key | Type | Description |
|-----|------|-------------|
| `gexvisor.settings` | `AppSettings` | Playback speed, axis scales, last symbol |
| `gexvisor.notebook` | `List<NotebookEntry>` | Research notes |
| `gexvisor.paperTrades` | `List<PaperTrade>` | Paper trading journal |
| `gexvisor.tradeLogs` | `List<OptionsLog>` | Autotrader trade logs with decision metadata |
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
| `TradeLogService` | `OnTradesChanged` | Add/Update/Delete trade | TradeLogging page components |
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

### Trade Journal Import Flow

```
1. User uploads CSV/JSON file in TradeLogging.razor
2. File content read as string
3. TradeLogging calls DecisionMetadataParser.ParseJsonLog() or ParseCsvLog()
4. Parser extracts:
   - OptionsLog entries (ticker, prices, dates, multiplier)
   - TradeDecision metadata (patterns, confidence, regime context)
5. Parser returns (List<OptionsLog>, List<TradeDecision>)
6. TradeLogging calls TradeLogService.ImportTrades(trades, decisions)
7. TradeLogService merges with existing entries
8. TradeLogService saves to localStorage ("tradeLogs" and "decisions" keys)
9. TradeLogService fires OnTradesChanged
10. UI components re-render: TradeGrid, TradeDetailModal, DecisionTimeline
11. User sees imported trades with decision metadata (PatternBadge, RegimeContextCard)
```

---

## Performance Considerations

### Known Hotspots

| Location | Issue | Impact | Mitigation | Status |
|----------|-------|--------|------------|--------|
| `GexChart.CalculateBars()` | Runs every state change | CPU during playback | ✅ Memoization | Resolved |
| `ComparisonAnalysisService` | O(n²) pairwise correlation | Slow for many assets | Limited to 4 assets | Open |
| `StatusMapper.MapStatus()` | Called per item render | Hot path | Static compiled regex | Open |

### Caching Strategies

| Service | Cache | TTL | Invalidation |
|---------|-------|-----|--------------|
| `GexDataService` | `_index` | Session | None (load once) |
| `BoardStateService` | `_itemsCache` | 5 minutes | Manual refresh |
| `TagService` | `_allTagsCache` | Until modified | On add/remove tag |
| `RegimeAnalysis` | In component | Until timeline changes | On new data load |

---

## Known Architecture Issues

### Recently Resolved

| Issue | Title | Resolution | Commit |
|-------|-------|------------|--------|
| ✅ #135 | GitHub pagination | Cursor-based pagination implemented | 02a3ebd |
| ✅ #138 | Demo data staleness | Fixed with dynamic date generation | 97251f7 |
| ✅ #139 | GexChart performance | Added memoization to prevent unnecessary recalculations | 317fc25 |
| ✅ #140 | Trade Journal implementation | Complete feature with import/export, 172 tests passing | - |
| ✅ #141 | IGexStateService interface extraction | Interface created, enables component testing | - |

### Open Issues

Tracked in GitHub issues:

| Issue | Title | Priority |
|-------|-------|----------|
| #149 | GEX Calculation Engine | High |
| #147 | Market Data Service (Alpaca/Finnhub) | High |
| #146 | API Key Configuration Service | High |
| #144 | Restore radar visualization (Research Complexity Map) | Medium |
| #111 | Simple Chart Viewer (Epic) | Medium |
| #110 | Self-Tracking Metrics Dashboard (Epic) | Medium |

### Recently Closed (Session 19)

| Issue | Title | Resolution |
|-------|-------|------------|
| #165 | Extract keyboard shortcuts to shared collection | AppConstants.Keyboard class |
| #166 | Extract GEX chart legend to C# collection | LegendItems tuple array |
| #167 | Extract GexChart magic numbers to constants | ChartConfiguration nested class |
| #168 | Extract Paper2Agreement bar count to constant | BarCount constant |
| #153 | Responsive font sizing with CSS clamp() | clamp() on axis labels |
| #143 | Trade Journal code quality improvements | Exception handling, delete confirmation |

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
