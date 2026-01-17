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
dotnet run
```

## Project Structure

```
GexVisor/
├── src/                          # Source code
│   ├── GexVisor.UI/              # Blazor WebAssembly UI
│   └── GexVisor.Core/            # Core models and business logic
├── docs/                         # Documentation
│   ├── research-visuals/         # Interactive research visualizations
│   └── research-archive/         # Historical research materials
├── tests/                        # Unit and integration tests
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
# Run tests
dotnet test

# Run specific test file
dotnet test tests/MyProject.Tests.csproj

# Build for release
dotnet build --configuration Release
```

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

### In Progress
- [ ] Research Lab Notebook (#64) - shared journal infrastructure
- [ ] Paper Trade Simulator (#65)

### Planned
- [ ] Export Chart as PNG (#33)
- [ ] Backtest Results Tracker (#67)
- [ ] Research Task Board (#68)
- [ ] Wasm-compatible SQLite research (#18)

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

**Phase:** Active Development - Research Tools

The GEX Visualizer core is complete with chart rendering, timeline simulation,
and pattern annotations. Currently building research journal infrastructure
to support notebook entries, paper trading, and backtest tracking. Access the
GEX Visualizer at `/gex` and Research Arcade at `/arcade`.

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Questions?

Review the [CONTRIBUTING.md](CONTRIBUTING.md) guide or check existing issues for
discussion threads.
