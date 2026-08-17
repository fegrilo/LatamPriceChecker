using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public interface INotifier
{
    Task SendPriceAlertAsync(ShopItem item, long targetPrice);
}
