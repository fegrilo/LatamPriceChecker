using System.Net;
using LatamPriceChecker;
using LatamPriceChecker.Services;

var priceFetcher = BuildPriceFetcher();
var notifier = BuildDiscordNotifier();
var monitorService = new PriceMonitorService(priceFetcher, notifier, new AlertTracker());

Console.WriteLine("Monitor de preços iniciado. Pressione Ctrl+C para sair.");
Console.WriteLine($"Verificando {AppConfig.MonitoredItems.Count} item(ns) a cada {AppConfig.CheckInterval.TotalMinutes} minutos.\n");

await RunSchedulerAsync(monitorService, AppConfig.CheckInterval);

return;

// ===== Composição das dependências =====

static IPriceFetcherService BuildPriceFetcher()
{
    var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
    var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");

    return new PriceFetcherService(httpClient);
}

static INotifier BuildDiscordNotifier()
{
    var httpClient = new HttpClient();
    return new DiscordNotifier(httpClient, AppConfig.DiscordWebhookUrl);
}

// ===== Loop do scheduler =====

static async Task RunSchedulerAsync(PriceMonitorService monitorService, TimeSpan interval)
{
    using var timer = new PeriodicTimer(interval);

    do
    {
        await monitorService.CheckAllAsync(AppConfig.MonitoredItems);
        Console.WriteLine($"\nPróxima verificação em {interval.TotalMinutes} minutos...\n");
    }
    while (await timer.WaitForNextTickAsync());
}
