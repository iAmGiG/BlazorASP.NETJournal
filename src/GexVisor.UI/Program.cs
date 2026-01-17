using GexVisor.UI;
using GexVisor.UI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// GEX Visualizer Services
builder.Services.AddSingleton<GexStateService>();
builder.Services.AddScoped<GexDataService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AnnotationService>();

await builder.Build().RunAsync();
