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
- Tooltip appears showing short label (e.g., "Monte Carlo")

### Clicking

**Action:** Click a research node

**Effect:**
- Opens **RadarModal** with detailed information:
  - Full title and description
  - Barrier explanation (why this is blocked)
  - Unblock requirements (what's needed to proceed)
  - Related research paths (links to connected work)
  - Status badge and taxonomy tag
  - GitHub issue link (if applicable)

**Close Modal:** Click the `×` button or outside the modal

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

Nodes are positioned using polar coordinates:

```
x = radius * cos(angle - 90°)
y = radius * sin(angle - 90°)
```

Where:
- `radius` = `RingRadii[path.Ring]` (0, 55, 105, 155, 210, 265, 315)
- `angle` = `path.Angle` in degrees

### CSS Classes

| Class | Purpose |
|-------|---------|
| `.node-ring-{0-5}` | Animation delay by ring (0.08s * ring) |
| `.center-pulse` | 2.5s infinite breathing animation |
| `.research-node.selected` | Blue outline + shadow |
| `.research-node.related-glow` | Blue glow for connected paths |
| `.research-node.dimmed` | Opacity 0.3 for filtered-out nodes |
| `.quadrant-bg` | Semi-transparent quadrant fill |
| `.ring-circle` | Concentric ring boundaries |

## Data Model

### ResearchPath Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique identifier (e.g., "montecarlo") |
| `Ring` | int | Complexity level (0-5) |
| `Quadrant` | ResearchQuadrant | Barrier type sector |
| `Angle` | int | Position within quadrant (degrees) |
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

## Future Enhancements

**Planned (not yet implemented):**
- Animated connection lines between related nodes
- MiniRadar component for cards/previews
- Export to PNG/SVG for presentations
- Historical view (show radar state over time as research progressed)

## Related Documentation

- [Architecture](architecture.md) - Research Visualization components technical details
- [Research Roadmap](research-roadmap.md) - PhD research timeline and phases
- [GitHub Issue #175](https://github.com/iAmGiG/GexVisor/issues/175) - ResearchPath enum migration

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Esc` | Close RadarModal |
| Click outside | Close RadarModal |

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

**Issue:** Modal not opening on click

**Solution:**
- Ensure JavaScript is enabled
- Check browser console for errors
- Try clicking the node center (larger hit area)

---

_Last Updated: 2026-01-24_
