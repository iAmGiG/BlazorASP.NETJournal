using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for GitHub OAuth authentication using Device Flow.
/// Device Flow is ideal for client-side apps as it doesn't expose the client secret.
/// </summary>
public class GitHubAuthService
{
    private readonly HttpClient _http;
    private readonly LocalStorageService _storage;
    private readonly GitHubAppConfig _config;

    private GitHubAuthState _state = new();
    private CancellationTokenSource? _pollCts;

    public event Action? OnAuthStateChanged;
    public event Action<string>? OnAuthError;
    public event Action<DeviceCodeResponse>? OnDeviceCodeReceived;

    public GitHubAuthState State => _state;
    public bool IsAuthenticated => _state.IsAuthenticated && !_state.IsExpired;
    public string? Username => _state.Username;

    public GitHubAuthService(HttpClient http, LocalStorageService storage)
    {
        _http = http;
        _storage = storage;

        // GitHub App config (Client ID is not secret)
        _config = new GitHubAppConfig
        {
            ClientId = "Iv23li5YORDVpgCnEURy",
            AppId = "2678638"
        };
    }

    /// <summary>
    /// Load saved auth state from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<GitHubAuthState>(StorageKeys.GitHubAuth);
        if (stored != null)
        {
            _state = stored;

            // If token is expired but we have a refresh token, try to refresh
            if (_state.NeedsRefresh)
            {
                await RefreshTokenAsync();
            }

            OnAuthStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Start the Device Flow authentication process.
    /// </summary>
    public async Task<DeviceCodeResponse?> StartDeviceFlowAsync()
    {
        try
        {
            // Cancel any existing poll
            _pollCts?.Cancel();

            // Use proxy endpoint (handles CORS)
            var response = await _http.PostAsync(GitHubAppConfig.DeviceCodeUrl, null);
            response.EnsureSuccessStatusCode();

            var deviceCode = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
            if (deviceCode != null)
            {
                OnDeviceCodeReceived?.Invoke(deviceCode);

                // Start polling for the token
                _ = PollForTokenAsync(deviceCode);
            }

            return deviceCode;
        }
        catch (Exception ex)
        {
            OnAuthError?.Invoke($"Failed to start auth: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Poll GitHub for the access token after user authorizes.
    /// </summary>
    private async Task PollForTokenAsync(DeviceCodeResponse deviceCode)
    {
        _pollCts = new CancellationTokenSource();
        var interval = deviceCode.Interval;
        var expiresAt = DateTime.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        try
        {
            while (!_pollCts.Token.IsCancellationRequested && DateTime.UtcNow < expiresAt)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), _pollCts.Token);

                var tokenResponse = await RequestTokenAsync(deviceCode.DeviceCode);

                if (tokenResponse.IsSuccess)
                {
                    await HandleSuccessfulAuthAsync(tokenResponse);
                    return;
                }
                else if (tokenResponse.IsSlowDown)
                {
                    interval += 5; // Back off as requested
                }
                else if (tokenResponse.IsPending)
                {
                    // Keep polling
                    continue;
                }
                else if (tokenResponse.IsExpired)
                {
                    OnAuthError?.Invoke("Authorization expired. Please try again.");
                    return;
                }
                else if (tokenResponse.IsAccessDenied)
                {
                    OnAuthError?.Invoke("Access denied. Please authorize the app.");
                    return;
                }
                else if (!string.IsNullOrEmpty(tokenResponse.Error))
                {
                    OnAuthError?.Invoke(tokenResponse.ErrorDescription ?? tokenResponse.Error);
                    return;
                }
            }

            if (DateTime.UtcNow >= expiresAt)
            {
                OnAuthError?.Invoke("Authorization timed out. Please try again.");
            }
        }
        catch (TaskCanceledException)
        {
            // Cancelled, ignore
        }
        catch (Exception ex)
        {
            OnAuthError?.Invoke($"Auth error: {ex.Message}");
        }
    }

    /// <summary>
    /// Request access token from GitHub via proxy.
    /// </summary>
    private async Task<TokenResponse> RequestTokenAsync(string deviceCode)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["device_code"] = deviceCode
        });

        var response = await _http.PostAsync(GitHubAppConfig.TokenUrl, content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TokenResponse>(json) ?? new TokenResponse { Error = "parse_error" };
    }

    /// <summary>
    /// Handle successful authentication.
    /// </summary>
    private async Task HandleSuccessfulAuthAsync(TokenResponse tokenResponse)
    {
        _state = new GitHubAuthState
        {
            AccessToken = tokenResponse.AccessToken,
            TokenType = tokenResponse.TokenType,
            Scope = tokenResponse.Scope,
            ExpiresAt = tokenResponse.ExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                : null,
            RefreshToken = tokenResponse.RefreshToken,
            RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(tokenResponse.RefreshTokenExpiresIn.Value)
                : null
        };

        // Fetch user info
        await FetchUserInfoAsync();

        // Save to localStorage
        await _storage.SetAsync(StorageKeys.GitHubAuth, _state);

        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Fetch authenticated user's info from GitHub API via proxy.
    /// </summary>
    private async Task FetchUserInfoAsync()
    {
        if (string.IsNullOrEmpty(_state.AccessToken)) return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GitHubAppConfig.UserUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.AccessToken);

            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<GitHubUser>();
                if (user != null)
                {
                    _state.Username = user.Login;
                    _state.AvatarUrl = user.AvatarUrl;
                }
            }
        }
        catch
        {
            // Non-critical, ignore
        }
    }

    /// <summary>
    /// Refresh the access token using the refresh token via proxy.
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_state.RefreshToken)) return false;

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = _state.RefreshToken
            });

            var response = await _http.PostAsync(GitHubAppConfig.RefreshTokenUrl, content);
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse?.IsSuccess == true)
            {
                await HandleSuccessfulAuthAsync(tokenResponse);
                return true;
            }
        }
        catch
        {
            // Refresh failed
        }

        return false;
    }

    /// <summary>
    /// Sign out and clear stored auth state.
    /// </summary>
    public async Task SignOutAsync()
    {
        _pollCts?.Cancel();
        _state = new GitHubAuthState();
        await _storage.RemoveAsync(StorageKeys.GitHubAuth);
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Cancel any pending auth flow.
    /// </summary>
    public void CancelAuth()
    {
        _pollCts?.Cancel();
    }

    /// <summary>
    /// Get the authorization header for API requests.
    /// </summary>
    public AuthenticationHeaderValue? GetAuthHeader()
    {
        if (!IsAuthenticated) return null;
        return new AuthenticationHeaderValue("Bearer", _state.AccessToken);
    }
}
