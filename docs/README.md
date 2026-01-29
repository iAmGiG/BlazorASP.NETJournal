# GexVisor Documentation

GexVisor is a Blazor WebAssembly application for exploring GEX (Gamma Exposure)
regime dynamics with interactive visualizations and research tools.

## Getting Started

New to GexVisor? Start here:

1. **[Getting Started](getting-started.md)** - Build, run, and develop locally

## Documentation

| Document | Description |
|----------|-------------|
| [getting-started.md](getting-started.md) | Prerequisites, build, run, and development workflow |
| [api-setup.md](api-setup.md) | API backend setup: configuration, endpoints, CORS, troubleshooting |
| [linting.md](linting.md) | Code linting setup: EditorConfig, analyzers, pre-commit hooks |
| [architecture.md](architecture.md) | Service inventory, state patterns, data flow |
| [live-data.md](live-data.md) | Live GEX data: setup, usage, and troubleshooting |
| [chart-viewer.md](chart-viewer.md) | Chart Viewer: candlestick charts and trade markers |
| [research-visualization.md](research-visualization.md) | Research radar: interactive complexity map with quadrants/rings |
| [diagrams.md](diagrams.md) | Mermaid diagrams (renders in GitHub) |
| [state-diagrams.md](state-diagrams.md) | ASCII state machines (universal) |
| [github-integration.md](github-integration.md) | GitHub Projects v2 API integration for task board |
| [paper-trading.md](paper-trading.md) | Paper trading module and import formats |
| [research-roadmap.md](research-roadmap.md) | Multi-phase research trajectory (Phases 1-5) |
| [adr/](adr/) | Architecture Decision Records |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Documentation standards and development workflow |

## Application Routes

| Route | Description | Environment |
|-------|-------------|-------------|
| `/` | Home - module selection | Production |
| `/gex` | GEX Visualizer - interactive gamma exposure analysis | Production |
| `/arcade` | Research Arcade - gateway to research visualizations | Production |
| `/research/pipeline` | Data pipeline state diagram | Production |
| `/research/patterns` | Pattern discovery (KDD process) | Production |
| `/research/complexity` | Research complexity map | Production |
| `/research/paper2` | Structural agreement test | Production |
| `/trading` | Paper trading simulator | Production |
| `/tradelogging` | Trade Journal - autotrader log import and analysis | Production |
| `/comparison` | Comparison Dashboard - multi-asset regime analysis | Production |
| `/backtests` | Backtest results tracker | Production |
| `/notebook` | Research notebook | Production |
| `/tasks` | Task board (local + GitHub) | Production |
| `/chart-test` | Chart library test sandbox | Development Only |

**Note:** Research visualizations are accessible via both:

- `/arcade` - Arcade-style visualization launcher
- `/research/*` - Direct routes to individual visualizations

## Research Context

GexVisor supports the [GEX-LLM Patterns](https://github.com/iAmGiG/gex-llm-patterns)
research project.

| Metric | Value |
|--------|-------|
| Detection Rate | 71.5% (Phase 1, 2024) |
| Predictive Accuracy | 91.2% (Phase 1, 2024) |
| Trading Days Analyzed | 242 (Full year 2024) |
| Source | arXiv:2512.17923 |

## Technology Stack

- .NET 10 / Blazor WebAssembly
- CSS scoped isolation
- LocalStorage for persistence
- GitHub GraphQL API

## Deprecated

Legacy HTML visualizations preserved in [deprecated/](deprecated/) for reference.
