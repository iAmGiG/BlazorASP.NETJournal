using GexVisor.Core;

namespace GexVisor.Api.Services;

/// <summary>
/// Service for fetching options chain data from market data providers.
/// </summary>
public interface IOptionsChainService
{
    /// <summary>
    /// Get full options chain for symbol (all expirations or specific date).
    /// </summary>
    /// <param name="symbol">Underlying symbol (e.g., "SPY")</param>
    /// <param name="expirationDate">Optional expiration date filter</param>
    /// <returns>Options chain with contracts</returns>
    Task<OptionsChainResult<OptionsChain>> GetChainAsync(
        string symbol,
        DateTime? expirationDate = null);

    /// <summary>
    /// Get specific option contract by parameters.
    /// </summary>
    /// <param name="symbol">Underlying symbol</param>
    /// <param name="strikePrice">Strike price</param>
    /// <param name="type">Call or Put</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <returns>Single option contract</returns>
    Task<OptionsChainResult<OptionContract>> GetContractAsync(
        string symbol,
        decimal strikePrice,
        OptionType type,
        DateTime expirationDate);

    /// <summary>
    /// Get available expiration dates for a symbol.
    /// </summary>
    /// <param name="symbol">Underlying symbol</param>
    /// <returns>List of available expiration dates</returns>
    Task<OptionsChainResult<List<DateTime>>> GetExpirationDatesAsync(string symbol);
}
