# ADR-0003: LocalStorage vs. IndexedDB for Persistence

## Status

Accepted (2021-12, revisited 2024-01)

## Context

GexVisor needs to persist user data:

- Trade journal entries (TradeLog, 50-500 entries typical)
- Paper trades (PaperTrade, 20-200 entries)
- Notebook entries (NotebookEntry, 10-100 entries)
- Research tasks (ResearchTask, 20-100 tasks)
- Backtest results (BacktestResult, 10-50 results)

Storage size: 1-10 MB typical, 50 MB max (worst case)

## Decision

Use browser LocalStorage (via Blazor LocalStorage library) for all persistence. Serialize data as JSON strings with `System.Text.Json`.

## Consequences

### Positive

- Simplicity: Key-value API, no schema migrations
- Synchronous access: `GetItemAsync()` wraps synchronous localStorage
- Cross-browser support: All modern browsers (IE11+)
- No external dependencies: Built into browsers
- Sufficient capacity: 5-10 MB limit (exceeds typical usage)

### Negative

- Size limit: 5-10 MB varies by browser (quota errors possible)
- No transactions: Multiple writes not atomic
- No indexing: Must deserialize entire dataset to filter
- Blocking: Synchronous under the hood (blocks main thread)

## Alternatives Considered

### 1. IndexedDB

- **Pros**: Larger storage (50 MB+), indexing, transactions, async
- **Cons**: Complex API, requires schema versioning, overkill for small datasets
- **Rejected**: LocalStorage sufficient for current use case
- **Future**: ADR 0001 (SQLite WASM) might supersede both

### 2. SQLite WASM (sql.js)

- **Pros**: SQL queries, relational data, 100 MB+ storage
- **Cons**: 1 MB runtime overhead, complex setup
- **Status**: ADR 0001 accepted for `SqliteService` (read-only queries)
- **Decision**: LocalStorage for writes, SQLite for analytics queries

### 3. Remote API + Database

- **Pros**: Unlimited storage, multi-device sync, backups
- **Cons**: Requires server hosting, auth, network dependency
- **Rejected**: Conflicts with offline-first requirement

## Quota Handling

```csharp
// PaperTradeService.cs
public async Task SaveAsync(IEnumerable<PaperTrade> trades)
{
    try
    {
        var json = JsonSerializer.Serialize(trades, _jsonOptions);
        await _localStorage.SetItemAsync(STORAGE_KEY, json);
    }
    catch (JSException ex) when (ex.Message.Contains("QuotaExceededError"))
    {
        // User notification: "Storage quota exceeded. Export data and clear old entries."
        throw new InvalidOperationException("LocalStorage quota exceeded", ex);
    }
}
```

## References

- LocalStorage API: https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage
- Blazor LocalStorage library: Blazored.LocalStorage (NuGet)
- Related: [ADR-0001: SQLite WASM Selection](0001-sqlite-wasm-selection.md)
