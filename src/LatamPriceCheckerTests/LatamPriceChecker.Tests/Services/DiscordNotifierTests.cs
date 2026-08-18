using System.Net;
using System.Text.Json;
using LatamPriceChecker.Models;
using LatamPriceChecker.Services;
using LatamPriceChecker.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LatamPriceChecker.Tests.Services;

public class DiscordNotifierTests
{
    private static IConfiguration BuildConfig(string webhookUrl = "https://discord.com/api/webhooks/1/token", string? mentionUserId = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Discord:WebhookUrl"] = webhookUrl
        };
        if (mentionUserId is not null)
            dict["Discord:MentionUserId"] = mentionUserId;

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void Constructor_Throws_WhenWebhookUrlIsMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var httpClient = new HttpClient(new FakeHttpMessageHandler(""));

        Assert.Throws<InvalidOperationException>(() => new DiscordNotifier(httpClient, config));
    }

    [Fact]
    public async Task SendPriceAlertAsync_PostsToConfiguredWebhookUrl()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig("https://discord.com/api/webhooks/123/abc"));
        var item = new ShopItem { ItemName = "Espada", ItemPrice = 1000, StoreName = "Loja", ItemSellerCharName = "Fulano", ItemCnt = 1 };

        await notifier.SendPriceAlertAsync(item, 1500);

        Assert.Equal("https://discord.com/api/webhooks/123/abc", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
    }

    [Fact]
    public async Task SendPriceAlertAsync_IncludesItemFieldsInEmbed()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig());
        var item = new ShopItem
        {
            ItemName = "Báculo Adulter Fides",
            ItemPrice = 999000,
            StoreName = "Loja do Zé",
            ItemSellerCharName = "Fulano",
            ItemCnt = 2
        };

        await notifier.SendPriceAlertAsync(item, 1000000);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var embed = doc.RootElement.GetProperty("embeds")[0];
        var fields = embed.GetProperty("fields");

        var fieldValues = fields.EnumerateArray()
            .ToDictionary(f => f.GetProperty("name").GetString()!, f => f.GetProperty("value").ToString());

        Assert.Equal("Báculo Adulter Fides", fieldValues["Item"]);
        Assert.Contains(999000L.ToString("N0"), fieldValues["Preço encontrado"]);
        Assert.Contains(1000000L.ToString("N0"), fieldValues["Preço alvo"]);
        Assert.Equal("Loja do Zé", fieldValues["Loja"]);
        Assert.Equal("Fulano", fieldValues["Vendedor"]);
        Assert.Equal("2", fieldValues["Quantidade"]);
    }

    [Fact]
    public async Task SendPriceAlertAsync_IncludesThumbnail_WhenDatabaseImgPathIsSet()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig());
        var item = new ShopItem { ItemName = "Item", ItemPrice = 100, DatabaseImgPath = "https://assets.gnjoylatam.com/img/1.png" };

        await notifier.SendPriceAlertAsync(item, 200);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var embed = doc.RootElement.GetProperty("embeds")[0];

        Assert.True(embed.TryGetProperty("thumbnail", out var thumbnail));
        Assert.Equal("https://assets.gnjoylatam.com/img/1.png", thumbnail.GetProperty("url").GetString());
    }

    [Fact]
    public async Task SendPriceAlertAsync_OmitsThumbnail_WhenDatabaseImgPathIsNullOrEmpty()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig());
        var item = new ShopItem { ItemName = "Item", ItemPrice = 100, DatabaseImgPath = null };

        await notifier.SendPriceAlertAsync(item, 200);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var embed = doc.RootElement.GetProperty("embeds")[0];

        Assert.False(embed.TryGetProperty("thumbnail", out _));
    }

    [Fact]
    public async Task SendPriceAlertAsync_Throws_WhenDiscordRespondsWithError()
    {
        var handler = new FakeHttpMessageHandler("rate limited", HttpStatusCode.TooManyRequests);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig());
        var item = new ShopItem { ItemName = "Item", ItemPrice = 100 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => notifier.SendPriceAlertAsync(item, 200));
    }

    [Fact]
    public async Task SendPriceAlertAsync_ShouldIncludeMentionContent_WhenMentionUserIdIsConfigured()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NoContent);
        var httpClient = new HttpClient(handler);
        var notifier = new DiscordNotifier(httpClient, BuildConfig(mentionUserId: "123456789012345678"));
        var item = new ShopItem { ItemName = "Item", ItemPrice = 100 };

        await notifier.SendPriceAlertAsync(item, 200);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);

        Assert.True(
            doc.RootElement.TryGetProperty("content", out var content),
            "BUG: o payload enviado ao Discord não contém 'content' com a menção, " +
            "mesmo com Discord:MentionUserId configurado. Veja o comentário deste teste.");
        Assert.Equal("<@123456789012345678>", content.GetString());
    }
}
