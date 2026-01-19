using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CarCanvas.Web;
using CarCanvas.Application;
using CarCanvas.Application.Interfaces;
using CarCanvas.Infrastructure.Services;
using CarCanvas.Web.Services;
using CarCanvas.Web.ViewModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Services
builder.Services.AddScoped<ICarLoader, CarFileLoader>();
builder.Services.AddScoped<IIntersectionService, IntersectionService>();
builder.Services.AddScoped<ICanvasSceneService, CanvasSceneService>();
builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddSingleton(new AppOptions());

await builder.Build().RunAsync();
