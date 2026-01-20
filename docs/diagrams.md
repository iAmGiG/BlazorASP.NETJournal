# System Diagrams

Visual documentation of GexVisor architecture and state flows.
These diagrams use [Mermaid](https://mermaid.js.org/) syntax and render natively in GitHub.

## GEX Regime State Machine

The core domain model - dealer gamma exposure determines market behavior.

```mermaid
stateDiagram-v2
    direction LR

    [*] --> PositiveGamma : Data Loaded

    PositiveGamma --> NegativeGamma : GEX crosses below zero
    NegativeGamma --> PositiveGamma : GEX crosses above zero

    state PositiveGamma {
        direction TB
        [*] --> Dampening
        Dampening : Dealers LONG gamma
        Dampening : Buy dips, sell rallies
        Dampening : Volatility suppressed
        Dampening : Mean reversion
    }

    state NegativeGamma {
        direction TB
        [*] --> Amplifying
        Amplifying : Dealers SHORT gamma
        Amplifying : Sell dips, buy rallies
        Amplifying : Volatility amplified
        Amplifying : Momentum/trend
    }
```

## GitHub OAuth Device Flow

Authentication sequence for GitHub Projects integration.

```mermaid
sequenceDiagram
    participant U as User
    participant App as GexVisor
    participant GH as GitHub API

    U->>App: Click "Connect to GitHub"
    App->>GH: POST /login/device/code
    GH-->>App: device_code, user_code, verification_uri

    App->>U: Display "Enter XXXX-XXXX at github.com/login/device"

    loop Poll every 5 seconds
        App->>GH: POST /login/oauth/access_token
        alt Pending
            GH-->>App: authorization_pending
        else Approved
            GH-->>App: access_token, refresh_token
        else Expired
            GH-->>App: expired_token
        end
    end

    App->>App: Store token in localStorage
    App->>U: Show "Connected as @username"
```

## Data Loading Pipeline

Flow from raw JSON files to rendered visualization.

```mermaid
flowchart TD
    subgraph Input
        A[wwwroot/data/*.json] --> B[HttpClient GET]
    end

    subgraph Processing
        B --> C{Parse JSON}
        C -->|Success| D[GexTimeline Model]
        C -->|Failure| E[Parse Error]

        D --> F{Validate Schema}
        F -->|Valid| G[GexStateService]
        F -->|Invalid| H[Validation Error]
    end

    subgraph Analysis
        G --> I[Regime Analysis]
        I --> J[RegimeSegments]
        I --> K[RegimeTransitions]
        I --> L[Summary Stats]
    end

    subgraph Output
        J --> M[Timeline Visualization]
        K --> M
        L --> N[Stats Panel]
    end

    E --> O[Error State]
    H --> O
    O --> P[Retry Button]
    P --> B
```

## Task Board State Flow

Local tasks and GitHub-synced items with status mapping.

```mermaid
stateDiagram-v2
    direction TB

    state "Local Tasks" as Local {
        [*] --> Backlog
        Backlog --> InProgress : Start
        InProgress --> Done : Complete
        InProgress --> Backlog : Defer
        Done --> Archived : Archive
    }

    state "GitHub Sync" as GitHub {
        [*] --> Fetching
        Fetching --> Mapping : Items loaded
        Mapping --> Normalized : StatusMapper

        state Mapping {
            [*] --> Infer
            Infer : "Todo" → Backlog
            Infer : "In Progress" → InProgress
            Infer : "Done" → Done
        }
    }

    Local --> GitHub : View GitHub tab
    GitHub --> Local : View Local tab
```

## Paper Trade Lifecycle

State machine for paper trading journal entries.

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Planned : Create Entry

    Planned --> Open : Set Entry Price
    Open --> Closed : Manual Exit
    Open --> Stopped : Stop-Loss Hit

    Closed --> [*] : P&L Calculated
    Stopped --> [*] : P&L Calculated

    state Open {
        [*] --> Tracking
        Tracking : Monitor price
        Tracking : Update unrealized P&L
        Tracking : Check stop/target
    }
```

## Component Data Flow

How data flows through the GEX Visualizer components.

```mermaid
flowchart LR
    subgraph Services
        GDS[GexDataService]
        GSS[GexStateService]
        CS[ComparisonService]
    end

    subgraph State
        GT[GexTimeline]
        RA[RegimeAnalysis]
        SEL[Selection State]
    end

    subgraph Components
        GV[GexVisualizer Page]
        TL[TimelineChart]
        RT[RegimeTimeline]
        SP[StatsPanel]
        CP[ComparisonPanel]
    end

    GDS -->|Load JSON| GT
    GT -->|Analyze| GSS
    GSS -->|Regimes| RA

    GV -->|Subscribe| GSS
    GSS -->|Notify| GV

    GV --> TL
    GV --> RT
    GV --> SP

    CS -->|Dual View| CP
    CP --> TL
```

## Service Dependencies

Dependency graph of GexVisor services.

```mermaid
flowchart BT
    subgraph Core
        LS[LocalStorageService]
        HTTP[HttpClient]
    end

    subgraph Data
        GDS[GexDataService]
        GSS[GexStateService]
        CS[ComparisonService]
        CAS[ComparisonAnalysisService]
    end

    subgraph Journal
        AS[AnnotationService]
        PTS[PaperTradeService]
        BS[BacktestService]
        NS[NotebookService]
    end

    subgraph GitHub
        GAS[GitHubAuthService]
        GPS[GitHubProjectService]
        BSS[BoardStateService]
        SM[StatusMapper]
    end

    GDS --> HTTP
    GDS --> LS
    GSS --> GDS
    CS --> GSS
    CAS --> CS

    AS --> LS
    PTS --> LS
    BS --> LS
    NS --> LS

    GAS --> HTTP
    GAS --> LS
    GPS --> HTTP
    GPS --> GAS
    BSS --> GPS
    SM --> LS
```

## Regime Transition Detection

Algorithm for detecting regime changes in GEX data.

```mermaid
flowchart TD
    A[Load GexTimeline] --> B[Sort by Date]
    B --> C[Initialize: prevRegime = null]

    C --> D{For each entry}
    D --> E[Determine regime from GEX value]

    E --> F{GEX > 0?}
    F -->|Yes| G[regime = Positive]
    F -->|No| H[regime = Negative]

    G --> I{regime != prevRegime?}
    H --> I

    I -->|Yes| J[Create RegimeTransition]
    I -->|No| K[Extend current segment]

    J --> L[Start new RegimeSegment]
    K --> M[Update segment end date]

    L --> N{More entries?}
    M --> N

    N -->|Yes| D
    N -->|No| O[Return RegimeAnalysisSummary]
```

---

## Rendering Notes

These diagrams render automatically in:
- **GitHub** - Native Mermaid support in markdown
- **VS Code** - With "Markdown Preview Mermaid Support" extension
- **Notion** - Paste as code block with `mermaid` language

For other environments, use the [Mermaid Live Editor](https://mermaid.live/) to export as SVG/PNG.
