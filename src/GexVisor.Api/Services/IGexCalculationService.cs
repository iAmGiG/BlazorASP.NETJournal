using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for calculating GEX (Gamma Exposure) metrics from options chain data.
/// </summary>
public interface IGexCalculationService
{
    /// <summary>
    /// Calculate GEX metrics for a symbol at specified spot price.
    /// </summary>
    /// <param name="symbol">Underlying symbol (e.g., "SPY")</param>
    /// <param name="spotPrice">Current spot price for S² calculation</param>
    /// <returns>GEX calculation result with strike gammas and regime</returns>
    Task<OptionsChainResult<GexCalculationResult>> CalculateGexAsync(string symbol, decimal spotPrice);

    /// <summary>
    /// Calculate GEX metrics for a symbol (fetches current spot price automatically).
    /// </summary>
    /// <param name="symbol">Underlying symbol (e.g., "SPY")</param>
    /// <returns>GEX calculation result with strike gammas and regime</returns>
    Task<OptionsChainResult<GexCalculationResult>> CalculateGexAsync(string symbol);

    /// <summary>
    /// Analyze gamma walls and flip points for a symbol (fetches current spot price automatically).
    /// </summary>
    /// <param name="symbol">Underlying symbol (e.g., "SPY")</param>
    /// <returns>Gamma wall analysis with support/resistance levels</returns>
    Task<OptionsChainResult<GammaWallAnalysis>> AnalyzeGammaWallsAsync(string symbol);

    /// <summary>
    /// Analyze gamma walls and flip points at a specific spot price.
    /// </summary>
    /// <param name="symbol">Underlying symbol (e.g., "SPY")</param>
    /// <param name="spotPrice">Spot price for wall analysis</param>
    /// <returns>Gamma wall analysis with support/resistance levels</returns>
    Task<OptionsChainResult<GammaWallAnalysis>> AnalyzeGammaWallsAsync(string symbol, decimal spotPrice);
}
