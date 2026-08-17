using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public interface IPriceFetcherService
{
    Task<List<ShopItem>> FetchItemsAsync(string searchWord, string serverType);
}