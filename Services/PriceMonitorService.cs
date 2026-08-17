using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public class PriceMonitorService
{
    private readonly IPriceFetcherService _fetcher;
    private readonly INotifier _notifier;
    private readonly AlertTracker _alertTracker;
    private readonly string _serverType = "FREYA";

    public PriceMonitorService(IPriceFetcherService fetcher, INotifier notifier, AlertTracker alertTracker)
    {
        _fetcher = fetcher;
        _notifier = notifier;
        _alertTracker = alertTracker;
    }

    public async Task CheckAllAsync(IEnumerable<MonitoredItem> monitoredItems)
    {
        foreach (var monitored in monitoredItems)
        {
            await CheckSingleItemAsync(monitored);
        }
    }

    private async Task CheckSingleItemAsync(MonitoredItem monitored)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Verificando '{monitored.SearchWord}'...");

        try
        {
            var items = await _fetcher.FetchItemsAsync(monitored.SearchWord, _serverType);

            if (items.Count == 0)
            {
                Console.WriteLine("  Nenhum item encontrado.");
                return;
            }

            var cheapest = items.OrderBy(i => i.ItemPrice).First();
            Console.WriteLine($"  Menor preço encontrado: {cheapest.ItemPrice:N0} ({cheapest.ItemName})");

            await NotifyIfBelowTargetAsync(monitored, cheapest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Erro ao verificar '{monitored.SearchWord}': {ex.Message}");
        }
    }

    private async Task NotifyIfBelowTargetAsync(MonitoredItem monitored, ShopItem cheapest)
    {
        var isBelowTarget = cheapest.ItemPrice <= monitored.TargetPrice;
        var alreadyNotified = _alertTracker.WasAlreadyNotified(cheapest, monitored.SearchWord);

        if (!isBelowTarget || alreadyNotified)
            return;

        Console.WriteLine("  🔔 Preço abaixo do alvo! Notificando...");
        await _notifier.SendPriceAlertAsync(cheapest, monitored.TargetPrice);
        _alertTracker.MarkAsNotified(cheapest, monitored.SearchWord);
    }
}
