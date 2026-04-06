/**
 * Automated screenshot capture for C-Day poster.
 *
 * Prerequisites (run once on your dev machine):
 *   npm install playwright
 *   npx playwright install chromium
 *
 * Usage:
 *   1. Start GexVisor:  dotnet run --project src/GexVisor.UI
 *   2. Run this script:  node docs/presentation/capture_screenshots.js
 *
 * Screenshots land in docs/presentation/screenshots/
 */

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:5246';
const OUT_DIR = path.join(__dirname, 'screenshots');

// Pages to capture — mapped to poster placeholder labels
const PAGES = [
    { route: '/gex',                   name: 'gex_visualizer',      title: 'GEX Visualizer',        wait: 3000 },
    { route: '/notebook',              name: 'research_notebook',   title: 'Research Notebook',      wait: 2000 },
    { route: '/trading',               name: 'paper_trading',       title: 'Paper Trading Journal',  wait: 2000 },
    { route: '/arcade',                name: 'research_arcade',     title: 'Research Arcade',        wait: 2000 },
    { route: '/tasks',                 name: 'task_board',          title: 'Task Board',             wait: 2000 },
    { route: '/backtests',             name: 'backtest_tracker',    title: 'Backtest Tracker',       wait: 2000 },
    { route: '/research/patterns',     name: 'pattern_discovery',   title: 'Pattern Discovery',      wait: 2000 },
    { route: '/research/pipeline',     name: 'data_pipeline',       title: 'Data Pipeline State',    wait: 2000 },
    { route: '/research/complexity',   name: 'complexity_map',      title: 'Research Complexity Map', wait: 2000 },
    { route: '/comparison',            name: 'comparison_dashboard',title: 'Comparison Dashboard',   wait: 2000 },
    { route: '/',                      name: 'home',                title: 'Home / Index',           wait: 2000 },
];

(async () => {
    fs.mkdirSync(OUT_DIR, { recursive: true });

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({
        viewport: { width: 1920, height: 1080 },
        deviceScaleFactor: 2,  // Retina-quality for poster printing
    });

    const page = await context.newPage();

    for (const pg of PAGES) {
        const url = `${BASE_URL}${pg.route}`;
        console.log(`Capturing: ${pg.title} (${url})`);

        try {
            await page.goto(url, { waitUntil: 'networkidle', timeout: 15000 });
            // Extra wait for Blazor WASM hydration + chart rendering
            await page.waitForTimeout(pg.wait);

            const filePath = path.join(OUT_DIR, `${pg.name}.png`);
            await page.screenshot({ path: filePath, fullPage: false });
            console.log(`  -> ${filePath}`);
        } catch (err) {
            console.error(`  FAILED: ${err.message}`);
        }
    }

    await browser.close();
    console.log(`\nDone! ${PAGES.length} screenshots in ${OUT_DIR}`);
})();
