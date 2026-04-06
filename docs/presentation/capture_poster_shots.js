/**
 * Interactive screenshot capture for C-Day poster.
 * Navigates the GEX Visualizer through different states and captures
 * multiple shots at different simulation positions.
 *
 * Usage:
 *   1. Start GexVisor:  dotnet run --project src/GexVisor.UI
 *   2. Run this script:  node docs/presentation/capture_poster_shots.js
 */

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:5246';
const OUT_DIR = path.join(__dirname, 'screenshots');

(async () => {
    fs.mkdirSync(OUT_DIR, { recursive: true });

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({
        viewport: { width: 1920, height: 1080 },
        deviceScaleFactor: 2,
    });

    const page = await context.newPage();

    // ── GEX Visualizer: multiple positions ──────────────────────────

    console.log('Navigating to GEX Visualizer...');
    await page.goto(`${BASE_URL}/gex`, { waitUntil: 'load', timeout: 60000 });
    await page.waitForTimeout(15000); // Blazor WASM hydration + chart render

    // Focus the app container for keyboard events
    await page.click('[tabindex="0"]');
    await page.waitForTimeout(500);

    // Step forward ~15 times to get to an interesting position
    console.log('  Stepping forward through simulation...');
    for (let i = 0; i < 15; i++) {
        await page.keyboard.press('ArrowRight');
        await page.waitForTimeout(300);
    }
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(OUT_DIR, 'gex_mid_position.png'), fullPage: false });
    console.log('  -> gex_mid_position.png');

    // Step forward more
    for (let i = 0; i < 20; i++) {
        await page.keyboard.press('ArrowRight');
        await page.waitForTimeout(300);
    }
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(OUT_DIR, 'gex_advanced_position.png'), fullPage: false });
    console.log('  -> gex_advanced_position.png');

    // Jump to next year
    await page.keyboard.press('ArrowUp');
    await page.waitForTimeout(2000);
    // Step forward a bit in the new year
    for (let i = 0; i < 10; i++) {
        await page.keyboard.press('ArrowRight');
        await page.waitForTimeout(300);
    }
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(OUT_DIR, 'gex_different_year.png'), fullPage: false });
    console.log('  -> gex_different_year.png');

    // Go back to start and use play/pause for a running simulation look
    await page.keyboard.press('Home');
    await page.waitForTimeout(1000);
    // Play simulation
    await page.keyboard.press(' ');
    await page.waitForTimeout(3000); // Let it run for 3 seconds
    // Pause
    await page.keyboard.press(' ');
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(OUT_DIR, 'gex_simulation_running.png'), fullPage: false });
    console.log('  -> gex_simulation_running.png');

    // ── Complexity Map: ensure it loads fully ────────────────────────
    console.log('Navigating to Complexity Map...');
    await page.goto(`${BASE_URL}/research/complexity`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(4000); // Extra wait for radar SVG rendering
    await page.screenshot({ path: path.join(OUT_DIR, 'complexity_map.png'), fullPage: false });
    console.log('  -> complexity_map.png (retake)');

    // ── Research Arcade ──────────────────────────────────────────────
    console.log('Navigating to Research Arcade...');
    await page.goto(`${BASE_URL}/arcade`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(OUT_DIR, 'research_arcade.png'), fullPage: false });
    console.log('  -> research_arcade.png (retake)');

    // ── Pattern Discovery ────────────────────────────────────────────
    console.log('Navigating to Pattern Discovery...');
    await page.goto(`${BASE_URL}/research/patterns`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(OUT_DIR, 'pattern_discovery.png'), fullPage: false });
    console.log('  -> pattern_discovery.png (retake)');

    // ── Home ─────────────────────────────────────────────────────────
    console.log('Navigating to Home...');
    await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(OUT_DIR, 'home.png'), fullPage: false });
    console.log('  -> home.png (retake)');

    // ── Comparison Dashboard ─────────────────────────────────────────
    console.log('Navigating to Comparison Dashboard...');
    await page.goto(`${BASE_URL}/comparison`, { waitUntil: 'networkidle', timeout: 15000 });
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(OUT_DIR, 'comparison_dashboard.png'), fullPage: false });
    console.log('  -> comparison_dashboard.png (retake)');

    await browser.close();
    console.log('\nDone! Poster screenshots ready.');
})();
