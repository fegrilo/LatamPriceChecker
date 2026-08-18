using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public class AlertTracker
{
    private readonly HashSet<string> _notifiedKeys = new();

    public bool WasAlreadyNotified(ShopItem item, string searchWord)
    {
        return _notifiedKeys.Contains(BuildKey(item, searchWord));
    }

    public void MarkAsNotified(ShopItem item, string searchWord)
    {
        _notifiedKeys.Add(BuildKey(item, searchWord));
    }

    private static string BuildKey(ShopItem item, string searchWord)
    {
        return $"{searchWord}_{item.ItemPrice}_{item.StoreName}";
    }
}
