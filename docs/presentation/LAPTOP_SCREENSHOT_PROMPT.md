# Prompt for Claude Code on Laptop

Copy-paste this into Claude Code on the machine where you can run `dotnet`:

---

I need you to capture screenshots of my running GexVisor Blazor WebAssembly app for a poster. Here's the plan:

## Setup

1. Install Playwright for Node.js if not already installed:
   ```
   npm install playwright
   npx playwright install chromium
   ```

2. Start the GexVisor app in background:
   ```
   dotnet run --project src/GexVisor.UI &
   ```
   Wait for it to be listening on http://localhost:5246

## Capture Screenshots

Use Playwright (Node.js) to capture 1920x1080 screenshots at 2x device scale (retina quality for poster printing). Save all to `docs/presentation/screenshots/`.

Pages to capture:

| Route | Filename | What it shows |
|-------|----------|---------------|
| `/gex` | gex_visualizer.png | Main GEX chart with bars, regime timeline, sidebar |
| `/notebook` | research_notebook.png | KDD step cards, complexity radar |
| `/trading` | paper_trading.png | Trade grid, decision timeline, regime context |
| `/arcade` | research_arcade.png | Pattern discovery tools |
| `/tasks` | task_board.png | GitHub Kanban board with cards |
| `/backtests` | backtest_tracker.png | Backtest results / strategy comparison |
| `/research/patterns` | pattern_discovery.png | Pattern validation tracker |
| `/research/pipeline` | data_pipeline.png | Data pipeline state visualization |
| `/research/complexity` | complexity_map.png | Research complexity map / radar |
| `/comparison` | comparison_dashboard.png | Correlation matrix, regime divergence |
| `/` | home.png | Landing page |

Important notes:
- The app is Blazor WebAssembly — it takes a few seconds to hydrate. Wait at least 3 seconds on `/gex` (has charts) and 2 seconds on other pages after `networkidle` before screenshotting.
- Use `fullPage: false` — we want viewport-sized captures.
- Use `deviceScaleFactor: 2` for print quality.

There's already a script at `docs/presentation/capture_screenshots.js` you can use or modify. Just run:
```
node docs/presentation/capture_screenshots.js
```

After capturing, also take a screenshot of a GitHub Actions CI run if you can access it:
- Go to https://github.com/WormsCanned/GexVisor/actions and screenshot the most recent successful run showing the lint/build/test/code-quality jobs.

Finally, list which screenshots were captured and their file sizes so I know what we got.
