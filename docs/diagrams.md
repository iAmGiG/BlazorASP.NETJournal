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

## Architecture Deep Dive

The following diagrams provide comprehensive documentation of the system architecture.

### Service Dependency Graph (Complete)

All 20 services with their dependencies and coupling analysis.

```mermaid
flowchart TB
    subgraph Singleton["Singleton (App-wide State)"]
        GSS[GexStateService]
        IGSS{{IGexStateService}}
    end

    subgraph Core["Core Services"]
        GDS[GexDataService]
        LSS[LocalStorageService]
        IGDS{{IGexDataService}}
        ILSS{{ILocalStorageService}}
    end

    subgraph Journal["Journal Services (Template Pattern)"]
        BES[BaseEntryService&lt;T&gt;<br/>Abstract]
        NS[NotebookService]
        PTS[PaperTradeService]
        TLS[TradeLogService]
        BS[BacktestService]
        AS[AnnotationService]
        RTS[ResearchTaskService]
    end

    subgraph GitHub["GitHub Integration"]
        GAS[GitHubAuthService]
        GPS[GitHubProjectService]
        BSS[BoardStateService<br/>5-min cache]
        SM[StatusMapper]
    end

    subgraph CrossAsset["Cross-Asset Analysis"]
        CS[ComparisonService]
        CAS[ComparisonAnalysisService<br/>Stateless]
    end

    subgraph Utilities["Utilities"]
        TS[TagService]
        SS[SqliteService]
        DMP[DecisionMetadataParser]
    end

    subgraph External["External Dependencies"]
        HTTP[HttpClient]
        JS[IJSRuntime]
        LS[(localStorage)]
    end

    GSS -->|implements| IGSS
    GDS -->|implements| IGDS
    LSS -->|implements| ILSS
    GDS --> HTTP
    LSS --> JS
    JS --> LS

    NS & PTS & BS & AS & RTS --> BES
    BES --> ILSS

    GAS --> HTTP
    GAS --> LSS
    GPS --> HTTP
    GPS --> GAS
    BSS --> GPS
    BSS --> LSS
    SM --> ILSS

    CS --> IGDS
    TS --> ILSS

    GSS --> GDS
```

### Complete Data Flow

End-to-end data journey from sources to UI components.

```mermaid
flowchart LR
    subgraph Sources["Data Sources"]
        JSON[(wwwroot/data/*.json)]
        GH[(GitHub GraphQL API)]
        STORE[(Browser localStorage)]
    end

    subgraph Loading["Loading Layer"]
        GDS[GexDataService]
        GPS[GitHubProjectService]
        LSS[LocalStorageService]
    end

    subgraph Transform["Transformation"]
        TL[TransformTimeline<br/>String→DateOnly]
        SM[StatusMapper<br/>Normalize status]
    end

    subgraph State["State Management"]
        GSS[GexStateService<br/>OnStateChanged]
        BSS[BoardStateService<br/>TTL cache]
        BES[BaseEntryService<br/>OnEntriesChanged]
    end

    subgraph Analysis["Analysis Layer"]
        RA[RegimeAnalysis<br/>Segments + Transitions]
        CAS[ComparisonAnalysis<br/>Correlations]
    end

    subgraph UI["UI Components"]
        GV[GexVisualizer]
        CD[ComparisonDashboard]
        KB[KanbanBoard]
        JN[Journal Pages]
    end

    JSON --> GDS --> TL --> GSS --> GV
    GSS --> RA --> GV
    GSS --> CAS --> CD
    GH --> GPS --> SM --> BSS --> KB
    STORE --> LSS --> BES --> JN
```

### Event-Driven State Patterns

How components react to state changes.

```mermaid
sequenceDiagram
    participant UI as GexVisualizer
    participant GSS as GexStateService
    participant Timer as System.Timer
    participant Chart as GexChart

    Note over GSS: Pattern 1: Reactive Updates
    UI->>GSS: SetCurrentIndex(5)
    GSS->>GSS: Update _state
    GSS->>GSS: NotifyStateChanged()
    GSS-->>UI: OnStateChanged
    GSS-->>Chart: OnStateChanged
    Chart->>Chart: CalculateBars()
    Chart->>Chart: Re-render SVG

    Note over GSS,Timer: Pattern 2: Simulation Playback
    UI->>GSS: ToggleSimulation()
    GSS->>Timer: Start(interval)

    loop Every Tick (500-2000ms)
        Timer->>GSS: OnElapsed
        GSS->>GSS: SetCurrentIndex(++i)
        GSS-->>Chart: OnStateChanged
        Chart->>Chart: CalculateBars() ⚠️ Recalc
    end

    UI->>GSS: ToggleSimulation()
    GSS->>Timer: Stop()
```

