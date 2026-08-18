using LatamPriceChecker.Models;
using LatamPriceChecker.Services;
using Moq;
using Xunit;

namespace LatamPriceChecker.Tests.Services;

public class PriceMonitorServiceTests
{
    private static (PriceMonitorService service, Mock<IPriceFetcherService> fetcher, Mock<INotifier> notifier, AlertTracker tracker)
        CreateService()
    {
        var fetcher = new Mock<IPriceFetcherService>();
        var notifier = new Mock<INotifier>();
        var tracker = new AlertTracker();
        var service = new PriceMonitorService(fetcher.Object, notifier.Object, tracker);
        return (service, fetcher, notifier, tracker);
    }

    [Fact]
    public async Task CheckAllAsync_Notifies_WhenCheapestItemPriceIsBelowTarget()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var cheapItem = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { cheapItem });

        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(cheapItem, 1000), Times.Once);
    }

    [Fact]
    public async Task CheckAllAsync_Notifies_WhenCheapestItemPriceEqualsTarget()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var exactPriceItem = new ShopItem { ItemName = "espada", ItemPrice = 1000, StoreName = "Loja A" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { exactPriceItem });

        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(exactPriceItem, 1000), Times.Once);
    }

    [Fact]
    public async Task CheckAllAsync_DoesNotNotify_WhenCheapestItemPriceIsAboveTarget()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var expensiveItem = new ShopItem { ItemName = "espada", ItemPrice = 1500, StoreName = "Loja A" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { expensiveItem });

        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(It.IsAny<ShopItem>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CheckAllAsync_DoesNotNotify_WhenNoItemsAreFound()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "item raro", TargetPrice = 1000 };

        fetcher.Setup(f => f.FetchItemsAsync("item raro")).ReturnsAsync(new List<ShopItem>());

        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(It.IsAny<ShopItem>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CheckAllAsync_NotifiesOnlyForCheapestItem_WhenMultipleItemsMatch()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 2000 };
        var cheapest = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };
        var pricier = new ShopItem { ItemName = "espada", ItemPrice = 900, StoreName = "Loja B" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { pricier, cheapest });

        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(cheapest, 2000), Times.Once);
        notifier.Verify(n => n.SendPriceAlertAsync(pricier, It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CheckAllAsync_DoesNotNotifyAgain_WhenSameItemWasAlreadyNotified()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var cheapItem = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { cheapItem });

        await service.CheckAllAsync(new[] { monitored });
        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(cheapItem, 1000), Times.Once);
    }

    [Fact]
    public async Task CheckAllAsync_NotifiesAgain_WhenNewCheaperItemAppearsAfterFirstAlert()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var firstCheapItem = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };
        var evenCheaperItem = new ShopItem { ItemName = "espada", ItemPrice = 400, StoreName = "Loja B" };

        fetcher.SetupSequence(f => f.FetchItemsAsync("espada"))
            .ReturnsAsync(new List<ShopItem> { firstCheapItem })
            .ReturnsAsync(new List<ShopItem> { evenCheaperItem });

        await service.CheckAllAsync(new[] { monitored });
        await service.CheckAllAsync(new[] { monitored });

        notifier.Verify(n => n.SendPriceAlertAsync(firstCheapItem, 1000), Times.Once);
        notifier.Verify(n => n.SendPriceAlertAsync(evenCheaperItem, 1000), Times.Once);
    }

    [Fact]
    public async Task CheckAllAsync_ContinuesToNextItem_WhenFetcherThrowsForOneItem()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var brokenItem = new MonitoredItem { SearchWord = "quebra", TargetPrice = 1000 };
        var okItem = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var cheapItem = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };

        fetcher.Setup(f => f.FetchItemsAsync("quebra")).ThrowsAsync(new HttpRequestException("falha de rede"));
        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { cheapItem });

        await service.CheckAllAsync(new[] { brokenItem, okItem });

        notifier.Verify(n => n.SendPriceAlertAsync(cheapItem, 1000), Times.Once);
    }

    [Fact]
    public async Task CheckAllAsync_ChecksEachMonitoredItemIndependently()
    {
        var (service, fetcher, notifier, _) = CreateService();
        var monitored1 = new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 };
        var monitored2 = new MonitoredItem { SearchWord = "machado", TargetPrice = 1000 };
        var item1 = new ShopItem { ItemName = "espada", ItemPrice = 500, StoreName = "Loja A" };
        var item2 = new ShopItem { ItemName = "machado", ItemPrice = 700, StoreName = "Loja B" };

        fetcher.Setup(f => f.FetchItemsAsync("espada")).ReturnsAsync(new List<ShopItem> { item1 });
        fetcher.Setup(f => f.FetchItemsAsync("machado")).ReturnsAsync(new List<ShopItem> { item2 });

        await service.CheckAllAsync(new[] { monitored1, monitored2 });

        notifier.Verify(n => n.SendPriceAlertAsync(item1, 1000), Times.Once);
        notifier.Verify(n => n.SendPriceAlertAsync(item2, 1000), Times.Once);
    }
}
