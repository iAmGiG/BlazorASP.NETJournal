# Strategic Research Roadmap: Computational Market Microstructure

> From Static Detection to Temporal Causal Reasoning

## Executive Summary

This document outlines the multi-phase research trajectory for the **GEX-LLM Patterns** project,
investigating whether Large Language Models can reason about latent market forces—specifically
dealer gamma exposure—without relying on temporal memorization.

### Key Findings (Phase 1-2)

| Metric | Value | Significance |
|--------|-------|--------------|
| Detection Rate (Unbiased) | 71.5% | LLM infers regime from structure alone |
| Detection Rate (Biased) | 100% | With regime hints |
| Predictive Accuracy | 91.2% | Consequence prediction |
| Persistent Regime Detection (2024) | 81.2% | High coherence markets |
| Fragmented Market Detection (2020) | 12.1% | Negative control validation |
| GEX Magnitude Growth | 360% | $5B (2021) → $23B (2024) |

---

## Phase 1: Foundation — Obfuscation Testing

### The Memorization Problem

Financial AI faces a critical validation challenge: models trained on internet data have likely
seen historical market events. The **obfuscation protocol** strips all temporal identifiers:

- No dates, tickers, or event labels
- Inputs genericized ("Index_1", "Day T+0")
- Pure structural reasoning required

### WHO → WHOM → WHAT Framework

The causal chain forcing interpretable economic narratives:

```
WHO:   The constrained actor (Option Dealer/Market Maker)
       ↓
WHOM:  The counterparty (Directional Trader, HFT)
       ↓
WHAT:  The mechanical outcome (forced selling/buying for Delta Neutrality)
```

### Key Innovation: Decoupling Detection from Profitability

Detection rates remained stable across quarters even as trading profitability fluctuated. The LLM
acts as a **physicist** (identifying structural tension) rather than a **trader** (predicting
profitable outcomes).

---

## Phase 2: Temporal Dynamics — Evolution of Constraint

### Central Narrative

> "From Static Snapshots to Temporal Narratives: Validating LLM Perception of Market Regime Evolution"

### Research Questions

| RQ | Question | Method |
|----|----------|--------|
| RQ1 | Can LLMs distinguish formation/persistence/decay of dealer constraints? | Sequential GEX analysis |
| RQ2 | Can LLMs autonomously cluster data into volatility regimes? | Unsupervised clustering |
| RQ3 | Can LLMs provide causal narratives for mathematically-identified anomalies? | Matrix Profile + LLM |

### Experimental Design

#### Experiment 1: Sequential Pattern Mining
- **Input**: 5-day sliding windows, days labeled "Day 1-5"
- **Task**: Detect if resistance is hardening or eroding
- **Baseline**: PrefixSpan algorithm comparison
- **Hypothesis**: LLM outperforms by understanding context of erosion

#### Experiment 2: Unsupervised Regime Detection
- **Input**: 100 unlabeled, obfuscated GEX profiles
- **Task**: Cluster into 3 categories based on dealer behavior
- **Expected Clusters**:
  - A: High Positive Gamma (Low Vol)
  - B: Deep Negative Gamma (High Vol)
  - C: Transition states
- **Metric**: Homogeneity Score of Realized Volatility

#### Experiment 3: Neuro-Symbolic Motif Discovery
- **Step 1**: Matrix Profile (STUMPY) identifies top 10 motifs
- **Step 2**: Extract sequences, pass to LLM
- **Step 3**: LLM explains "WHY" using WHO→WHOM→WHAT
- **Value**: Math finds patterns, LLM explains economics

#### Experiment 4: Cross-Asset Generalization
- **Data**: Expand to TSLA, NVDA (high-beta stocks)
- **Test**: Zero-shot transfer of SPX prompts
- **Hypothesis**: "Pinning" detection degrades, "Squeeze" detection improves

---

## Phase 3: Cross-Asset Generalization

### The Divergence Problem

Index and single-stock gamma dynamics are fundamentally different:

| Feature | Index (SPY/SPX) | Single Stock (TSLA) |
|---------|-----------------|---------------------|
| Dominant Flow | Wealth Management | Retail Speculation |
| Dealer Position | Generally Long Gamma | Generally Short Gamma |
| Regime Type | Pinning / Mean Reversion | Squeeze / Momentum |
| Mechanism | Dispersion Selling | Weaponized Call Buying |
| Volatility | Suppressed | Amplified |

