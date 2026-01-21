using System.Net.Http.Headers;
using GexVisor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// API Configuration Service (loads API keys from config/config.json + environment)
builder.Services.AddSingleton<IApiConfigService, ApiConfigService>();

// Cache Service (SQLite-based, stores market data locally)
builder.Services.AddSingleton<ICacheService>(sp =>
{
    var logger = sp.GetService<ILogger<SqliteCacheService>>();
    return new SqliteCacheService(".cache/gexvisor.db", logger);
});
builder.Services.AddSingleton<MarketDataCacheService>();

// Market Data Service
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();

// Add services
builder.Services.AddHttpClient(); // Generic HttpClient for market data APIs

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5001",
                "http://localhost:5246",
                "https://localhost:7161",
                "http://localhost:10354")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("GitHub", client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GexVisor/1.0");
});

var app = builder.Build();

app.UseCors();

// GitHub OAuth proxy endpoints
var github = app.MapGroup("/api/github");

// Device Flow: Get device code
github.MapPost("/device/code", async (HttpContext ctx, IHttpClientFactory httpFactory, IConfiguration config) =>
{
    var clientId = config["GitHub:ClientId"] ?? "Iv23li5YORDVpgCnEURy";

    var http = httpFactory.CreateClient("GitHub");
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = clientId,
        ["scope"] = "read:project project repo"
    });

    var response = await http.PostAsync("https://github.com/login/device/code", content);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// Device Flow: Poll for token
github.MapPost("/device/token", async (HttpContext ctx, IHttpClientFactory httpFactory, IConfiguration config) =>
{
    var clientId = config["GitHub:ClientId"] ?? "Iv23li5YORDVpgCnEURy";

    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    var form = System.Web.HttpUtility.ParseQueryString(body);
    var deviceCode = form["device_code"];

    if (string.IsNullOrEmpty(deviceCode))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = "device_code required" });
        return;
    }

    var http = httpFactory.CreateClient("GitHub");
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = clientId,
        ["device_code"] = deviceCode,
        ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
    });

    var response = await http.PostAsync("https://github.com/login/oauth/access_token", content);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// Token refresh
github.MapPost("/token/refresh", async (HttpContext ctx, IHttpClientFactory httpFactory, IConfiguration config) =>
{
    var clientId = config["GitHub:ClientId"] ?? "Iv23li5YORDVpgCnEURy";

    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    var form = System.Web.HttpUtility.ParseQueryString(body);
    var refreshToken = form["refresh_token"];

    if (string.IsNullOrEmpty(refreshToken))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = "refresh_token required" });
        return;
    }

    var http = httpFactory.CreateClient("GitHub");
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = clientId,
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken
    });

    var response = await http.PostAsync("https://github.com/login/oauth/access_token", content);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// GraphQL proxy (for GitHub Projects API)
github.MapPost("/graphql", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var authHeader = ctx.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader))
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Authorization header required" });
        return;
    }

    var http = httpFactory.CreateClient("GitHub");

    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();

    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
    request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
    request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

    var response = await http.SendAsync(request);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// REST API proxy (for user info etc)
github.MapGet("/user", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var authHeader = ctx.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader))
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Authorization header required" });
        return;
    }

    var http = httpFactory.CreateClient("GitHub");

    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
    request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);

    var response = await http.SendAsync(request);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// Debug: Test projects query
github.MapGet("/debug/projects", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    var authHeader = ctx.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader))
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Authorization header required" });
        return;
    }

    var http = httpFactory.CreateClient("GitHub");

    var query = @"{""query"": ""{ viewer { login projectsV2(first: 20) { nodes { id title url closed } } } }""}";

    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
    request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
    request.Content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");

    var response = await http.SendAsync(request);
    var json = await response.Content.ReadAsStringAsync();

    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = (int)response.StatusCode;
    await ctx.Response.WriteAsync(json);
});

// API Configuration status endpoint
var config = app.MapGroup("/api/config");

config.MapGet("/status", (IApiConfigService configService) =>
{
    var cfg = configService.Configuration;
    return Results.Ok(new
    {
        isConfigured = configService.IsConfigured,
        providers = new
        {
            alphaVantage = cfg.HasAlphaVantage,
            finnhub = cfg.HasFinnhub,
            alpaca = cfg.HasAlpaca,
            polygon = cfg.HasPolygon,
            fred = cfg.HasFred
        },
        errors = configService.ValidationErrors
    });
});

// Market Data API endpoints
var market = app.MapGroup("/api/market");

// Get quote for a single symbol
market.MapGet("/quote/{symbol}", async (string symbol, IMarketDataService marketService) =>
{
    var result = await marketService.GetQuoteAsync(symbol.ToUpperInvariant());
    return result.Success
        ? Results.Ok(result.Data)
        : Results.NotFound(new { error = result.Error });
});

// Get quotes for multiple symbols (comma-separated)
market.MapGet("/quotes", async (string symbols, IMarketDataService marketService) =>
{
    var symbolList = symbols.ToUpperInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries);
    var result = await marketService.GetMultipleQuotesAsync(symbolList);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.NotFound(new { error = result.Error });
});

// Get OHLCV bars for a symbol
market.MapGet("/bars/{symbol}", async (
    string symbol,
    string? timeframe,
    int? limit,
    IMarketDataService marketService) =>
{
    var tf = timeframe?.ToLowerInvariant() switch
    {
        "1m" or "1min" => GexVisor.Core.BarTimeframe.Minute1,
        "5m" or "5min" => GexVisor.Core.BarTimeframe.Minute5,
        "15m" or "15min" => GexVisor.Core.BarTimeframe.Minute15,
        "30m" or "30min" => GexVisor.Core.BarTimeframe.Minute30,
        "1h" or "1hour" => GexVisor.Core.BarTimeframe.Hour1,
        "4h" or "4hour" => GexVisor.Core.BarTimeframe.Hour4,
        "1d" or "day" or null => GexVisor.Core.BarTimeframe.Day,
        "1w" or "week" => GexVisor.Core.BarTimeframe.Week,
        "1mo" or "month" => GexVisor.Core.BarTimeframe.Month,
        _ => GexVisor.Core.BarTimeframe.Day
    };

    var result = await marketService.GetBarsAsync(symbol.ToUpperInvariant(), tf, limit ?? 100);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.NotFound(new { error = result.Error });
});

// Cache Management API endpoints
var cache = app.MapGroup("/api/cache");

cache.MapGet("/stats", async (ICacheService cacheService) =>
{
    var stats = await cacheService.GetStatsAsync();
    return Results.Ok(stats);
});

cache.MapPost("/cleanup", async (ICacheService cacheService) =>
{
    var deleted = await cacheService.CleanupExpiredAsync();
    return Results.Ok(new { deletedEntries = deleted });
});

cache.MapPost("/invalidate", async (string pattern, ICacheService cacheService) =>
{
    var deleted = await cacheService.InvalidateAsync(pattern);
    return Results.Ok(new { deletedEntries = deleted, pattern });
});

// Serve Blazor WASM static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