### Component Hierarchy with Service Dependencies

Which services each component depends on.

```mermaid
graph TB
    subgraph GexVisualizerPage["GexVisualizer Page"]
        GV[GexVisualizer.razor]
        GH[GexHeader]
        GS[GexSidebar]
        GC1[GexChart<br/>Normalized]
        GC2[GexChart<br/>Absolute]
        RT[RegimeTimeline]
        AP[AnnotationPanel]
        KSO[KeyboardShortcutsOverlay]
    end

    subgraph ComparisonPage["ComparisonDashboard Page"]
        CD[ComparisonDashboard.razor]
        CM[CorrelationMatrix]
        RDL[RegimeDivergenceList]
        RTM[RegimeTimeline<br/>Compact]
    end

    subgraph TaskBoardPage["TaskBoard Page"]
        TB[TaskBoard.razor]
        UKB[UnifiedKanbanBoard]
        GAP[GitHubAuthPanel]
        GPS_UI[GitHubProjectSelector]
    end

    subgraph JournalPages["Journal Pages"]
        RN[ResearchNotebook.razor]
        PT[PaperTrading.razor]
        BR[BacktestResults.razor]
    end

    GV --> GH & GS & GC1 & GC2 & RT & AP & KSO
    CD --> CM & RDL & RTM
    TB --> UKB & GAP & GPS_UI

    GSS{{GexStateService}} -.-> GV & GC1 & GC2 & RT & GH & GS
    COMP{{ComparisonService}} -.-> CD & CM
    AUTH{{GitHubAuthService}} -.-> TB & GAP
    BOARD{{BoardStateService}} -.-> UKB
```

### Entity Relationship Diagram

Domain model relationships.

```mermaid
erDiagram
    GexTimeline ||--o{ GexDataPoint : contains
    GexTimeline ||--|| DateRange : has

    GexDataPoint {
        DateOnly Date PK
        decimal Price
        decimal Gex
        decimal CallGex
        decimal PutGex
        decimal ZeroGamma
        decimal MaxGamma
        string Regime
        int Contracts
        decimal Quality
    }

    DateRange {
        DateOnly Start
        DateOnly End
    }

    IEntry ||--o{ BaseEntry : implements
    BaseEntry ||--o{ ContextualEntry : extends
    ContextualEntry ||--o{ NotebookEntry : extends
    ContextualEntry ||--o{ PaperTrade : extends
    ContextualEntry ||--o{ PatternAnnotation : extends
    BaseEntry ||--o{ BacktestResult : extends
    BaseEntry ||--o{ ResearchTask : extends

    BaseEntry {
        Guid Id PK
        DateTime CreatedAt
        DateTime UpdatedAt
        List Tags
    }

    ContextualEntry {
        string LinkedDate
        decimal PriceAtCreation
        decimal GexAtCreation
        bool IsNegativeGammaAtCreation
    }

    CrossAssetSummary ||--o{ AssetComparisonData : contains
    CrossAssetSummary ||--o{ CorrelationMetrics : contains
    CrossAssetSummary ||--o{ RegimeDivergenceEvent : contains
    CrossAssetSummary ||--|| DateRangeOverlap : has

    RegimeAnalysisSummary ||--o{ RegimeSegment : contains
    RegimeAnalysisSummary ||--o{ RegimeTransition : contains

    CorrelationMetrics {
        string Symbol1
        string Symbol2
        double PriceCorrelation
        double GexCorrelation
        double RegimeAlignment
        double RegimeFlipCorrelation
    }
```

### Caching Strategy (BoardStateService)

TTL-based caching with concurrency guard.

```mermaid
flowchart TB
    subgraph Request["Request Flow"]
        REQ[GetItemsAsync called]
        CHECK{Cache exists?<br/>TTL < 5min?}
        HIT[Return cached items]
        GUARD{_isLoading?}
        WAIT[Return stale cache]
        FETCH[Fetch from GitHub]
    end

    subgraph Update["Cache Update"]
        SET[Update _itemsCache]
        TS[Set _cacheTimestamp]
        NOTIFY[Fire OnBoardStateChanged]
    end

    REQ --> CHECK
    CHECK -->|Yes - Fresh| HIT
    CHECK -->|No - Stale/Missing| GUARD
    GUARD -->|Yes - In flight| WAIT
    GUARD -->|No| FETCH
    FETCH --> SET
    SET --> TS
    TS --> NOTIFY
    NOTIFY --> HIT
```