### Systematic Dampening vs. Idiosyncratic Acceleration

**Long Gamma (Index)**: Dealers buy dips, sell rallies → Mean reversion
**Short Gamma (Single Stock)**: Dealers buy rallies, sell dips → Positive feedback

### Target Assets (10 stocks)

| Category | Tickers | Profile |
|----------|---------|---------|
| High-Beta/Momentum | NVDA, TSLA, AMD | Gamma squeeze prone |
| Mega-Cap Tech | AAPL, MSFT, AMZN, META | Index proxies |
| Financials/Value | JPM, BAC, GS | Control group |

### Dispersion Trading Mechanism

```
Dispersion Trade Structure:
├── SELL Index Volatility (SPX options)
│   └── Dealers become LONG index gamma → Suppression
└── BUY Component Volatility (single stocks)
    └── Dealers become SHORT stock gamma → Amplification
```

### The "Weaponized Gamma" Phenomenon

GameStop (2021) demonstrated intentional exploitation of dealer mechanics:
1. Massive OTM call buying
2. Forces dealer stock purchases
3. Price rises → Options more sensitive
4. Feedback loop accelerates

---

## Phase 2 (Extended): Intraday Dynamics — The Scar Tissue Hypothesis

### The Paradox

> How do 0DTE options (< 24 hours lifespan) create persistent multi-month regimes?

### Scar Tissue Mechanism

```
09:45 AM │ Opening Flurry
         │ Establish initial dealer inventory
         ↓
12:00 PM │ Midday Pivot
         │ Theta decay accelerates, gamma adjustments
         ↓
03:00 PM │ Gamma Trap
         │ ATM gamma explodes, hyper-active hedging
         ↓
04:00 PM │ Expiry Discontinuity
         │ 0DTE gamma vanishes INSTANTLY
         │ Delta hedge (stock/futures) REMAINS
         ↓
OVERNIGHT │ Residual Inventory
         │ "Toxic flow" carried overnight
         │ Cannot liquidate without slippage
         ↓
NEXT DAY │ Structural Persistence
         │ "Scar tissue" dictates opening price
         │ Links independent 0DTE cycles
```

### Intraday Experiments

| Experiment | Description | Success Metric |
|------------|-------------|----------------|
| Intraday Flip Detection | Predict 4PM state from 9:45AM data | Accuracy vs baseline |
| Volume vs. OI | A/B test flow vs. structure predictive power | Correlation strength |
| Gamma Wall Validation | LLM identifies resistance levels | Price reversal frequency |

### Supplementary Signals

- **SABR Parameters**: ρ (spot-vol correlation), ν (vol-of-vol)
- **GAMMA-SVIX Divergence**: Historical correlation -0.89; divergence = regime shift

---

## Phase 4: Agent-Based Simulation

### Neuro-Symbolic Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    NEURO-SYMBOLIC AGENT                  │
├──────────────────────┬──────────────────────────────────┤
│   NEURO (System 1)   │      SYMBOLIC (System 2)         │
│   ─────────────────  │      ──────────────────          │
│   LLM "Intuition"    │      Rule-Based "Physics"        │
│   • Sentiment        │      • LOB Matching Engine       │
│   • Strategy         │      • Capital Requirements      │
│   • FOMO/Fear        │      • Risk Limits (VaR)         │
│   • Intent to Trade  │      • Clearing Mechanics        │
│                      │                                  │
│   Role: "Trader"     │      Role: "Risk Manager"        │
└──────────────────────┴──────────────────────────────────┘
```

### Agent Profiles

| Agent Type | Core Function | LLM Prompt | Symbolic Constraint |
|------------|---------------|------------|---------------------|
| Market Maker | Provide liquidity | "Minimize inventory risk; capture spread" | Max position; Delta threshold |
| 0DTE Speculator | Buy volatility | "Chase momentum; buy OTM calls" | Capital limits; PDT rules |
| Value Investor | Mean reversion | "Buy dip if price < fundamental" | Long-only; No leverage |
| Noise Trader | Random liquidity | (Stochastic) | Random walk budget |

### Counterfactual Testing

Key experiment: Run simulation WITH and WITHOUT "Market Maker Inventory Constraint"
- If regimes only emerge with constraints → Causal validation
- Tests Hayekian "spontaneous order" hypothesis

---

## Phase 5: Network Effects — GNN Contagion

### The Network Problem

Dealers don't hedge in isolation—they manage portfolios across assets:
- Hedge JPM with XLF
- Hedge SPX with ES futures
- Stress in one node → Forced liquidation in others

### Graph Neural Network Approaches

| Method | Description | Advantage |
|--------|-------------|-----------|
| TGNN | Dealers/assets as nodes, hedging as edges | Captures systemic risk |
| Temporal GAT | Attention-weighted neighbor importance | Lead-lag effects |
| LLM+GNN Hybrid | LLM extracts causal graph from news | Dynamic topology |

### Causal Discovery

Using PCMCI algorithms + LLM to identify directed acyclic graphs:

```
0DTE Volume → Dealer Inventory → Overnight Volatility
     ↓              ↓                    ↓
   (cause)      (mechanism)          (effect)
