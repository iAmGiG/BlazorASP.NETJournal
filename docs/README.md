# GexVisor Documentation

GexVisor is an interactive Blazor WebAssembly application for exploring GEX (Gamma Exposure) regime dynamics with real-time state management and research visualization tools.

## Documentation Index

| Document | Description |
|----------|-------------|
| [RESEARCH_ROADMAP.md](RESEARCH_ROADMAP.md) | Multi-phase research trajectory for the GEX-LLM Patterns project |
| [GITHUB_INTEGRATION.md](GITHUB_INTEGRATION.md) | GitHub Projects v2 API integration for task board sync |
| [sqlite-wasm-research.md](sqlite-wasm-research.md) | Research on client-side SQLite with WebAssembly |

## Research Origins

GexVisor is part of the [GEX-LLM Patterns](https://github.com/iAmGiG/gex-llm-patterns) research project, investigating whether Large Language Models can detect and reason about market microstructure patterns.

### Key Research Findings

| Metric | Value | Source |
|--------|-------|--------|
| Detection Rate | 71.5% | Unbiased prompts validation |
| Predictive Accuracy | 91.2% | When pattern detected |
| Trading Days | 242 | Full year 2024 backtesting |
| Patterns Cataloged | 15 | Market mechanics pattern library |

### Research Papers

| Paper | Title | Status |
|-------|-------|--------|
| Paper 1 | LLM Pattern Detection | Validated |
| Paper 2 | Regime Detection | In Progress |
| Paper 3 | Sector Rotation | Planned |

## Application Structure

### Core Pages

| Route | Page | Description |
|-------|------|-------------|
| `/` | Index | Module selection landing page |
| `/gex` | GEX Visualizer | Interactive gamma exposure analysis |
| `/arcade` | Research Arcade | Research visualization hub |

### Research Visualizations

Migrated from standalone HTML to Blazor components (Epic #100):

| Route | Visualization | Description |
|-------|---------------|-------------|
| `/research/pipeline` | Data Pipeline States | State diagram of data flow |
| `/research/paper2` | Structural Agreement Test | GEX formula validation over time |
| `/research/patterns` | Pattern Discovery Pipeline | KDD process visualization |
| `/research/complexity` | Research Complexity Map | 16 research paths by barrier type |

### Journal & Tracking

| Route | Feature | Description |
|-------|---------|-------------|
| `/paper-trading` | Paper Trading Journal | Trade logging with P&L tracking |
| `/backtests` | Backtest Results | Backtest comparison and analysis |
| `/notebook` | Research Notebook | Pattern annotations and notes |
| `/tasks` | Task Board | Local + GitHub Projects kanban |

## Technology Stack

- **.NET 10** / Blazor WebAssembly
- **CSS** scoped isolation with design system variables
- **LocalStorage** for client-side persistence
- **GitHub GraphQL API** for Projects v2 integration

## Deprecated Files

Legacy standalone HTML visualizations are preserved in [deprecated/research-visuals-html/](deprecated/research-visuals-html/) for reference. These have been superseded by the Blazor implementations.

## Related Links

- [GEX-LLM Patterns Repository](https://github.com/iAmGiG/gex-llm-patterns) - Research project source
- [Research Papers](https://github.com/iAmGiG/gex-llm-patterns/tree/main/docs/papers) - Academic documentation
