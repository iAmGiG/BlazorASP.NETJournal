namespace GexVisor.Core;

/// <summary>
/// Real-time quote data for a symbol.
/// </summary>
public record Quote
{
    public required string Symbol { get; init; }
    public decimal Price { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercent { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal PreviousClose { get; init; }
    public long Volume { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Source { get; init; }
}

/// <summary>
/// OHLCV bar data for charting.
/// </summary>
public record OhlcvBar
{
    public required string Symbol { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
}

/// <summary>
/// Time intervals for OHLCV data.
/// </summary>
public enum BarTimeframe
{
    Minute1,
    Minute5,
    Minute15,
    Minute30,
    Hour1,
    Hour4,
    Day,
    Week,
    Month
}

/// <summary>
/// Market data provider types.
/// </summary>
public enum MarketDataProvider
{
    Alpaca,
    Finnhub,
    Polygon,
    AlphaVantage
}

/// <summary>
/// Result wrapper for market data operations.
/// </summary>
public record MarketDataResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public MarketDataProvider? Source { get; init; }

    public static MarketDataResult<T> Ok(T data, MarketDataProvider source) =>
        new() { Success = true, Data = data, Source = source };

    public static MarketDataResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Option contract type (call or put).
/// </summary>
public enum OptionType
{
    Call,
    Put
}

/// <summary>
/// Single option contract with pricing and Greeks.
/// </summary>
public record OptionContract
{
    // Identification
    public required string Symbol { get; init; }              // Underlying symbol (SPY)
    public required string ContractSymbol { get; init; }      // Full contract ID (SPY240119C00425000)
    public required decimal StrikePrice { get; init; }
    public required OptionType Type { get; init; }
    public required DateTime ExpirationDate { get; init; }
    public required DateTime TradingDate { get; init; }       // Date of quote

    // Pricing
    public decimal? Bid { get; init; }
    public decimal? Ask { get; init; }
    public decimal? Last { get; init; }
    public decimal? Mark { get; init; }
    public int? BidSize { get; init; }
    public int? AskSize { get; init; }

    // Volume & Interest
    public long? Volume { get; init; }
    public long? OpenInterest { get; init; }

    // Greeks (critical for GEX calculation)
    public decimal? Delta { get; init; }         // [-1, 1] for puts, [0, 1] for calls
    public decimal? Gamma { get; init; }         // [0, ∞) - always non-negative
    public decimal? Theta { get; init; }         // Usually negative (time decay)
    public decimal? Vega { get; init; }          // [0, ∞) - always non-negative
    public decimal? Rho { get; init; }           // Interest rate sensitivity
    public decimal? ImpliedVolatility { get; init; }  // [0.01, 5.0] typical range

    // Derived fields
    public decimal? MidPrice { get; init; }      // (Bid + Ask) / 2
    public decimal? BidAskSpread { get; init; }  // Ask - Bid

    // Metadata
    public MarketDataProvider? Source { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal DataQualityScore { get; init; } = 1.0m;
}

/// <summary>
/// Collection of option contracts for a symbol.
/// </summary>
public record OptionsChain
{
    public required string Symbol { get; init; }
    public DateTime? ExpirationDate { get; init; }      // Null = all expirations
    public required List<OptionContract> Contracts { get; init; }
    public DateTime Timestamp { get; init; }
    public MarketDataProvider? Source { get; init; }

    // Summary statistics
    public int TotalContracts => Contracts.Count;
    public int CallsCount => Contracts.Count(c => c.Type == OptionType.Call);
    public int PutsCount => Contracts.Count(c => c.Type == OptionType.Put);
    public List<DateTime> ExpirationDates => Contracts
        .Select(c => c.ExpirationDate.Date)
        .Distinct()
        .OrderBy(d => d)
        .ToList();
}

/// <summary>
/// Result wrapper for options chain operations.
/// </summary>
public record OptionsChainResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public MarketDataProvider? Source { get; init; }

    public static OptionsChainResult<T> Ok(T data, MarketDataProvider source) =>
        new() { Success = true, Data = data, Source = source };

    public static OptionsChainResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// GEX regime classification based on net gamma exposure.
/// </summary>
public enum GexRegime
{
    /// <summary>Dealers short gamma - hedging dampens volatility.</summary>
    LongGamma,
    /// <summary>Dealers long gamma - hedging amplifies volatility.</summary>
    ShortGamma,
    /// <summary>Balanced gamma exposure.</summary>
    Neutral
}

/// <summary>
/// Gamma exposure at a single strike price.
/// </summary>
public record StrikeGamma
{
    public required decimal StrikePrice { get; init; }
    public required decimal CallGex { get; init; }
    public required decimal PutGex { get; init; }
    public decimal NetGex => CallGex - PutGex;

    // For visualization
    public int ContractsCount { get; init; }
    public decimal TotalOpenInterest { get; init; }
}

/// <summary>
/// Complete GEX calculation result for a symbol.
/// </summary>
public record GexCalculationResult
{
    public required string Symbol { get; init; }
    public required decimal SpotPrice { get; init; }
    public required decimal TotalGex { get; init; }
    public decimal CallGex { get; init; }
    public decimal PutGex { get; init; }

    /// <summary>Flip point where net GEX = 0.</summary>
    public decimal? ZeroGammaLevel { get; init; }

    public required GexRegime Regime { get; init; }
    public required List<StrikeGamma> StrikeGammas { get; init; }

    // Metadata
    public DateTime Timestamp { get; init; }
    public MarketDataProvider? Source { get; init; }

    // Summary statistics
    public int TotalContracts => StrikeGammas.Sum(s => s.ContractsCount);
    public decimal AvgStrikeSpacing => StrikeGammas.Count > 1
        ? (StrikeGammas.Max(s => s.StrikePrice) - StrikeGammas.Min(s => s.StrikePrice)) / (StrikeGammas.Count - 1)
        : 0m;
}

/// <summary>
/// Classification of gamma wall behavior based on dealer hedging dynamics.
/// </summary>
public enum GammaWallType
{
    /// <summary>Strong positive gamma - dealers buy dips, acts as support.</summary>
    Support,

    /// <summary>Strong negative gamma - dealers sell rallies, acts as resistance.</summary>
    Resistance,

    /// <summary>Balanced gamma exposure - potential regime flip point.</summary>
    FlipPoint,
}

/// <summary>
/// Significant gamma concentration at a strike price.
/// Gamma walls act as support/resistance levels due to dealer hedging behavior.
/// </summary>
public record GammaWall
{
    /// <summary>Strike price where gamma is concentrated.</summary>
    public required decimal StrikePrice { get; init; }

    /// <summary>Net GEX at this strike (Call GEX - Put GEX).</summary>
    public required decimal NetGex { get; init; }

    /// <summary>Percentage of total chain GEX at this strike.</summary>
    public required decimal GexConcentrationPercent { get; init; }

    /// <summary>Type of wall based on net gamma direction.</summary>
    public required GammaWallType WallType { get; init; }

    /// <summary>Distance from current spot price in dollars.</summary>
    public decimal DistanceFromSpot { get; init; }

    /// <summary>Distance from spot as percentage.</summary>
    public decimal DistancePercent { get; init; }

    /// <summary>Estimated price magnetism strength (0-1). Higher = stronger pull.</summary>
    public decimal MagnetismScore { get; init; }
}

/// <summary>
/// Enhanced GEX analysis with gamma wall detection for support/resistance levels.
/// </summary>
public record GammaWallAnalysis
{
    /// <summary>Primary support levels (strongest positive gamma below spot).</summary>
    public required IReadOnlyList<GammaWall> SupportLevels { get; init; }

    /// <summary>Primary resistance levels (strongest negative gamma above spot).</summary>
    public required IReadOnlyList<GammaWall> ResistanceLevels { get; init; }

    /// <summary>Flip points where net GEX crosses zero.</summary>
    public required IReadOnlyList<GammaWall> FlipPoints { get; init; }

    /// <summary>Maximum positive gamma strike (strongest support).</summary>
    public GammaWall? MaxPositiveGammaStrike { get; init; }

    /// <summary>Maximum negative gamma strike (strongest resistance).</summary>
    public GammaWall? MaxNegativeGammaStrike { get; init; }

    /// <summary>Net gamma exposure above spot (upside exposure).</summary>
    public decimal GexAboveSpot { get; init; }

    /// <summary>Net gamma exposure below spot (downside exposure).</summary>
    public decimal GexBelowSpot { get; init; }

    /// <summary>Asymmetry ratio: (Above - Below) / (|Above| + |Below|). Range: -1 to +1.</summary>
    public decimal GexAsymmetry { get; init; }

    /// <summary>Timestamp of analysis.</summary>
    public DateTime Timestamp { get; init; }
}
