using GexVisor.UI;
using GexVisor.UI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// GEX Visualizer Services
builder.Services.AddScoped<IGexStateService, GexStateService>();
builder.Services.AddScoped<IGexDataService, GexDataService>();
builder.Services.AddScoped<IGexExportService, GexExportService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IResearchPathService, ResearchPathService>();
builder.Services.AddScoped<AnnotationService>();
builder.Services.AddScoped<SqliteService>();

// Journal Services
builder.Services.AddScoped<TaskPersistenceService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<NotebookService>();
builder.Services.AddScoped<PaperTradeService>();
builder.Services.AddScoped<TradeLogService>();
builder.Services.AddScoped<DecisionMetadataParser>();
builder.Services.AddScoped<BacktestService>();
builder.Services.AddScoped<ResearchTaskService>();

// GitHub Integration Services
builder.Services.AddScoped<GitHubAuthService>();
builder.Services.AddScoped<GitHubProjectService>();
builder.Services.AddScoped<BoardStateService>();
builder.Services.AddScoped<StatusMapper>();

// Cross-Asset Comparison Services
builder.Services.AddScoped<ComparisonService>();
builder.Services.AddScoped<ComparisonAnalysisService>();

// Chart Services
builder.Services.AddScoped<IPriceDataService, PriceDataService>();

// Alert Services
builder.Services.AddScoped<IAlertService, AlertService>();

await builder.Build().RunAsync();
