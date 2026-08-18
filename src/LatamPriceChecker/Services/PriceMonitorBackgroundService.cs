using LatamPriceChecker.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LatamPriceChecker.Services;

public class PriceMonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;

    public PriceMonitorBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;

        var minutes = configuration.GetValue<double?>("Monitor:CheckIntervalMinutes") ?? 10;
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Monitor de preços iniciado.");
        Console.WriteLine($"Intervalo de verificação: {_interval.TotalMinutes} minutos.\n");

        using var timer = new PeriodicTimer(_interval);

        do
        {
            await RunCheckAsync(stoppingToken);
            Console.WriteLine($"\nPróxima verificação em {_interval.TotalMinutes} minutos...\n");
        }
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMonitoredItemRepository>();
        var monitorService = scope.ServiceProvider.GetRequiredService<PriceMonitorService>();

        try
        {
            var items = await repository.GetAllAsync(ct);

            if (items.Count == 0)
            {
                Console.WriteLine("Nenhum item cadastrado para monitoramento (use a API para adicionar itens).");
                return;
            }

            await monitorService.CheckAllAsync(items);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no ciclo de verificação: {ex.Message}");
        }
    }
}
