# Research Visualization Guide

The Research Visualization feature provides an interactive radar-style map of research paths, displaying the scope and complexity of GEX analysis research. This guide explains how to interpret and interact with the radar.

## Overview

The research radar visualizes 16 research paths organized by **complexity level** (rings) and **barrier type** (quadrants). Each node represents a distinct research direction, color-coded by implementation status.

**Access the radar at:** `/research/complexity`

## Radar Structure

### Rings (Complexity Levels)

The radar has 6 concentric rings representing increasing complexity:

| Ring | Name | Description |
|------|------|-------------|
| **Ring 0** | Validated Core | Foundation achieving 71.5% detection, 91.2% accuracy |
| **Ring 1** | Minor Extensions | Simple improvements requiring <1 week effort |
| **Ring 2** | Statistical Expertise | Advanced statistical methods (CPCV, FDR, Monte Carlo) |
| **Ring 3** | Infrastructure Required | Multi-agent systems, backtesting frameworks |
| **Ring 4** | Expensive/Rare Data | High-frequency data, alternative datasets (sentiment, macro) |
| **Ring 5** | Theoretical Barriers | Unsolved problems (multi-period, stochastic control) |

**Distance from center = complexity**. The core (Ring 0) contains the validated GEX analysis system. Outer rings represent increasingly challenging research directions.

### Quadrants (Barrier Types)

Four quadrants represent the primary obstacle preventing implementation:

| Quadrant | Color | Barrier Type | Examples |
|----------|-------|--------------|----------|
| **DATA ACCESS** | Red | Missing or expensive data sources | High-freq options data, sentiment feeds |
| **DOMAIN KNOWLEDGE** | Purple | Requires specialized expertise | Statistical methods, quant finance theory |
| **SCOPE / FOCUS** | Blue | Outside PhD research scope | Production trading, real-time execution |
| **METHODOLOGY** | Orange | Requires new techniques or frameworks | Multi-agent orchestration, backtesting |
| **COMPUTE** | Green | Computationally expensive | 10K+ Monte Carlo iterations, parallel sims |

### Status Colors

Each node is color-coded by implementation status:

| Status | Color | Meaning |
|--------|-------|---------|
| **Implemented** | Green | Fully complete and validated |
| **Partial** | Yellow | Started but incomplete |
| **Deferred** | Blue | Postponed to future work |
| **Abandoned** | Purple | Explored and rejected |
| **Blocked** | Orange | Cannot proceed without external resource |
| **Infeasible** | Red | Theoretically or practically impossible |
| **Superseded** | Teal | Replaced by better approach |

## Interacting with the Radar

### Hovering

**Action:** Move mouse over a node

**Effect:**

- Node glows with `brightness(1.25)` and drop shadow
- Related nodes highlight with blue glow
- **Connection lines appear** showing dashed curves to related research paths
- Tooltip appears showing short label (e.g., "Monte Carlo")

### Clicking

**Action:** Click a research node

**Effect (depends on view mode):**

