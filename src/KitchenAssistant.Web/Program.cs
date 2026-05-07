using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KitchenAssistant.Web;
using KitchenAssistant.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 从 appsettings.json 读取 SignalR 服务器地址，支持局域网/手机访问
var serverUrl = builder.Configuration["ServerUrl"] ?? "http://localhost:5001";
var hubUrl = $"{serverUrl}/cookingHub";

builder.Services.AddSingleton<RecipeService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped(_ => new ChatService(hubUrl));
builder.Services.AddScoped<ShoppingListService>();
builder.Services.AddScoped(_ => new LiveRoomService(hubUrl));

var host = builder.Build();

// 初始化时从本地存储加载用户信息
using var scope = host.Services.CreateScope();
var appState = scope.ServiceProvider.GetRequiredService<AppState>();
await appState.LoadFromStorageAsync();

await host.RunAsync();
