using System.Net;
using LatamPriceChecker.Data;
using LatamPriceChecker.Repositories;
using LatamPriceChecker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ===== Banco de dados (PostgreSQL) =====

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada em appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IMonitoredItemRepository, MonitoredItemRepository>();

// ===== HTTP clients / serviços de domínio =====

builder.Services.AddHttpClient<IPriceFetcherService, PriceFetcherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

builder.Services.AddHttpClient<INotifier, DiscordNotifier>();

builder.Services.AddSingleton<AlertTracker>();
builder.Services.AddScoped<PriceMonitorService>();

// ===== Background service (loop de checagem periódica) =====

builder.Services.AddHostedService<PriceMonitorBackgroundService>();

// ===== API =====

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LatamPriceChecker API",
        Version = "v1",
        Description = "API para gerenciar itens monitorados e acompanhar preços do shop-search do Ragnarok Online LATAM."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LatamPriceChecker API v1");
    });
}

// Garante que o schema do banco existe (para produção, prefira migrations: dotnet ef migrations add InitialCreate)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { status = "ok", service = "LatamPriceChecker" }));

app.Run();
