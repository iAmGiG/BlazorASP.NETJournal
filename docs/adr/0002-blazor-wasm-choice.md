# ADR-0002: Blazor WebAssembly Architecture Choice

## Status

Accepted (2021-12)

## Context

GexVisor needed to visualize large GEX datasets (1010+ data points per symbol) with interactive charts. Requirements:

- Client-side processing (no server round-trips for filtering/sorting)
- Offline-first operation (research tool, not live trading)
- Fast rendering of SVG charts
- C# codebase consistency

## Decision

Use Blazor WebAssembly (WASM) for the frontend, compiled to WebAssembly and running entirely in the browser.

## Consequences

### Positive

- Full C# stack (shared models between UI and Core)
- Client-side processing enables fast filtering/sorting (no server latency)
- Offline-first: Works without network after initial load
- LocalStorage persistence (no database server required)
- Deployment simplicity (static files, no server hosting costs)

### Negative

- Initial load time: ~2-3 seconds (WASM bundle size)
- Debugging complexity (browser DevTools + Blazor DevTools)
- Limited browser APIs (must use JS interop for canvas, localStorage, etc.)
- Memory constraints (browser heap limits, ~2GB typical)

## Alternatives Considered

### 1. Blazor Server

- **Pros**: Smaller initial payload, full .NET API access
- **Cons**: Requires persistent SignalR connection, server hosting costs, latency
- **Rejected**: Incompatible with offline-first requirement

### 2. SPA Framework (Vue.js, React)

- **Pros**: Mature ecosystem, smaller bundle sizes, faster initial load
- **Cons**: Different language (TypeScript), no C# model sharing, rewrite existing logic
- **Rejected**: Team expertise in C#, existing GEX calculation logic in .NET

### 3. Desktop App (WPF, WinForms, Electron)

- **Pros**: Full OS integration, no browser constraints
- **Cons**: Platform-specific builds, no web distribution, harder updates
- **Rejected**: Web distribution preferred for research collaboration

## References

- Blazor WASM docs: https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly
