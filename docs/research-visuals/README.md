# Research Complexity Map - Visualization Documentation

A self-contained interactive visualization mapping abandoned and deferred
research paths by barrier type, complexity, and status.

## Overview

This visualization displays research exploration paths from the GEX-LLM
Patterns project, organized as a radial complexity map. The center
represents validated, accessible research; outer rings represent
increasingly difficult research requiring specialized data, knowledge, or
infrastructure.

**Live Context:** This visualization was created to communicate research
scope decisions for PhD dissertation work on LLM-based market mechanics
pattern detection.

## Source Project

- **Repository:** [iAmGiG/gex-llm-patterns](https://github.com/iAmGiG/gex-llm-patterns)
- **Original Location:** `docs/reference/research_complexity_map.html`
- **Source Document:** `docs/reference/abandoned_research_paths.md`

## Key Metrics Displayed

| Metric            | Value  | Source                       |
| ----------------- | ------ | ---------------------------- |
| Detection Rate    | 71.5%  | Unbiased prompts validation  |
| Predictive Accuracy | 91.2% | When pattern detected        |
| Trading Days      | 242    | Full year 2024 backtesting   |
| Patterns Cataloged | 15     | Market mechanics pattern lib |
| Research Paths    | 16     | Explored/abandoned paths     |

## Visualization Structure

### Concentric Rings (Complexity Levels)

| Ring | Name                   | Description                    |
| ---- | ---------------------- | ------------------------------ |
| 0    | Validated Core         | Currently implemented/validated |
| 1    | Minor Extensions       | Small additions to sys         |
| 2    | Statistical Expertise  | Advanced statistical methods   |
| 3    | Infrastructure Req.    | Significant engineering work   |
| 4    | Expensive/Rare Data    | Costly data sources ($10K+/yr) |
| 5    | Theoretical Barriers   | Fundamental feasibility issue  |

### Quadrants (Barrier Types)

| Quadrant          | Angle    | Color  | Description                |
| ----------------- | -------- | ------ | -------------------------- |
| DATA ACCESS       | 0-90     | Red    | Data availability/cost     |
| DOMAIN KNOWLEDGE  | 90-180   | Purple | Expertise/learning curve   |
| SCOPE/FOCUS       | 180-270  | Blue   | Out of scope research      |
| METHODOLOGY       | 270-360  | Orange | Approach/technique issues  |

### Status Colors

| Status      | Color          | Meaning               |
| ----------- | -------------- | --------------------- |
| Implemented | Green (#2ecc71) | Working in production |
| Partial     | Orange (#f39c12) | Partially implemented |
| Deferred    | Blue (#3498db) | Planned for future    |
| Abandoned   | Purple (#9b59b6) | Not pursuing         |
| Blocked     | Orange (#e67e22) | Waiting on external   |
| Infeasible  | Red (#e74c3c)  | Cannot be done        |
| Superseded  | Teal (#1abc9c) | Replaced by better    |

### Pattern Taxonomy

| Badge | Type          | Description                    |
| ----- | ------------- | ------------------------------ |
| MECH  | Mechanical    | Must occur, passes obfuscation |
| PROB  | Probabilistic | Statistical edge (>60%)        |
| NARR  | Narrative     | Folklore, fails obfuscation    |

## Research Paths Data

Each research path includes:

```javascript
{
    id: "unique-id",           // Internal identifier
    ring: 0-5,                 // Complexity ring
    quadrant: "data|knowledge|scope|methodology",
    angle: 0-180,              // Position within quadrant
    label: "Short",            // Primary label (displayed)
    labelLong: "Label",        // Secondary label (displayed below)
    status: "implemented|partial|deferred|abandoned|blocked|infeasible|superseded",
    taxonomy: "mechanical|probabilistic|narrative",
    title: "Full Research Path Title",
    paper: "Paper 1|Paper 2|Paper 3|N/A",
    description: "Detailed description of what this research entails",
    barrier: "Why this research stalled or was abandoned",
    unblock: "What would enable this research to proceed (null if N/A)",
    barrierType: "data|knowledge|scope|methodology|compute|none",
    related: ["Related", "Concepts", "Array"]
}
```

### Complete Research Path Reference

#### Ring 0: Core (Validated)

**Core GEX Analysis System**

- Status: Implemented
- Taxonomy: Mechanical
- Paper: Paper 1
- Description: GEX calculations, 15-pattern library, single LLM agent
  (MarketMechanicsAgent), O3-mini integration. WHO/WHOM/WHAT causal
  attribution framework.
- GitHub Issues: Core system spans multiple foundational issues

#### Ring 1: Minor Extensions

**Dynamic Trailing Stops**
- Status: Partial
- Taxonomy: Probabilistic
- Paper: Paper 2+
- Issue: [#46](https://github.com/iAmGiG/gex-llm-patterns/issues/46)
- Barrier: Trade execution outside research scope

**Six-Category Pattern Classification**
- Status: Partial
- Taxonomy: Probabilistic
- Paper: Paper 1
- Barrier: Each category requires dedicated historical event testing

#### Ring 2: Statistical Expertise

**Monte Carlo & Permutation Testing**
- Status: Partial
- Taxonomy: Mechanical
- Issue: [#11](https://github.com/iAmGiG/gex-llm-patterns/issues/11)
- Barrier: Massive compute for 10K+ iterations

**CPCV (Combinatorial Purged Cross-Validation)**
- Status: Deferred
- Taxonomy: Mechanical
- Issue: [#27](https://github.com/iAmGiG/gex-llm-patterns/issues/27)
- Barrier: Requires advanced ML validation expertise

**Sequential Pattern Mining (PrefixSpan)**
- Status: Superseded
- Taxonomy: Probabilistic
- Barrier: LLM-based detection became core thesis contribution

#### Ring 3: Infrastructure Required

**Multi-Agent LLM Orchestration**
- Status: Abandoned
- Taxonomy: Narrative
- Issue: [#20](https://github.com/iAmGiG/gex-llm-patterns/issues/20)
- Barrier: 75% win rate achieved with single agent

**LLM Few-Shot Training Pipeline**
- Status: Abandoned
- Taxonomy: Narrative
- Issue: [#38](https://github.com/iAmGiG/gex-llm-patterns/issues/38)
- Barrier: Production infrastructure, not research contribution

**Forward-Test Live Trading**
- Status: Abandoned
- Taxonomy: Narrative
- Issue: [#39](https://github.com/iAmGiG/gex-llm-patterns/issues/39)
- Barrier: Production trading system exceeds research scope

#### Ring 4: Expensive/Rare Data

**Short Put Arbitrage Detection**
- Status: Abandoned
- Taxonomy: Mechanical
- Barrier: Requires fill-side TAQ data ($10K+/year)

**0DTE Intraday Gamma Dynamics**
- Status: Blocked
- Taxonomy: Mechanical
- Issue: [#130](https://github.com/iAmGiG/gex-llm-patterns/issues/130)
- Barrier: Methodological conflict with obfuscation testing

**Cross-Asset Dealer Hedging Networks**
- Status: Deferred
- Taxonomy: Probabilistic
- Issue: [#132](https://github.com/iAmGiG/gex-llm-patterns/issues/132)
- Barrier: Each asset class requires separate literature base

#### Ring 5: Theoretical Barriers

**Third-Order Greeks (Speed, Zomma, Color)**
- Status: Infeasible
- Taxonomy: Narrative
- Barrier: Signal-to-noise ratio makes third-order derivatives unmeasurable

**Volatility Greeks (Vomma, Veta, Vanna)**
- Status: Infeasible
- Taxonomy: Narrative
- Barrier: Requires real-time IV surface data (~$15K/year)

## Interactive Features

### Filters
- **Barrier Type:** All, Data, Knowledge, Compute, Scope, Methodology
- **Status:** Click legend items to filter by status
- **Search:** Type to filter by title or related concepts

### Node Interactions
- **Hover:** Tooltip with title, status, description preview
- **Click:** Full details panel + connection lines to related nodes
- **Related Tags:** Click to search for that concept

### Keyboard Navigation
- **Arrow Keys:** Navigate between nodes
- **Escape:** Clear selection
- **Enter/Space:** Select first node when none selected

## Customization Guide

### Adding New Research Paths

Add entries to the `researchPaths` array in the `<script>` section:

```javascript
{
    id: "new-path",
    ring: 2,  // Complexity level 0-5
    quadrant: "knowledge",  // data|knowledge|scope|methodology
    angle: 100,  // 0-180 within quadrant
    label: "New",
    labelLong: "Path",
    status: "deferred",
    taxonomy: "probabilistic",
    title: "New Research Path Title",
    paper: "Paper 2",
    description: "Full description here",
    barrier: "Why it stalled",
    unblock: "What would enable it",
    barrierType: "knowledge",
    related: ["Related", "Concepts"]
}
```

### Modifying Ring Configuration

Edit the `config` object:

```javascript
const config = {
    rings: [0, 55, 105, 155, 210, 265, 315],  // Ring radii
    ringNames: [
        "Validated Core",
        "Minor Extensions",
        // ... etc
    ],
    // ...
}
```

### Changing Colors

Status colors in `config.statusColors`:
```javascript
statusColors: {
    implemented: { fill: "#2ecc71", stroke: "#27ae60" },
    // ... etc
}
```

### Adjusting Quadrant Layout

```javascript
quadrants: {
    data: { start: 0, end: 90, label: "DATA ACCESS", color: "#e74c3c" },
    knowledge: { start: 90, end: 180, label: "DOMAIN KNOWLEDGE", color: "#9b59b6" },
    // ... etc
}
```

## Three-Paper Research Arc

The visualization references a three-paper PhD dissertation arc:

| Paper | Title              | Status        | Focus                      |
| ----- | -------------------|-------|------------------|
| 1     | LLM Pattern Det.   | Valid | Can LLMs detect patterns?  |
| 2     | Regime Detection   | In Prog | Can LLMs classify regimes? |
| 3     | Sector Rotation    | Planned | Can LLMs ID rotation signals? |

## Technical Details

- **Self-Contained:** No external dependencies (CSS/JS embedded)
- **SVG-Based:** Vector graphics scale to any resolution
- **Responsive:** Adapts to smaller screens
- **Animation:** CSS keyframe animations for entrance/pulse effects

## License

This visualization was created for the GEX-LLM Patterns research project. Refer to the source repository for license terms.

## Related Documentation

From the source project:
- Pattern Taxonomy: Classification framework for market patterns
- Validation Framework: Testing methodology for LLM pattern detection
- Data Obfuscation: Preventing LLM training data leakage in validation

---

*Documentation generated for standalone use. For the latest research context, see the [source repository](https://github.com/iAmGiG/gex-llm-patterns).*
