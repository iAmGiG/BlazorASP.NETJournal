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
| [github-integration.md](github-integration.md) | GitHub Projects v2 API integration for task board |
| [state-diagrams.md](state-diagrams.md) | State machines and data flow diagrams |
| [research-roadmap.md](research-roadmap.md) | Multi-phase research trajectory (Phases 1-5) |
| [adr/](adr/) | Architecture Decision Records |

## Application Routes

| Route | Description |
|-------|-------------|
| `/` | Home - module selection |
| `/gex` | GEX Visualizer - interactive gamma exposure analysis |
| `/arcade` | Research Arcade - visualization hub |
| `/research/pipeline` | Data pipeline state diagram |
| `/research/patterns` | Pattern discovery (KDD process) |
| `/research/complexity` | Research complexity map |
| `/research/paper2` | Structural agreement test |
| `/paper-trading` | Paper trading journal |
| `/backtests` | Backtest results tracker |
| `/notebook` | Research notebook |
| `/tasks` | Task board (local + GitHub) |

## Research Context

GexVisor supports the [GEX-LLM Patterns](https://github.com/iAmGiG/gex-llm-patterns)
research project.

| Metric | Value |
|--------|-------|
| Detection Rate | 71.5% |
| Predictive Accuracy | 91.2% |
| Trading Days Analyzed | 242 |

## Technology Stack

- .NET 10 / Blazor WebAssembly
- CSS scoped isolation
- LocalStorage for persistence
- GitHub GraphQL API

## Deprecated

Legacy HTML visualizations preserved in [deprecated/](deprecated/) for reference.
