# Research Visualizations

> **Migration Status**: These standalone HTML visualizations have been migrated to Blazor components in the GexVisor.UI project. See [Migration Notes](#migration-to-blazor) below.

## Overview

Interactive visualizations for the GEX-LLM Patterns research project, mapping research complexity, methodology, and findings.

**Live Context:** These visualizations communicate research scope decisions for PhD dissertation work on LLM-based market mechanics pattern detection.

## Source Project

- **Repository:** [iAmGiG/gex-llm-patterns](https://github.com/iAmGiG/gex-llm-patterns)
- **GexVisor App:** `/arcade` - Research Arcade landing page

## Key Metrics

| Metric            | Value  | Source                       |
| ----------------- | ------ | ---------------------------- |
| Detection Rate    | 71.5%  | Unbiased prompts validation  |
| Predictive Accuracy | 91.2% | When pattern detected        |
| Trading Days      | 242    | Full year 2024 backtesting   |
| Patterns Cataloged | 15     | Market mechanics pattern lib |
| Research Paths    | 16     | Explored/abandoned paths     |

---

## Migration to Blazor

All visualizations have been migrated to Blazor components as part of Epic #100:

| Original HTML | Blazor Page | Route | Issue |
|---------------|-------------|-------|-------|
| `data_pipeline_state_diagram.html` | `DataPipelineState.razor` | `/research/pipeline` | #101 |
| `paper2.html` | `Paper2Agreement.razor` | `/research/paper2` | #102 |
| `data_mining_pattern_discovery.html` | `PatternDiscovery.razor` | `/research/patterns` | #103 |
| `research_complexity_map.html` | `ResearchComplexityMap.razor` | `/research/complexity` | #104 |

### Benefits of Migration

- **Integrated Navigation**: All visualizations accessible from Research Arcade
- **Shared Components**: Reusable DetailPanel, KddStepCard, TechniqueCard
- **Consistent Styling**: Uses GexVisor design system variables
- **Type Safety**: C# models for research path data
- **State Management**: Proper filtering, selection, and animation handling

### New File Structure

```
src/GexVisor.UI/
├── Pages/Research/
│   ├── DataPipelineState.razor       # Pipeline state diagram
│   ├── Paper2Agreement.razor         # Structural agreement test
│   ├── PatternDiscovery.razor        # KDD process visualization
│   └── ResearchComplexityMap.razor   # Research paths map
├── Components/Research/
│   ├── DetailPanel.razor             # Shared modal overlay
│   ├── KddStepCard.razor             # KDD step component
│   └── TechniqueCard.razor           # Pattern discovery technique
└── Models/
    ├── Paper2Data.cs                 # Timeline data (2020-2025)
    └── ResearchPath.cs               # 16 research paths with metadata
```

### Legacy HTML Files

The original HTML files are preserved for reference:
- `data_pipeline_state_diagram.html` - 7 state rows, 880 lines
- `paper2.html` - Timeline simulation, 330 lines
- `data_mining_pattern_discovery.html` - KDD process, 1,369 lines
- `research_complexity_map.html` - SVG radial chart, 1,660 lines
- `index.html` - Landing page (superseded by `/arcade`)

---

## Research Paths Reference

### Complexity Rings (0-5)

| Ring | Name                   | Description                    |
| ---- | ---------------------- | ------------------------------ |
| 0    | Validated Core         | Currently implemented/validated |
| 1    | Minor Extensions       | Small additions to system      |
| 2    | Statistical Expertise  | Advanced statistical methods   |
| 3    | Infrastructure Req.    | Significant engineering work   |
| 4    | Expensive/Rare Data    | Costly data sources ($10K+/yr) |
| 5    | Theoretical Barriers   | Fundamental feasibility issues |

### Barrier Types (Quadrants)

| Quadrant          | Color  | Description                |
| ----------------- | ------ | -------------------------- |
| DATA ACCESS       | Red    | Data availability/cost     |
| DOMAIN KNOWLEDGE  | Purple | Expertise/learning curve   |
| SCOPE/FOCUS       | Blue   | Out of scope research      |
| METHODOLOGY       | Orange | Approach/technique issues  |
| COMPUTE           | Green  | Computational requirements |

### Status Colors

| Status      | Color   | Meaning               |
| ----------- | ------- | --------------------- |
| Implemented | Green   | Working in production |
| Partial     | Orange  | Partially implemented |
| Deferred    | Blue    | Planned for future    |
| Abandoned   | Purple  | Not pursuing          |
| Blocked     | Orange  | Waiting on external   |
| Infeasible  | Red     | Cannot be done        |
| Superseded  | Teal    | Replaced by better    |

### Pattern Taxonomy

| Badge | Type          | Description                    |
| ----- | ------------- | ------------------------------ |
| MECH  | Mechanical    | Must occur, passes obfuscation |
| PROB  | Probabilistic | Statistical edge (>60%)        |
| NARR  | Narrative     | Folklore, fails obfuscation    |

---

## Three-Paper Research Arc

| Paper | Title              | Status    | Focus                         |
| ----- | ------------------ | --------- | ----------------------------- |
| 1     | LLM Pattern Det.   | Validated | Can LLMs detect patterns?     |
| 2     | Regime Detection   | In Prog   | Can LLMs classify regimes?    |
| 3     | Sector Rotation    | Planned   | Can LLMs ID rotation signals? |

---

## Related Documentation

- [RESEARCH_ROADMAP.md](RESEARCH_ROADMAP.md) - Multi-phase research trajectory
- [Pattern Taxonomy](https://github.com/iAmGiG/gex-llm-patterns) - Classification framework
- [Validation Framework](https://github.com/iAmGiG/gex-llm-patterns) - Testing methodology

---

*Documentation updated for Blazor migration. For the latest research context, see the [source repository](https://github.com/iAmGiG/gex-llm-patterns).*
