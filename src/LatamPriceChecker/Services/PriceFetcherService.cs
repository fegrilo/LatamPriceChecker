using LatamPriceChecker.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LatamPriceChecker.Services;

public class PriceFetcherService : IPriceFetcherService
{
    private const string BaseUrl = "https://ro.gnjoylatam.com/pt/intro/shop-search/trading";

    private static readonly Regex ListPattern = new(
        @"\\""list\\"":\[(.*?)\],\\""totalCount\\"":(\d+)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public PriceFetcherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ShopItem>> FetchItemsAsync(string searchWord)
    {
        var url = BuildUrl(searchWord);
        var html = await _httpClient.GetStringAsync(url);

        return ParseItemsFromHtml(html);
    }

    private static string BuildUrl(string searchWord)
    {
        return $"{BaseUrl}?storeType=BUY&serverType=FREYA" +
               $"&searchWord={Uri.EscapeDataString(searchWord)}&sortType=LOW_PRICE&p=1&view=list";
    }

    private static List<ShopItem> ParseItemsFromHtml(string html)
    {
        var match = ListPattern.Match(html);
        if (!match.Success)
            return new List<ShopItem>();

        var jsonArray = "[" + match.Groups[1].Value.Replace("\\\"", "\"") + "]";

        var items = JsonSerializer.Deserialize<List<ShopItem>>(jsonArray, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return items ?? new List<ShopItem>();
    }
}