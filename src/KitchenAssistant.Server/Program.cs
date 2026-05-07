using KitchenAssistant.Server.Data;
using KitchenAssistant.Server.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Location");
    });
});

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "kitchen.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<PersistenceService>();
builder.Services.AddSignalR();

var app = builder.Build();

// 自动建表
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("AllowAll");

app.MapHub<CookingHub>("/cookingHub");

app.MapGet("/", () => "🍳 Kitchen Assistant Server is running!");

// iPhone 安装根证书端点
app.MapGet("/cert", async (HttpContext ctx) =>
{
    var certPath = Path.Combine(app.Environment.ContentRootPath, "../../certs/rootCA.pem");
    if (!File.Exists(certPath)) { ctx.Response.StatusCode = 404; return; }
    ctx.Response.ContentType = "application/x-pem-file";
    ctx.Response.Headers.Append("Content-Disposition", "attachment; filename=KitchenAssistant-CA.pem");
    await ctx.Response.SendFileAsync(certPath);
});

app.Run();
