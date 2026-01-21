using GexVisor.Core;
using Xunit;

namespace GexVisor.Core.Tests;

/// <summary>
/// Unit tests for ApiConfiguration validation and provider detection.
/// </summary>
public class ApiConfigurationTests
{
    #region Validation Tests

    [Fact]
    public void Validate_NoProviders_ReturnsError()
    {
        // Arrange
        var config = new ApiConfiguration();

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("At least one market data provider", errors[0]);
    }

    [Fact]
    public void Validate_AlphaVantageOnly_NoErrors()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            AlphaVantageKey = "test-key"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FinnhubOnly_NoErrors()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            FinnhubKey = "test-key"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AlpacaComplete_NoErrors()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            AlpacaApiKey = "test-key",
            AlpacaSecret = "test-secret"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AlpacaKeyOnly_ReturnsError()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            AlpacaApiKey = "test-key"
            // Missing AlpacaSecret
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("secret is missing"));
    }

    [Fact]
    public void Validate_AlpacaSecretOnly_ReturnsError()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            AlpacaSecret = "test-secret"
            // Missing AlpacaApiKey
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("API key is missing"));
    }

    [Fact]
    public void Validate_MultipleProviders_NoErrors()
    {
        // Arrange
        var config = new ApiConfiguration
        {
            AlphaVantageKey = "av-key",
            FinnhubKey = "fh-key",
            AlpacaApiKey = "alpaca-key",
            AlpacaSecret = "alpaca-secret"
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Provider Detection Tests

    [Fact]
    public void HasAlphaVantage_WithKey_ReturnsTrue()
    {
        var config = new ApiConfiguration { AlphaVantageKey = "test" };
        Assert.True(config.HasAlphaVantage);
    }

    [Fact]
    public void HasAlphaVantage_WithoutKey_ReturnsFalse()
    {
        var config = new ApiConfiguration();
        Assert.False(config.HasAlphaVantage);
    }

    [Fact]
    public void HasAlphaVantage_WithWhitespace_ReturnsFalse()
    {
        var config = new ApiConfiguration { AlphaVantageKey = "   " };
        Assert.False(config.HasAlphaVantage);
    }

    [Fact]
    public void HasFinnhub_WithKey_ReturnsTrue()
    {
        var config = new ApiConfiguration { FinnhubKey = "test" };
        Assert.True(config.HasFinnhub);
    }

    [Fact]
    public void HasAlpaca_RequiresBothKeyAndSecret()
    {
        // Only key
        var config1 = new ApiConfiguration { AlpacaApiKey = "key" };
        Assert.False(config1.HasAlpaca);

        // Only secret
        var config2 = new ApiConfiguration { AlpacaSecret = "secret" };
        Assert.False(config2.HasAlpaca);

        // Both
        var config3 = new ApiConfiguration { AlpacaApiKey = "key", AlpacaSecret = "secret" };
        Assert.True(config3.HasAlpaca);
    }

    [Fact]
    public void HasPolygon_WithKey_ReturnsTrue()
    {
        var config = new ApiConfiguration { PolygonKey = "test" };
        Assert.True(config.HasPolygon);
    }

    [Fact]
    public void HasFred_WithKey_ReturnsTrue()
    {
        var config = new ApiConfiguration { FredApiKey = "test" };
        Assert.True(config.HasFred);
    }

    #endregion

    #region Rate Limits Constants

    [Fact]
    public void RateLimits_AlphaVantage_Is75()
    {
        Assert.Equal(75, ApiConfiguration.RateLimits.AlphaVantage);
    }

    [Fact]
    public void RateLimits_Finnhub_Is60()
    {
        Assert.Equal(60, ApiConfiguration.RateLimits.Finnhub);
    }

    [Fact]
    public void RateLimits_Alpaca_Is200()
    {
        Assert.Equal(200, ApiConfiguration.RateLimits.Alpaca);
    }

    [Fact]
    public void RateLimits_Polygon_Is5()
    {
        Assert.Equal(5, ApiConfiguration.RateLimits.Polygon);
    }

    #endregion
}
