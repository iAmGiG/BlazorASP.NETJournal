# GexVisor.NET

An interactive Gamma Exposure (GEX) analysis tool for financial research,
rebuilt from the ground up with a modern, cross-platform .NET stack.

This is the next-generation version of the GEX Visualizer, migrating from a
JavaScript/Python implementation to a high-performance, self-contained application
using .NET MAUI and Blazor WebAssembly.

## About GEX Analysis

GexVisor analyzes Gamma Exposure (GEX) across options markets to provide
insights into market dynamics and potential volatility drivers. The tool
visualizes complex derivatives data through interactive charts and simulation
tools.

## Tech Stack

- **Framework:** .NET MAUI (cross-platform desktop/mobile)
- **UI:** Blazor WebAssembly (components and layouts)
- **Language:** C# (.NET 10.0 LTS)
- **Data Store:** Client-side SQLite (embedded database)
- **Charting:** SVG rendering via Blazor components

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** or later
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Git

### Installation

1. Clone the repository:

```bash
git clone https://github.com/iAmGiG/GexVisor.git
cd GexVisor
```

1. Restore dependencies:

```bash
dotnet restore
```

1. Build the project:

```bash
dotnet build
```

1. Run the application:

```bash
# Run Blazor WebAssembly app (UI only - uses static demo data)
dotnet run --project src/GexVisor.UI

# OR run the full application with API backend
dotnet run --project src/GexVisor.Api
```

The application will be available at `https://localhost:5001` (or the port shown in the console output).

## Project Structure

```
GexVisor/
├── src/                          # Source code
│   ├── GexVisor.UI/              # Blazor WebAssembly UI
│   ├── GexVisor.Core/            # Core models and business logic
│   └── GexVisor.Api/             # ASP.NET Core API backend
├── tests/                        # Unit and integration tests
│   ├── GexVisor.UI.Tests/        # UI component and service tests
│   └── GexVisor.Core.Tests/      # Core business logic tests
├── docs/                         # Documentation
│   ├── research-visuals/         # Interactive research visualizations
│   └── research-archive/         # Historical research materials
├── .claude/                      # Claude working notes (not committed)
├── .editorconfig                 # Code style settings
├── .pre-commit-config.yaml       # Git hooks configuration
├── GexVisor.NET.sln              # Solution file
├── CONTRIBUTING.md               # Development guide
└── README.md                     # This file
```

## Development

### Code Standards

This project enforces code quality through:

- **EditorConfig:** Consistent formatting across editors
  (.NET, Markdown, YAML, JSON)
- **Pre-commit hooks:** Automatic validation before commits
  (markdown, YAML, C#)
- **GitHub Actions:** CI pipeline (linting, build, code quality)

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed development instructions.

### Building and Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/GexVisor.Core.Tests      # Core business logic tests (17 tests)
dotnet test tests/GexVisor.UI.Tests        # UI component tests (44 tests)

# Build for release
dotnet build --configuration Release

# Watch mode for development
dotnet watch --project src/GexVisor.UI
```

**Test Coverage:**
- **Core Tests** (17): JournalFramework, OptionsLog P&L calculations
- **UI Tests** (44): Services, components, caching optimizations

### Git Workflow

Branch naming convention:

- `feature/description` - New features
- `fix/description` - Bug fixes
- `enhancement/description` - Improvements to existing features
- `research/description` - Research or experimental work

See [CONTRIBUTING.md](CONTRIBUTING.md) for commit message guidelines.

## Features

### Completed
- [x] Project infrastructure and CI/CD
- [x] .editorconfig and pre-commit hooks
- [x] .NET 10 LTS upgrade
- [x] GEX data models (GexDataPoint, GexTimeline, GexState)
- [x] GexStateService - reactive state management
- [x] GexDataService - JSON data loading
- [x] GexHeader component - metrics display
- [x] GexSidebar component - controls and parameters
- [x] PriceSparkline component - interactive price history
- [x] GexChart component - dual-mode SVG rendering with axis labels
- [x] Keyboard shortcuts (Space, arrows, Home/End, R, F, ?)
- [x] Real-time data simulation (auto-play timer)
- [x] Client-side data persistence (localStorage) (#44)
- [x] Keyboard shortcuts help overlay (#34)
- [x] Pattern annotations for LLM training data (#66)
- [x] Research Arcade page with research visualizations
- [x] Export Chart as PNG (#33)
- [x] Research Notebook - document observations and findings (#64)
- [x] Paper Trading Journal - track theoretical trades (#65)
- [x] Backtest Results Tracker with comparison (#67)
- [x] Research Task Board - Kanban-style task management (#68)
- [x] GitHub Projects integration - Kanban board sync (#69-73)
- [x] Accessibility improvements - ARIA labels (#35)
- [x] UI consistency - unified patterns across all pages
- [x] Mobile responsive layout (#32)
- [x] CSS migration to shared variables (#17)
- [x] GitVersion semantic versioning (#10)
- [x] Pattern validation progress tracker (#78)
- [x] Regime transition timeline (#79)
- [x] DateOnly type migration (#90)
- [x] OptionsLog P&L bug fix (#91)

### In Progress
- [ ] OptionsLog unit tests (#93)
- [ ] SQLite WASM integration (#18) - research complete, awaiting database

### Planned
- [ ] DateRange DateOnly refactoring (#94)
- [ ] StatusMapper pattern optimization (#95)
- [ ] Add SQLite database as static asset (#11)

## Research Visualizations

GexVisor includes interactive research visualizations showcasing the analysis tools
and methodologies. Access them at `/docs/research-visuals/`.

## Contributing

Contributions welcome! Please review [CONTRIBUTING.md](CONTRIBUTING.md) for:

- Development setup
- Code standards and naming conventions
- Pre-commit hooks and GitHub Actions workflow
- Testing requirements
- Commit message guidelines
- Pull request process

## Project Status

**Phase:** Active Development - Infrastructure

The GEX Visualizer and all research journal features are complete:
- **GEX Visualizer** (`/gex`) - Interactive gamma exposure visualization
- **Research Arcade** (`/arcade`) - Research visualizations hub
- **Research Notebook** (`/notebook`) - Document observations and hypotheses
- **Paper Trading** (`/trading`) - Log theoretical trades with P&L tracking
- **Backtest Results** (`/backtests`) - Compare strategy performance
- **Research Tasks** (`/tasks`) - Kanban-style task board

Currently working on SQLite WASM integration for persistent data storage.

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Questions?

Review the [CONTRIBUTING.md](CONTRIBUTING.md) guide or check existing issues for
discussion threads.