**Radar View:**
- Selects the node (highlights with glow)
- **Connection lines remain visible** (dashed curves to related paths)
- Shows **details panel below the radar** with:
  - Full title and description
  - Barrier explanation (why this is blocked)
  - Unblock requirements (what's needed to proceed)
  - Related research paths (clickable tags)
  - Status badge and taxonomy tag
  - GitHub issue link (if applicable)
- No blocking modal - radar stays fully interactive
- **Close selection:** Click the `×` button or press `Escape`

**Cards View:**
- Opens **RadarModal** popup with detailed information
- Modal can be closed by clicking `×`, pressing `Escape`, or clicking outside
- **Navigate Between Related Concepts:** Click tags to seamlessly navigate between paths
- **Navigation History:** Click "Previous" button (or press `Backspace`) to return to previously viewed paths
- History is maintained as long as the modal stays open

### Filtering

**Filter Panel** (right sidebar):

1. **By Status:**
   - Toggle checkboxes to show/hide statuses
   - Example: Show only `Implemented` + `Partial` to see current work
   - Hidden nodes are dimmed (opacity 0.3)

2. **By Taxonomy:**
   - **Mechanical**: Rule-based, deterministic approaches
   - **Probabilistic**: Statistical, confidence-based methods
   - **Narrative**: Context-driven, interpretive analysis

3. **By Quadrant:**
   - Filter to specific barrier types
   - Example: Show only `Data Access` paths to understand data requirements

4. **Reset Filters:** Click "Reset All" to show all paths

### Animation

**On page load:**

- Nodes animate in with cascading effect (0.08s delay per ring)
- Ring 0 (core) appears first, then Ring 1, etc.
- Center section pulses with infinite breathing animation (2.5s cycle)

**Hover effects:**

- Smooth glow transitions
- Related nodes light up automatically
- Cursor changes to pointer on interactive elements

## Understanding Research Paths

### Example: Monte Carlo & Permutation Testing

**Location:** Ring 2 (Statistical Expertise), Compute quadrant

**Status:** Partial (Yellow)

**Taxonomy:** Mechanical

**Description:**
> Basic stats completed (Wilson CI, Sharpe ratio, Kelly Criterion). Missing: 10K+ permutation iterations.

**Barrier:**
> Massive compute for 10K+ iterations; PhD timeline pressure made basic validation sufficient

**Unblock:**
> Cloud compute budget for large-scale permutation testing

**Related:** Statistical Significance, FDR Correction, Regime Testing

**Interpretation:**

- Complexity: Ring 2 = requires statistical expertise
- Barrier: Compute-intensive (green quadrant)
- Status: Started but incomplete (yellow)
- Next steps: Cloud compute resources needed

### Example: Market Regime Detection

**Location:** Ring 3 (Infrastructure Required), Methodology quadrant

**Status:** Deferred (Blue)

**Barrier:**
> Requires multi-agent orchestration framework with state machines - infrastructure overhead for single-agent validation

**Interpretation:**

- Would require building agent coordination system
- Outside scope of current single-agent approach
- Postponed to future work (after PhD)

## Practical Use Cases

### 1. Understanding Research Scope

**Goal:** See what's been implemented vs. what's planned

**Steps:**

1. Filter by `Implemented` status only
2. Observe Ring 0 (core) + partial Ring 1/2 nodes
3. Recognize validated foundation vs. exploratory work

### 2. Identifying Data Requirements

**Goal:** Understand what data sources are needed

**Steps:**

1. Filter by `Data Access` quadrant (red sector)
2. Review barrier descriptions for each node
3. Example findings:
   - High-frequency options data (Ring 4)
   - Sentiment data feeds (Ring 4)
   - Alternative data sources (Ring 4)

### 3. Planning Future Work

**Goal:** Prioritize next research directions

**Steps:**

1. Filter by `Partial` status (yellow)
2. Review Ring 1-2 paths (lower complexity)
3. Check unblock requirements
4. Example candidates:
   - Six-Category Pattern Classification (Ring 1) - needs validation sprints
   - Dynamic Trailing Stops (Ring 1) - needs production trading system

### 4. Explaining Abandoned Paths

**Goal:** Understand why certain approaches were rejected

**Steps:**

1. Filter by `Abandoned` status (purple)
2. Read barrier + description for rationale
3. Example: Multi-Period Optimization (Ring 5) - theoretically intractable

## Technical Details

### Quadrant Angles

Quadrants are defined in degrees (clockwise from top):

| Quadrant | Start | End |
|----------|-------|-----|
| Data Access | 0° | 90° |
| Domain Knowledge | 90° | 180° |
| Scope/Focus | 180° | 270° |
| Methodology | 270° | 360° |

### Node Positioning

Nodes are positioned using polar coordinates within their quadrant:

```
normalizedPosition = clamp(path.Angle / 90, 0, 1)
normalizedAngle = quadrantStart + normalizedPosition * quadrantRange
x = radius * cos(normalizedAngle - 90°)
y = radius * sin(normalizedAngle - 90°)
```

Where:

- `radius` = midpoint between `RingRadii[path.Ring]` and `RingRadii[path.Ring + 1]`
- `RingRadii` = [0, 55, 105, 155, 210, 265, 315]
- `path.Angle` = 0-90 representing position within quadrant (0% to 100%)
- Quadrant ranges: Data (0-90°), Knowledge (90-180°), Scope (180-270°), Methodology (270-315°), Compute (315-360°)

### CSS Classes

| Class | Purpose |
|-------|---------|
| `.node-ring-{0-5}` | Animation delay by ring (0.08s * ring) |
| `.center-pulse` | 2.5s infinite breathing animation |
| `.research-node.selected` | Blue outline + shadow |
| `.research-node.related-glow` | Blue glow for connected paths |
| `.research-node.dimmed` | Opacity 0.15 for filtered/unfocused nodes |
| `.quadrant-bg` | Semi-transparent quadrant fill |
| `.ring-circle` | Concentric ring boundaries |
| `.grid-line` | Subtle background grid pattern (5% opacity) |
| `.grid-bg` | Background rect with grid pattern fill |
| `.radar-btn.active` | Cyan highlight for active toggle buttons |
| `.connection-line` | Dashed curves connecting related nodes |
| `.radar-details-below` | Details panel styling (below radar in radar-layout) |
| `.cards-layout` | 2-column grid layout for Cards view (filters + cards) |
| `.radar-layout` | 2-column grid layout for Radar view (filters + radar) |

## Data Model

### ResearchPath Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique identifier (e.g., "montecarlo") |
| `Ring` | int | Complexity level (0-5) |
| `Quadrant` | ResearchQuadrant | Barrier type sector |
| `Angle` | int | Position within quadrant (0-90, representing 0-100%) |
| `Label` | string | Short display name (e.g., "Monte") |
| `LabelLong` | string? | Second line label (e.g., "Carlo") |
| `Status` | ResearchStatus | Implementation state |
| `Taxonomy` | ResearchTaxonomy | Approach classification |
| `Title` | string | Full research path name |
| `Description` | string | What this path involves |
| `Barrier` | string | Why this is blocked/deferred |
| `Unblock` | string? | What's needed to proceed |
| `BarrierQuadrant` | ResearchQuadrant | Which quadrant contains the blocker |
| `Related` | List<string> | IDs of connected paths |
| `Paper` | string? | Associated research paper |
| `IssueUrl` | string? | GitHub issue link |
| `IssueNum` | string? | GitHub issue number (#46) |

### Enums

**ResearchStatus:**

- Implemented, Partial, Deferred, Abandoned, Blocked, Infeasible, Superseded

**ResearchTaxonomy:**

- Mechanical, Probabilistic, Narrative

**ResearchQuadrant:**

- Data, Knowledge, Scope, Methodology, Compute, None

## Foundation Statistics

The validated core (Ring 0) achieved:

- **Detection Rate:** 71.5% (pattern recognition accuracy)
- **Accuracy:** 91.2% (directional prediction accuracy)
- **Trading Days:** 242 (test period duration)

These metrics represent the baseline performance of the GEX analysis system.

## Data Loading

Research path data is loaded from JSON via `IResearchPathService`:

**Data Source:** `wwwroot/data/research-paths.json`

**Service Interface:**

```csharp
public interface IResearchPathService
{
    ResearchPath[] Paths { get; }
    Task<ResearchPath[]> LoadPathsAsync();
    Task<ResearchPath?> GetPathByIdAsync(string id);
}
```

**Usage in Components:**

```razor
@inject IResearchPathService ResearchPathService

@code {
    private ResearchPath[] _paths = [];

    protected override async Task OnInitializedAsync()
    {
        _paths = await ResearchPathService.LoadPathsAsync();
    }
}
```

**Benefits:**

- Content updates without recompilation
- Caching after first load
- Consistent data across all radar components

## Recent Enhancements

**Implemented (2026-01-29):**

- **Radar as Default View**: Radar view is now the default landing experience for `/research/complexity`
- **Connection Lines on Hover**: Dashed curves now appear when hovering over a node, showing related research paths
- **View-Specific UX**: Radar view shows details below the radar (keeps connection lines visible); Cards view uses modal popup only
- **Cards View Simplified**: Removed redundant details panel from Cards view - modal provides all information, giving cards more room
- **Larger Radar Size**: Increased from 650px to 800px for better visibility
- **Label Animation Timing**: Labels now fade in after nodes finish entrance animation (0.6s delay)
- **Layout Restructure**: Both views now use 2-column layout (filters + content); switching views clears selection to prevent unintended modal popups

**Implemented (2026-01-27):**

- **Focus Mode**: Toggle to dim unrelated nodes, highlighting only selected path and its connections
- **Fit to Screen**: Auto-zoom to fit all visible/filtered nodes with optimal padding
- **Grid Background**: Subtle grid pattern provides spatial reference during pan/zoom

**Implemented (2026-01-25):**

- **Zoom and Pan**: Scroll to zoom (0.5x-10x), drag to pan, R key to reset
- **In-Modal Navigation**: Click related concept tags to navigate between research paths without closing the modal
- **Focus Preservation**: Modal automatically refocuses when content changes, maintaining keyboard accessibility
- **Navigation History**: Back button with keyboard shortcut (`Backspace`) to return to previously viewed paths

## Future Enhancements

**Planned (not yet implemented):**

- Export to PNG/SVG for presentations
- Historical view (show radar state over time as research progressed)
- Visual breadcrumb trail showing navigation path in modal header

## Related Documentation

- [Architecture](architecture.md) - Research Visualization components technical details
- [Research Roadmap](research-roadmap.md) - PhD research timeline and phases
- [GitHub Issue #175](https://github.com/iAmGiG/GexVisor/issues/175) - ResearchPath enum migration

## Zoom and Pan

The radar supports interactive zoom and pan for exploring dense node clusters:

**Zoom:**

- **Scroll wheel**: Zoom in/out centered on current view
- **Zoom range**: 0.5x (zoomed out) to 10x (zoomed in)

**Pan:**

- **Click and drag**: Pan the radar view in any direction
- Works at any zoom level

**Reset:**

- **R key**: Reset zoom and pan to default view
- **Reset button**: Click the button in the top-right corner

**Fit to Screen:**

- **F key**: Auto-zoom and center to fit all visible/filtered nodes
- **Fit button**: Click the Fit button in the top-right corner
- Uses filtered paths when filters are active, otherwise all paths
- Adds padding around bounds for comfortable viewing

## Focus Mode

Focus Mode dims unrelated nodes to help concentrate on a selected research path
and its connections.

**Toggle:**

- Click the **Focus** button in the top-right corner
- Button highlights cyan when active

**Behavior:**

- When a node is selected, only related nodes remain fully visible
- Unrelated nodes dim to 15% opacity
- Relationship is determined by the `Related` field in research path data
- Combine with filters for even more targeted exploration

**Use Case:** Select "Monte Carlo" node, enable Focus Mode, and only see
connected paths like "Statistical Significance" and "FDR Correction".

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Tab` | Focus radar container |
| `←` / `→` | Navigate between nodes |
| `↑` / `↓` | Navigate between nodes |
| `Enter` / `Space` | Select focused node |
| `Esc` | Close RadarModal / Clear selection |
| `Backspace` | Navigate back to previous research path (when modal is open) |
| `R` | Reset zoom and pan to default |
| `F` | Fit all visible nodes to screen |
| `Scroll` | Zoom in/out |
| `Drag` | Pan the radar view |
| Click outside | Close RadarModal |

## Accessibility (WCAG 2.1)

The radar visualization implements several accessibility features:

### Screen Reader Support

- **ARIA Live Region**: Filter changes are announced (e.g., "Showing 5 research paths. Filter: status Partial.")
- **Node Descriptions**: Each node has `role="button"`, `aria-label`, and detailed `aria-describedby` text
- **Modal Semantics**: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` for proper modal announcement

### Keyboard Navigation

- **Skip Links**: Press Tab on page load to reveal "Skip to filters" and "Skip to content" links
- **Focus Indicators**: Cyan outline on focused elements (`:focus-visible`)
- **Arrow Key Navigation**: Navigate nodes without mouse

### Reduced Motion

Users with `prefers-reduced-motion: reduce` enabled will see:

- No entrance animations on nodes
- No pulse animation on center
- Instant transitions instead of smooth animations

### Focus Management

- Modal auto-focuses when opened
- Modal refocuses when navigating between related concepts (maintains keyboard accessibility)
- Escape key closes modal from anywhere within it
- Focus returns to triggering element on close

## Troubleshooting

**Issue:** Nodes not appearing

**Solution:**

- Check filter settings (might be hiding all nodes)
- Click "Reset All" filters
- Verify browser supports SVG rendering

**Issue:** Animation stuttering

**Solution:**

- Reduce browser window size (smaller SVG viewBox)
- Disable browser extensions that interfere with CSS animations
- Close other tabs to free GPU resources

**Issue:** Modal not opening on click (Cards view only)

**Solution:**

- Ensure JavaScript is enabled
- Check browser console for errors
- Try clicking the node center (larger hit area)
- Note: In Radar view, clicking shows details below instead of modal

**Issue:** Connection lines not appearing on hover

**Solution:**

- Verify the `HoveredPath` parameter is passed to `ConnectionLines` component
- Check that the node has `Related` tags in `research-paths.json`
- Ensure CSS for `.connection-line.visible` is loaded

---

_Last Updated: 2026-01-29_
