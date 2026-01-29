# ADR-0001: SQLite WASM Library Selection

## Status

Accepted

## Date

2026-01-17

## Context

GexVisor needs client-side database capabilities to query bundled GEX research data in the browser. The application is built with Blazor WebAssembly, which runs entirely in the browser without server-side processing.

**Requirements:**

- Query pre-built SQLite database files bundled with the app
- Read-only access (no write operations needed)
- Fast build times (avoid native WASM compilation)
- Small bundle size
- Reliable browser compatibility

**Related Issue:** #18 - Research and Integrate a Wasm-Compatible SQLite Library

## Decision

Use **sql.js** with JavaScript interop for client-side SQLite queries.

**Architecture:**

```bash
Blazor Component
       │
       ▼
SqliteService.cs (C# wrapper)
       │
       ▼ JS Interop
sqljsInterop.js
       │
       ▼
sql.js (SQLite compiled to WASM)
```

## Consequences

**Benefits:**

- Most mature and battle-tested browser SQLite solution
- No native WASM compilation needed (faster builds, smaller bundles)
- Well-documented with active community
- Simple integration via JS interop
- Works reliably for read-only and in-memory databases

**Drawbacks:**

- Requires JavaScript interop layer (not pure .NET)
- No direct Entity Framework Core support
- Database is in-memory by default (requires IndexedDB for persistence if needed later)

**Implementation notes:**

- Database files bundled in `wwwroot/data/`
- SqliteService handles initialization and query execution
- Results returned as JSON arrays for Blazor consumption

## Alternatives Considered

### SqliteWasmHelper

NuGet package wrapping SQLite with EF Core support.

**Rejected because:** Archived in February 2025. Not recommended for new projects.

### SqliteWasmBlazor

Newer approach using sqlite-wasm in a Web Worker with OPFS persistence.

**Rejected because:**

- More complex architecture than needed
- OPFS browser support varies
- Overkill for read-only use case

### Microsoft.Data.Sqlite with Native WASM

Official Microsoft library compiled to WASM using native AOT.

**Rejected because:**

- Significantly slower build times
- Larger WASM bundle size
- Complex setup with native dependencies
- Requires wasm-tools workload

## References

- [sql.js GitHub Repository](https://github.com/sql-js/sql.js)
- [Run SQLite in the Browser with Blazor WebAssembly](https://dev.to/auyeungdavid_2847435260/run-sqlite-in-the-browser-with-blazor-webassembly-a-practical-step-by-step-guide-2e2p)
- [SqliteWasmHelper (Archived)](https://github.com/JeremyLikness/SqliteWasmHelper)
