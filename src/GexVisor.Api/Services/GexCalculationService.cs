using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for calculating GEX (Gamma Exposure) metrics from options chain data.
/// Implements formulas ported from autogen-trader Python reference.
/// </summary>
public class GexCalculationService : IGexCalculationService
{
    private readonly IOptionsChainService _optionsService;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<GexCalculationService> _logger;

    // Constants (from autogen-trader reference)
    private const decimal ContractMultiplier = 100m;  // Standard options contract size
    private const decimal RegimeNeutralThreshold = 0.1m;  // +/-10% range for neutral classification

    // Gamma wall detection constants
    private const int MaxWallsReturned = 5;  // Top 5 support/resistance levels

    public GexCalculationService(
        IOptionsChainService optionsService,
        IMarketDataService marketDataService,
        ILogger<GexCalculationService> logger)
    {
        _optionsService = optionsService;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<OptionsChainResult<GexCalculationResult>> CalculateGexAsync(string symbol)
    {
        // Fetch current spot price
        var quoteResult = await _marketDataService.GetQuoteAsync(symbol.ToUpperInvariant());
        if (!quoteResult.Success || quoteResult.Data == null)
        {
            _logger.LogWarning("Failed to fetch spot price for {Symbol}", symbol);
            return OptionsChainResult<GexCalculationResult>.Fail(
                $"Failed to fetch spot price for {symbol}: {quoteResult.Error}");
        }

        return await CalculateGexAsync(symbol, quoteResult.Data.Price);
    }

    public async Task<OptionsChainResult<GexCalculationResult>> CalculateGexAsync(string symbol, decimal spotPrice)
    {
        symbol = symbol.ToUpperInvariant();

        // 1. Fetch options chain (all expirations)
        var chainResult = await _optionsService.GetChainAsync(symbol, expirationDate: null);
        if (!chainResult.Success || chainResult.Data == null)
        {
            _logger.LogWarning("Failed to fetch options chain for {Symbol}", symbol);
            return OptionsChainResult<GexCalculationResult>.Fail(
                $"Failed to fetch options chain for {symbol}: {chainResult.Error}");
        }

        var chain = chainResult.Data;

        if (chain.Contracts.Count == 0)
        {
            return OptionsChainResult<GexCalculationResult>.Fail(
                $"No contracts found in options chain for {symbol}");
        }

        // 2. Group contracts by strike price, filtering for valid gamma/OI
        var strikeGroups = chain.Contracts
            .Where(c => c.Gamma.HasValue && c.Gamma > 0 && c.OpenInterest.HasValue && c.OpenInterest > 0)
            .GroupBy(c => c.StrikePrice)
            .OrderBy(g => g.Key);

        // 3. Calculate GEX for each strike
        var spotSquared = spotPrice * spotPrice;
        var strikeGammas = new List<StrikeGamma>();

        foreach (var group in strikeGroups)
        {
            var strike = group.Key;

            // Separate calls and puts
            var calls = group.Where(c => c.Type == OptionType.Call).ToList();
            var puts = group.Where(c => c.Type == OptionType.Put).ToList();

            // GEX formula: gamma x OI x 100 x S^2
            // Call GEX is positive (dealers short calls -> long stock as delta increases)
            var callGex = calls.Sum(c =>
                c.Gamma!.Value * c.OpenInterest!.Value * ContractMultiplier * spotSquared);

            // Put GEX is negative (dealers short puts -> short stock as delta decreases)
            // Note: We keep it positive here and subtract in total calculation
            var putGex = puts.Sum(p =>
                p.Gamma!.Value * p.OpenInterest!.Value * ContractMultiplier * spotSquared);

            strikeGammas.Add(new StrikeGamma
            {
                StrikePrice = strike,
                CallGex = callGex,
                PutGex = putGex,
                ContractsCount = group.Count(),
                TotalOpenInterest = group.Sum(c => c.OpenInterest ?? 0)
            });
        }

        if (strikeGammas.Count == 0)
        {
            return OptionsChainResult<GexCalculationResult>.Fail(
                $"No valid contracts with gamma/OI for {symbol}");
        }

        // 4. Calculate totals
        var totalCallGex = strikeGammas.Sum(s => s.CallGex);
        var totalPutGex = strikeGammas.Sum(s => s.PutGex);
        var totalGex = totalCallGex - totalPutGex;

        // 5. Find zero-gamma level (interpolate between strikes)
        var zeroGammaLevel = FindZeroGammaLevel(strikeGammas, spotPrice);

        // 6. Classify regime
        var regime = ClassifyRegime(totalGex, totalCallGex, totalPutGex);

        _logger.LogInformation(
            "Calculated GEX for {Symbol}: Total={Total:N0}, Regime={Regime}, ZeroGamma={ZeroGamma:F2}, Strikes={StrikeCount}",
            symbol, totalGex, regime, zeroGammaLevel, strikeGammas.Count);

        var result = new GexCalculationResult
        {
            Symbol = symbol,
            SpotPrice = spotPrice,
            TotalGex = totalGex,
            CallGex = totalCallGex,
            PutGex = totalPutGex,
            ZeroGammaLevel = zeroGammaLevel,
            Regime = regime,
            StrikeGammas = strikeGammas,
            Timestamp = DateTime.UtcNow,
            Source = chainResult.Source
        };

        return OptionsChainResult<GexCalculationResult>.Ok(result, chainResult.Source ?? MarketDataProvider.AlphaVantage);
    }

    /// <summary>
    /// Find the strike price where net GEX crosses zero (flip point).
    /// Uses linear interpolation between adjacent strikes.
    /// </summary>
    private decimal? FindZeroGammaLevel(List<StrikeGamma> strikes, decimal spotPrice)
    {
        if (strikes.Count < 2)
        {
            return null;
        }

        // Find strikes where net GEX crosses zero
        for (int i = 0; i < strikes.Count - 1; i++)
        {
            var current = strikes[i];
            var next = strikes[i + 1];

            var currentNet = current.NetGex;
            var nextNet = next.NetGex;

            // Check for sign change (zero crossing)
            if ((currentNet >= 0 && nextNet < 0) || (currentNet < 0 && nextNet >= 0))
            {
                // Linear interpolation to find exact crossing point
                var range = next.StrikePrice - current.StrikePrice;
                var gexRange = nextNet - currentNet;

                if (gexRange != 0)
                {
                    var fraction = -currentNet / gexRange;
                    var zeroLevel = current.StrikePrice + (range * fraction);

                    _logger.LogDebug(
                        "Found zero-gamma level at {ZeroLevel:F2} between strikes {Current} and {Next}",
                        zeroLevel, current.StrikePrice, next.StrikePrice);

                    return zeroLevel;
                }
            }
        }

        // No zero crossing found - check if spot price is within strike range
        var minStrike = strikes.Min(s => s.StrikePrice);
        var maxStrike = strikes.Max(s => s.StrikePrice);

        if (spotPrice >= minStrike && spotPrice <= maxStrike)
        {
            // Return spot price as approximation when no clear crossing
            _logger.LogDebug("No zero-gamma crossing found, using spot price {SpotPrice} as approximation", spotPrice);
            return spotPrice;
        }

        return null;
    }

    /// <summary>
    /// Classify market regime based on net GEX.
    /// </summary>
    private GexRegime ClassifyRegime(decimal totalGex, decimal callGex, decimal putGex)
    {
        var maxGex = Math.Max(Math.Abs(callGex), Math.Abs(putGex));

        if (maxGex == 0)
        {
            return GexRegime.Neutral;
        }

        var gexRatio = totalGex / maxGex;

        // Regime classification:
        // - LongGamma: Dealers are short gamma (net positive GEX) - hedging dampens volatility
        // - ShortGamma: Dealers are long gamma (net negative GEX) - hedging amplifies volatility
        // - Neutral: Balanced exposure

        if (gexRatio > RegimeNeutralThreshold)
        {
            return GexRegime.LongGamma;
        }
        else if (gexRatio < -RegimeNeutralThreshold)
        {
            return GexRegime.ShortGamma;
        }
        else
        {
            return GexRegime.Neutral;
        }
    }

    public async Task<OptionsChainResult<GammaWallAnalysis>> AnalyzeGammaWallsAsync(string symbol)
    {
        var quoteResult = await _marketDataService.GetQuoteAsync(symbol.ToUpperInvariant());
        if (!quoteResult.Success || quoteResult.Data == null)
        {
            return OptionsChainResult<GammaWallAnalysis>.Fail(
                $"Failed to fetch spot price for {symbol}: {quoteResult.Error}");
        }

        return await AnalyzeGammaWallsAsync(symbol, quoteResult.Data.Price);
    }

    public async Task<OptionsChainResult<GammaWallAnalysis>> AnalyzeGammaWallsAsync(string symbol, decimal spotPrice)
    {
        // First calculate base GEX
        var gexResult = await CalculateGexAsync(symbol, spotPrice);
        if (!gexResult.Success || gexResult.Data == null)
        {
            return OptionsChainResult<GammaWallAnalysis>.Fail(gexResult.Error ?? "GEX calculation failed");
        }

        var analysis = AnalyzeWalls(gexResult.Data, spotPrice);

        _logger.LogInformation(
            "Analyzed gamma walls for {Symbol}: Support={SupportCount}, Resistance={ResistanceCount}, FlipPoints={FlipCount}, Asymmetry={Asymmetry:F2}",
            symbol, analysis.SupportLevels.Count, analysis.ResistanceLevels.Count,
            analysis.FlipPoints.Count, analysis.GexAsymmetry);

        return OptionsChainResult<GammaWallAnalysis>.Ok(analysis, gexResult.Source ?? MarketDataProvider.AlphaVantage);
    }

    /// <summary>
    /// Analyze gamma walls from GEX calculation result.
    /// </summary>
    private GammaWallAnalysis AnalyzeWalls(GexCalculationResult gexData, decimal spotPrice)
    {
        var strikes = gexData.StrikeGammas;
        var totalAbsGex = strikes.Sum(s => Math.Abs(s.NetGex));

        // Convert strikes to gamma walls with calculations
        var walls = strikes.Select(s => new GammaWall
        {
            StrikePrice = s.StrikePrice,
            NetGex = s.NetGex,
            GexConcentrationPercent = totalAbsGex > 0 ? Math.Abs(s.NetGex) / totalAbsGex * 100 : 0,
            WallType = ClassifyWallType(s.NetGex, s.StrikePrice, spotPrice),
            DistanceFromSpot = s.StrikePrice - spotPrice,
            DistancePercent = spotPrice > 0 ? (s.StrikePrice - spotPrice) / spotPrice * 100 : 0,
            MagnetismScore = CalculateMagnetism(s, spotPrice, totalAbsGex),
        }).ToList();

        // Categorize walls - support is positive gamma BELOW spot
        var supportLevels = walls
            .Where(w => w.WallType == GammaWallType.Support)
            .OrderByDescending(w => Math.Abs(w.NetGex))
            .Take(MaxWallsReturned)
            .ToList();

        // Resistance is negative gamma ABOVE spot
        var resistanceLevels = walls
            .Where(w => w.WallType == GammaWallType.Resistance)
            .OrderByDescending(w => Math.Abs(w.NetGex))
            .Take(MaxWallsReturned)
            .ToList();

        // Find flip points
        var flipPoints = FindFlipPoints(strikes, spotPrice);

        // Calculate asymmetry
        var gexAbove = strikes.Where(s => s.StrikePrice > spotPrice).Sum(s => s.NetGex);
        var gexBelow = strikes.Where(s => s.StrikePrice <= spotPrice).Sum(s => s.NetGex);
        var totalGex = Math.Abs(gexAbove) + Math.Abs(gexBelow);
        var asymmetry = totalGex > 0 ? (gexAbove - gexBelow) / totalGex : 0;

        return new GammaWallAnalysis
        {
            SupportLevels = supportLevels,
            ResistanceLevels = resistanceLevels,
            FlipPoints = flipPoints,
            MaxPositiveGammaStrike = walls.Where(w => w.NetGex > 0).MaxBy(w => w.NetGex),
            MaxNegativeGammaStrike = walls.Where(w => w.NetGex < 0).MinBy(w => w.NetGex),
            GexAboveSpot = gexAbove,
            GexBelowSpot = gexBelow,
            GexAsymmetry = asymmetry,
            Timestamp = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Classify wall type based on net GEX and position relative to spot.
    /// </summary>
    private static GammaWallType ClassifyWallType(decimal netGex, decimal strikePrice, decimal spotPrice)
    {
        // Support: positive gamma below spot (dealers buy dips)
        if (netGex > 0 && strikePrice < spotPrice)
        {
            return GammaWallType.Support;
        }

        // Resistance: negative gamma above spot (dealers sell rallies)
        if (netGex < 0 && strikePrice > spotPrice)
        {
            return GammaWallType.Resistance;
        }

        // Flip point for balanced gamma or opposite direction
        return GammaWallType.FlipPoint;
    }

    /// <summary>
    /// Find flip points where net GEX crosses zero.
    /// </summary>
    private List<GammaWall> FindFlipPoints(List<StrikeGamma> strikes, decimal spotPrice)
    {
        var flipPoints = new List<GammaWall>();

        if (strikes.Count < 2)
        {
            return flipPoints;
        }

        var sortedStrikes = strikes.OrderBy(s => s.StrikePrice).ToList();

        for (int i = 0; i < sortedStrikes.Count - 1; i++)
        {
            var current = sortedStrikes[i];
            var next = sortedStrikes[i + 1];

            // Check for sign change (zero crossing)
            if ((current.NetGex >= 0 && next.NetGex < 0) ||
                (current.NetGex < 0 && next.NetGex >= 0))
            {
                // Interpolate flip point
                var range = next.StrikePrice - current.StrikePrice;
                var gexRange = next.NetGex - current.NetGex;

                if (gexRange != 0)
                {
                    var fraction = -current.NetGex / gexRange;
                    var flipStrike = current.StrikePrice + (range * fraction);

                    flipPoints.Add(new GammaWall
                    {
                        StrikePrice = flipStrike,
                        NetGex = 0,
                        GexConcentrationPercent = 0,
                        WallType = GammaWallType.FlipPoint,
                        DistanceFromSpot = flipStrike - spotPrice,
                        DistancePercent = spotPrice > 0 ? (flipStrike - spotPrice) / spotPrice * 100 : 0,
                        MagnetismScore = 0,
                    });
                }
            }
        }

        return flipPoints;
    }

    /// <summary>
    /// Calculate magnetism score (0-1) based on concentration and proximity.
    /// </summary>
    private static decimal CalculateMagnetism(
        StrikeGamma strike,
        decimal spotPrice,
        decimal totalAbsGex)
    {
        if (totalAbsGex == 0 || spotPrice == 0)
        {
            return 0;
        }

        var concentration = Math.Abs(strike.NetGex) / totalAbsGex;
        var distance = Math.Abs(strike.StrikePrice - spotPrice);
        var proximity = 1 - Math.Min(distance / spotPrice, 1);

        // Magnetism is higher when concentration is high and distance is small
        return Math.Min(concentration * proximity * 2, 1);
    }
}
