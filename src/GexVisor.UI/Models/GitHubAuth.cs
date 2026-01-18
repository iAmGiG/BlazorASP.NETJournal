using System.Text.Json.Serialization;

namespace GexVisor.UI.Models;

/// <summary>
/// GitHub OAuth access token and user information.
/// </summary>
public class GitHubAuthState
{
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public string? Scope { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // User info (populated after auth)
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
    public bool NeedsRefresh => IsExpired && !string.IsNullOrEmpty(RefreshToken);
}

/// <summary>
/// Response from GitHub device code request.
/// </summary>
public class DeviceCodeResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = "";

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = "";

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 5;
}

/// <summary>
/// Response from GitHub token request.
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; set; }

    // Error fields
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    public bool IsSuccess => !string.IsNullOrEmpty(AccessToken);
    public bool IsPending => Error == "authorization_pending";
    public bool IsSlowDown => Error == "slow_down";
    public bool IsExpired => Error == "expired_token";
    public bool IsAccessDenied => Error == "access_denied";
}

/// <summary>
/// GitHub user info from API.
/// </summary>
public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Configuration for GitHub App OAuth.
/// Uses local API proxy to avoid CORS issues with GitHub's OAuth endpoints.
/// </summary>
public class GitHubAppConfig
{
    public string ClientId { get; set; } = "";
    public string AppId { get; set; } = "";

    // Proxy URLs (relative to app origin)
    public const string DeviceCodeUrl = "/api/github/device/code";
    public const string TokenUrl = "/api/github/device/token";
    public const string RefreshTokenUrl = "/api/github/token/refresh";
    public const string UserUrl = "/api/github/user";
    public const string GraphQLUrl = "/api/github/graphql";
}
