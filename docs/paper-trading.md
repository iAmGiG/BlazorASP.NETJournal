# Paper Trading

The Paper Trading module (`/trading`) allows simulated trading without risking capital.

## Import Functionality

Import existing trades from JSON files using the **Import** button.

### Supported Formats

#### 1. GexVisor Native Format (JSON)

```json
[
  {
    "id": "guid",
    "createdAt": "2024-01-15T10:30:00Z",
    "direction": "Long",
    "entryPrice": 450.25,
    "targetPrice": 455.00,
    "stopLoss": 448.00,
    "exitDate": "2024-01-16T15:00:00Z",
    "exitPrice": 454.75,
    "exitReason": "Target",
    "tags": ["SPY", "breakout"],
    "notes": "Strong momentum"
  }
]
```

#### 2. Autogen-Trader Format (JSON)

```json
[
  {
    "trade_id": "12345",
    "symbol": "SPY",
    "entry_date": "2024-01-15",
    "entry_price": 450.25,
    "quantity": 100,
    "exit_date": "2024-01-16",
    "exit_price": 454.75,
    "exit_reason": "take_profit",
    "initial_stop_loss": 448.00,
    "initial_take_profit": 455.00,
    "strategy_name": "momentum",
    "signal_strength": "high",
    "realized_pnl": 450.00
  }
]
```

### Import Workflow

1. Click **Import** button (Paper Trading page header)
2. Select JSON file (max 10 MB)
3. System auto-detects format:
   - Tries GexVisor format first (checks for `EntryPrice` field)
   - Falls back to autogen-trader format
4. Imports trades with new GUIDs (prevents ID conflicts)
5. Shows confirmation: "Imported N trade(s)"

### Format Detection Logic

**Service:** `PaperTradeService.ImportFromJsonAsync()`

1. Attempts GexVisor format deserialization
2. If successful and `EntryPrice > 0`, imports directly
3. If fails, tries autogen-trader format via `ImportFromAutotraderAsync()`
4. Returns count of imported trades (0 if both fail)

### Direction Inference (Autogen-Trader)

For autogen-trader format, direction is inferred from P&L and price movement:

- Positive P&L + price increase = Long
- Positive P&L + price decrease = Short
- Negative P&L + price increase = Short
- Negative P&L + price decrease = Long
- Default: Long (if cannot infer)

### Exit Reason Mapping

| Autogen-Trader | GexVisor |
|----------------|----------|
| `take_profit`, `target` | `Target` |
| `stop_loss`, `stop` | `Stop` |
| `manual`, `manual_close` | `Manual` |
| `time`, `expiry`, `timeout` | `Time` |
| Other values | Preserved as-is |

## Features

- **Open/Closed trade tabs** - Separate views for active and completed trades
- **Tag-based filtering** - Organize trades by custom tags
- **Analytics** - Win rate, P&L statistics, tag performance breakdown
- **Export** - JSON and CSV export formats
- **GEX Visualizer integration** - Ctrl+Enter hotkey to create trade from current visualization

---

_Last Updated: 2026-01-24_