```

---

## Theoretical Framework: The Physics of Gamma

### Core Mechanics

**Gamma (Γ)**: Rate of change of Delta with respect to price
- Represents convexity/"acceleration" of dealer exposure
- Hedging is contractual obligation, not speculative choice

### Regime Types

| Regime | Dealer Position | Market Effect | Behavior |
|--------|-----------------|---------------|----------|
| Positive Gamma | Long Options | Mean Reversion | Buy dips, sell rallies |
| Negative Gamma | Short Options | Vol Amplifier | Sell dips, buy rallies |

### Temporal Greeks (Phase 2+)

| Greek | Definition | Market Implication |
|-------|------------|-------------------|
| Charm (dΔ/dt) | Delta decay over time | Predictable flows at specific times |
| Vanna (dΔ/dσ) | Delta sensitivity to volatility | Crisis acceleration driver |

---

## Literature Positioning

### Gaps Addressed

| Domain | Current Limitation | Our Approach |
|--------|-------------------|--------------|
| Time-Series Transformers | Black box correlation | Mechanism inference |
| Financial NLP/Sentiment | Ignores market state | Regime-conditioned analysis |
| Causal Discovery | Low-frequency, linear | High-frequency, non-linear |

### Comparative Analysis

| Feature | TimeLLM | Sentiment NLP | GEX-LLM (Ours) |
|---------|---------|---------------|----------------|
| Input | Price/Volume | News/Social | Obfuscated Structure |
| Goal | Forecasting | Classification | Mechanism Inference |
| Interpretability | Low | Medium | High (WHO→WHOM→WHAT) |
| Regime Handling | Fails at shifts | State-ignorant | Explicit modeling |

---

## Strategic Implications

### From Alpha to Surveillance

The research enables **Market Surveillance** rather than just trading:
- Detect fragility before crashes
- "Risk Sentinel" identifying structural instability
- Independent of current price level

### Solving the Black Box Problem

By anchoring reasoning to option math (verifiable ground truth), we:
- Validate LLM reasoning in high-stakes domains
- Create template for other critical applications
- Build confidence in abstract risk reasoning

### Roadmap to Phase 3+

```
Phase 1: Can AI see the wall?
         ↓ (Validated)
Phase 2: Can AI see the wall evolving?
         ↓ (In Progress)
Phase 3: Can AI see walls across assets?
         ↓ (Proposed)
Phase 4: Can AI simulate wall formation?
         ↓ (Proposed)
Phase 5: Can AI integrate news + structure?
         (Future: Cross-Modal Reasoning)
```

---

## Related Visualizations

- [Research Complexity Map](research_complexity_map.html) — Abandoned/deferred research paths
- [Data Pipeline State Diagram](data_pipeline_state_diagram.html) — Multi-tier data architecture
- [GEX Visualizer](../gex-visualizer/index.html) — Interactive regime comparator

---

## References

This roadmap synthesizes findings from:
- arXiv:2512.17923v2 — Phase 1 paper
- gex-llm-patterns repository documentation
- Academic literature on gamma fragility (Barbon & Buraschi)
- Practitioner research on weaponized gamma dynamics

---

*Last Updated: 2026-01-09*
*Project: GexVisor / GEX-LLM Patterns*
