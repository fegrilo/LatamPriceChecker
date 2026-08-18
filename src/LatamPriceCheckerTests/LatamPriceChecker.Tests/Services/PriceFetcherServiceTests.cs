using LatamPriceChecker.Services;
using LatamPriceChecker.Tests.TestHelpers;
using Xunit;

namespace LatamPriceChecker.Tests.Services;

public class PriceFetcherServiceTests
{
    // Simula o formato real da página: JSON embutido no HTML do Next.js,
    // com aspas escapadas (\"), no padrão \"list\":[...],\"totalCount\":N
    private const string HtmlWithTwoItems =
        "<script>self.__next_f.push([1,\"...\\\"list\\\":[" +
        "{\\\"itemName\\\":\\\"Espada Lendaria\\\",\\\"itemPrice\\\":150000,\\\"storeName\\\":\\\"Loja do Zé\\\"," +
        "\\\"itemSellerCharName\\\":\\\"ZeVendedor\\\",\\\"itemCnt\\\":3,\\\"storeTypeName\\\":\\\"BUY\\\"," +
        "\\\"mapName\\\":\\\"prt_mk.gat\\\",\\\"databaseImgPath\\\":\\\"https://assets.gnjoylatam.com/img/1.png\\\"}," +
        "{\\\"itemName\\\":\\\"Espada Lendaria\\\",\\\"itemPrice\\\":120000,\\\"storeName\\\":\\\"Barganha\\\"," +
        "\\\"itemSellerCharName\\\":\\\"Mercador2\\\",\\\"itemCnt\\\":1,\\\"storeTypeName\\\":\\\"BUY\\\"," +
        "\\\"mapName\\\":\\\"prt_mk.gat\\\",\\\"databaseImgPath\\\":\\\"https://assets.gnjoylatam.com/img/2.png\\\"}" +
        "],\\\"totalCount\\\":2}\"])</script>";

    private const string HtmlWithoutListPattern =
        "<html><body>Nenhum dado relevante aqui</body></html>";

    private static PriceFetcherService CreateService(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = null };
        return new PriceFetcherService(httpClient);
    }

    [Fact]
    public async Task FetchItemsAsync_ParsesAllItems_WhenListPatternIsPresent()
    {
        var handler = new FakeHttpMessageHandler(HtmlWithTwoItems);
        var service = CreateService(handler);

        var items = await service.FetchItemsAsync("Espada Lendaria");

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task FetchItemsAsync_MapsAllFieldsCorrectly()
    {
        var handler = new FakeHttpMessageHandler(HtmlWithTwoItems);
        var service = CreateService(handler);

        var items = await service.FetchItemsAsync("Espada Lendaria");
        var cheapest = items.Single(i => i.ItemPrice == 120000);

        Assert.Equal("Espada Lendaria", cheapest.ItemName);
        Assert.Equal("Barganha", cheapest.StoreName);
        Assert.Equal("Mercador2", cheapest.ItemSellerCharName);
        Assert.Equal(1, cheapest.ItemCnt);
        Assert.Equal("BUY", cheapest.StoreTypeName);
        Assert.Equal("prt_mk.gat", cheapest.MapName);
        Assert.Equal("https://assets.gnjoylatam.com/img/2.png", cheapest.DatabaseImgPath);
    }

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyList_WhenListPatternIsNotFound()
    {
        var handler = new FakeHttpMessageHandler(HtmlWithoutListPattern);
        var service = CreateService(handler);

        var items = await service.FetchItemsAsync("Item Inexistente");

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchItemsAsync_RequestsCorrectUrl_WithBuyStoreTypeAndFreyaServer()
    {
        var handler = new FakeHttpMessageHandler(HtmlWithoutListPattern);
        var service = CreateService(handler);

        await service.FetchItemsAsync("Espada Lendaria");

        Assert.NotNull(handler.LastRequest?.RequestUri);
        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("storeType=BUY", url);
        Assert.Contains("serverType=FREYA", url);
        Assert.Contains("sortType=LOW_PRICE", url);
    }

    [Fact]
    public async Task FetchItemsAsync_UsesGetHttpMethod()
    {
        var handler = new FakeHttpMessageHandler(HtmlWithoutListPattern);
        var service = CreateService(handler);

        await service.FetchItemsAsync("qualquer item");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    }
}