### Journal Entry Lifecycle

BaseEntryService template pattern.

```mermaid
sequenceDiagram
    participant UI as Component
    participant Svc as BaseEntryService&lt;T&gt;
    participant LS as LocalStorageService
    participant JS as Browser

    Note over Svc: Template Method Pattern

    UI->>Svc: LoadAsync()
    Svc->>LS: GetAsync&lt;List&lt;T&gt;&gt;(key)
    LS->>JS: localStorage.getItem
    JS-->>LS: JSON string
    LS-->>Svc: List&lt;T&gt; or default
    Svc->>Svc: Entries = result
    Svc-->>UI: OnEntriesChanged

    UI->>Svc: AddAsync(entry)
    Svc->>Svc: Entries.Insert(0, entry)
    Svc->>Svc: SaveAsync()
    Svc->>LS: SetAsync(key, Entries)
    LS->>JS: localStorage.setItem
    Note over JS: ⚠️ Silent failure possible
    Svc-->>UI: OnEntriesChanged
```

### Cross-Asset Comparison Flow

Multi-symbol loading and analysis.

```mermaid
sequenceDiagram
    participant User
    participant CD as ComparisonDashboard
    participant CS as ComparisonService
    participant GDS as GexDataService
    participant CAS as ComparisonAnalysisService

    User->>CD: Select SPY, QQQ, NVDA
    User->>CD: Click "Compare"
    CD->>CS: LoadSymbolsAsync([SPY, QQQ, NVDA])

    Note over CS: ⚠️ Race condition risk
    CS->>CS: _loadedAssets.Clear()

    par Parallel Loading
        CS->>GDS: LoadSymbolAsync("SPY")
        CS->>GDS: LoadSymbolAsync("QQQ")
        CS->>GDS: LoadSymbolAsync("NVDA")
    end

    GDS-->>CS: GexTimeline x3
    CS->>CS: Build AssetComparisonData[]
    CS-->>CD: OnSelectionChanged

    CD->>CAS: GenerateSummary(assets)
    Note over CAS: Pure stateless calculation

    loop For each pair
        CAS->>CAS: CalculateCorrelation()
    end
    CAS->>CAS: FindDivergenceEvents()
    CAS-->>CD: CrossAssetSummary

    CD->>CD: Render CorrelationMatrix
    CD->>CD: Render RegimeDivergenceList
```

---

## Trade Journal Import Flow

Sequence diagram showing how autotrader logs are imported and processed.

```mermaid
sequenceDiagram
    participant User
    participant UI as TradeLogging.razor
    participant Parser as DecisionMetadataParser
    participant Service as TradeLogService
    participant Storage as LocalStorageService

    User->>UI: Upload CSV/JSON file
    UI->>Parser: ParseJsonLog(content) or ParseCsvLog(content)
    Parser->>Parser: Extract trades & decisions
    Parser-->>UI: (List<OptionsLog>, List<TradeDecision>)
    UI->>Service: ImportTrades(trades, decisions)
    Service->>Storage: SetAsync("tradeLogs", trades)
    Service->>Storage: SetAsync("decisions", decisions)
    Storage-->>Service: Success
    Service-->>UI: Import complete
    UI-->>User: Show success message
```

**Key components:**
- **DecisionMetadataParser**: Handles both JSON and CSV formats
- **TradeLogService**: Manages CRUD operations and persistence
- **TradeDecision**: Metadata model with pattern tracking and confidence scores

---

## Trade Decision Context Flow

Graph showing how decision metadata enhances trade display.

```mermaid
graph TD
    A[Trade Entry] --> B{Has Decision Metadata?}
    B -->|Yes| C[Display PatternBadge]
    B -->|Yes| D[Display RegimeContextCard]
    B -->|Yes| E[Display DecisionTimeline]
    B -->|No| F[Show basic trade info only]
    C --> G[User views patterns]
    D --> G
    E --> G
    F --> G
```

**Decision metadata includes:**
- Active patterns (MECH, PROB, NARR taxonomy)
- Primary trigger signal
- Confidence score (0-1)
- Regime type (Positive γ / Negative γ)
- GEX level at entry
- IV level at entry
- Spot price
- Decision rationale text

---

## Rendering Notes

These diagrams render automatically in:
- **GitHub** - Native Mermaid support in markdown
- **VS Code** - With "Markdown Preview Mermaid Support" extension
- **Notion** - Paste as code block with `mermaid` language

For other environments, use the [Mermaid Live Editor](https://mermaid.live/) to export as SVG/PNG.
