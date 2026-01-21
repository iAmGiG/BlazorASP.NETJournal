using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services
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

// Serve Blazor WASM static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
