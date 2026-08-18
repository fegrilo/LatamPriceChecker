using LatamPriceChecker.Models;
using LatamPriceChecker.Services;
using Xunit;

namespace LatamPriceChecker.Tests.Services;

public class AlertTrackerTests
{
    [Fact]
    public void WasAlreadyNotified_ReturnsFalse_WhenItemNeverMarked()
    {
        var tracker = new AlertTracker();
        var item = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };

        var result = tracker.WasAlreadyNotified(item, "espada");

        Assert.False(result);
    }

    [Fact]
    public void MarkAsNotified_ThenWasAlreadyNotified_ReturnsTrue_ForSameItemAndSearchWord()
    {
        var tracker = new AlertTracker();
        var item = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };

        tracker.MarkAsNotified(item, "espada");

        Assert.True(tracker.WasAlreadyNotified(item, "espada"));
    }

    [Fact]
    public void WasAlreadyNotified_ReturnsFalse_WhenSearchWordDiffers()
    {
        var tracker = new AlertTracker();
        var item = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };

        tracker.MarkAsNotified(item, "espada");

        Assert.False(tracker.WasAlreadyNotified(item, "machado"));
    }

    [Fact]
    public void WasAlreadyNotified_ReturnsFalse_WhenPriceDiffers()
    {
        var tracker = new AlertTracker();
        var markedItem = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };
        var differentPriceItem = new ShopItem { ItemPrice = 200, StoreName = "Loja A" };

        tracker.MarkAsNotified(markedItem, "espada");

        Assert.False(tracker.WasAlreadyNotified(differentPriceItem, "espada"));
    }

    [Fact]
    public void WasAlreadyNotified_ReturnsFalse_WhenStoreNameDiffers()
    {
        var tracker = new AlertTracker();
        var markedItem = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };
        var differentStoreItem = new ShopItem { ItemPrice = 100, StoreName = "Loja B" };

        tracker.MarkAsNotified(markedItem, "espada");

        Assert.False(tracker.WasAlreadyNotified(differentStoreItem, "espada"));
    }

    [Fact]
    public void MarkAsNotified_IsIdempotent_WhenCalledTwiceForSameItem()
    {
        var tracker = new AlertTracker();
        var item = new ShopItem { ItemPrice = 100, StoreName = "Loja A" };

        tracker.MarkAsNotified(item, "espada");
        tracker.MarkAsNotified(item, "espada");

        Assert.True(tracker.WasAlreadyNotified(item, "espada"));
    }
}
