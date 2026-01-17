# SQLite in Blazor WebAssembly - Research Findings

**Issue:** #18 - Research and Integrate a Wasm-Compatible SQLite Library
**Date:** 2026-01-17

## Overview

This document summarizes research into running SQLite client-side in a Blazor WebAssembly application.

## Options Evaluated

### 1. sql.js (Recommended)

**Description:** JavaScript port of SQLite compiled to WebAssembly. Accessed via JS Interop from Blazor.

**Pros:**
- Most mature and battle-tested solution
- No native WASM compilation needed (faster builds)
- Well-documented with active community
- Simple integration via JS Interop
- Works reliably for read-only and in-memory databases

**Cons:**
- Requires JavaScript interop layer
- No direct EF Core support
- Database is in-memory by default (requires IndexedDB for persistence)

**Best For:** Read-only databases, demonstrations, data exploration tools

### 2. SqliteWasmHelper (Not Recommended)

**Description:** NuGet package that wraps SQLite with EF Core support and browser cache persistence.

**Status:** **Archived** (February 2025) - Not recommended for new projects.

**Was Good For:** EF Core integration, automatic cache persistence

### 3. SqliteWasmBlazor

**Description:** Newer approach using sqlite-wasm in a Web Worker with OPFS (Origin Private File System) for real filesystem persistence.

**Pros:**
- Full EF Core support
- Real persistent storage via OPFS
- Worker-based (non-blocking)

**Cons:**
- Newer, less battle-tested
- More complex architecture
- OPFS browser support varies

**Best For:** Full CRUD applications needing EF Core and persistence

### 4. Microsoft.Data.Sqlite with Native WASM

**Description:** Official Microsoft library compiled to WASM using native AOT.

**Requirements:**
```xml
<PropertyGroup>
    <WasmBuildNative>true</WasmBuildNative>
</PropertyGroup>
```

**Pros:**
- Official Microsoft support
- Direct .NET API access
- Full EF Core compatibility

**Cons:**
- Significantly slower build times
- Larger WASM bundle size
- Complex setup with native dependencies
- Requires wasm-tools workload

**Best For:** Applications already using native WASM compilation

## Recommendation for GexVisor

**Chosen Approach:** sql.js with JavaScript Interop

### Rationale:

1. **Use Case Match:** GexVisor needs to query bundled GEX research data (read-only). sql.js excels at this.

2. **Build Simplicity:** No native WASM compilation means fast builds and smaller bundle.

3. **Reliability:** sql.js is the most proven solution for browser-based SQLite.

4. **Future Flexibility:** Can enhance with IndexedDB persistence later if needed.

5. **Current Data Format:** Existing data is JSON. Migration to SQLite can be incremental.

## Implementation Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Blazor Component                      │
│                                                         │
│  await SqliteService.QueryAsync("SELECT * FROM gex")    │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                   SqliteService.cs                       │
│                                                         │
│  - InitAsync(): Load sql.js                             │
│  - OpenAsync(bytes): Load .db file                      │
│  - QueryAsync(sql): Execute SELECT                      │
│  - ExportAsync(): Get database bytes                    │
│  - CloseAsync(): Clean up                               │
└─────────────────────────┬───────────────────────────────┘
                          │ JS Interop
┌─────────────────────────▼───────────────────────────────┐
│                 sqljsInterop.js                          │
│                                                         │
│  - init(): Load sql-wasm.js                             │
│  - openDb(bytes): Create SQL.Database                   │
│  - exec(id, sql): Run query, return rows                │
│  - export(id): Get Uint8Array                           │
│  - close(id): Free memory                               │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                sql.js (WebAssembly)                      │
│                                                         │
│  SQLite compiled to WASM, runs in browser               │
└─────────────────────────────────────────────────────────┘
```

## Files Required

1. **wwwroot/sqljs/sql-wasm.js** - sql.js library
2. **wwwroot/sqljs/sql-wasm.wasm** - SQLite WASM binary
3. **wwwroot/sqljs/sqljsInterop.js** - Custom interop layer
4. **Services/SqliteService.cs** - C# service wrapper

## Usage Example

```csharp
@inject SqliteService Sql
@inject HttpClient Http

@code {
    private string? _dbId;

    protected override async Task OnInitializedAsync()
    {
        await Sql.InitAsync();

        // Load database file
        var bytes = await Http.GetByteArrayAsync("data/gex_research.db");
        _dbId = await Sql.OpenAsync(bytes);

        // Query data
        var results = await Sql.QueryAsync(_dbId,
            "SELECT date, gex, price FROM daily_gex WHERE symbol = 'SPY' LIMIT 10");
    }
}
```

## Next Steps

1. [ ] Download sql.js files from CDN
2. [ ] Create sqljsInterop.js interop layer
3. [ ] Create SqliteService.cs C# wrapper
4. [ ] Create sample gex_research.db with test data
5. [ ] Build proof-of-concept component
6. [ ] Verify with bundled database file

## Sources

- [Run SQLite in the Browser with Blazor WebAssembly](https://dev.to/auyeungdavid_2847435260/run-sqlite-in-the-browser-with-blazor-webassembly-a-practical-step-by-step-guide-2e2p)
- [sql.js GitHub Repository](https://github.com/sql-js/sql.js)
- [SqliteWasmHelper (Archived)](https://github.com/JeremyLikness/SqliteWasmHelper)
- [Offline Data in Blazor with WASM, SQLite & IndexedDB](https://mzansibytes.com/2025/07/01/offline-data-in-blazor-with-wasm-sqlite-indexeddb/)
