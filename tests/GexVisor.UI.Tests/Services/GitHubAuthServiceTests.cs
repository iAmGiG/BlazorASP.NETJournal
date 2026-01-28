// Copyright (c) GexVisor. All rights reserved.

using System.Net;
using System.Text.Json;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;
using Moq.Protected;

namespace GexVisor.UI.Tests.Services;

public class GitHubAuthServiceTests : IDisposable
{
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GitHubAuthService _service;

    public GitHubAuthServiceTests()
    {
        _mockStorage = new Mock<ILocalStorageService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost"),
        };
        _service = new GitHubAuthService(_httpClient, _mockStorage.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task StartDeviceFlowAsync_UsesCorrectProxyUrl()
    {
        // Arrange
        var expectedUrl = AppConstants.GitHub.Proxy.DeviceCodeUrl;
        var response = new DeviceCodeResponse
        {
            DeviceCode = "test-code",
            UserCode = "1234",
            VerificationUri = "http://github.com/login",
            ExpiresIn = 900,
            Interval = 5,
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(expectedUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response)),
            });

        // Act
        await _service.StartDeviceFlowAsync();

        // Assert
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(expectedUrl)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RefreshTokenAsync_UsesCorrectProxyUrl()
    {
        // Arrange
        var refreshUrl = AppConstants.GitHub.Proxy.RefreshTokenUrl;
        var userUrl = AppConstants.GitHub.Proxy.UserUrl;

        // Setup initial state with a refresh token
        var state = new GitHubAuthState
        {
            AccessToken = "old-token",
            RefreshToken = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
        };

        _mockStorage.Setup(s => s.GetAsync<GitHubAuthState>(AppConstants.Storage.GitHubAuth))
            .ReturnsAsync(state);

        // Load the state into the service
        await _service.LoadAsync();

        var tokenResponse = new TokenResponse
        {
            AccessToken = "new-token",
            TokenType = "bearer",
            Scope = "repo",
            ExpiresIn = 3600,
        };

        var userResponse = new GitHubUser
        {
            Login = "testuser",
            AvatarUrl = "http://avatar.url",
        };

        // Setup Refresh Token Response
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(refreshUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse)),
            });

        // Setup User Info Response (called after successful refresh)
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(userUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(userResponse)),
            });

        // Act
        await _service.RefreshTokenAsync();

        // Assert
        // Verify Refresh Token URL was called (may be called multiple times due to retry logic)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(refreshUrl)),
            ItExpr.IsAny<CancellationToken>());

        // Verify User Info URL was called (happens automatically after successful auth/refresh)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(userUrl)),
            ItExpr.IsAny<CancellationToken>());
    }
}
