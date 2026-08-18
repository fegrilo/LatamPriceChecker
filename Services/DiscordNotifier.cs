using System.Text;
using System.Text.Json;
using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public class DiscordNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;
    private readonly string? _mentionUserId;

    public DiscordNotifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _webhookUrl = configuration["Discord:WebhookUrl"]
            ?? throw new InvalidOperationException(
                "Configuração 'Discord:WebhookUrl' não encontrada. Defina-a em appsettings.json, variável de ambiente ou user-secrets.");
        _mentionUserId = configuration["Discord:MentionUserId"];
    }

    public async Task SendPriceAlertAsync(ShopItem item, long targetPrice)
    {
        var payload = BuildPayload(item, targetPrice, _mentionUserId);
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Falha ao enviar para o Discord: {response.StatusCode} - {body}");
        }
    }

    private static object BuildPayload(ShopItem item, long targetPrice, string? mentionUserId)
    {
        var fields = new List<object>
        {
            new { name = "Item", value = item.ItemName ?? "?", inline = false },
            new { name = "Preço encontrado", value = $"{item.ItemPrice:N0} zeny", inline = false },
            new { name = "Preço alvo", value = $"{targetPrice:N0} zeny", inline = false },
            new { name = "Loja", value = item.StoreName ?? "?", inline = false },
            new { name = "Vendedor", value = item.ItemSellerCharName ?? "?", inline = false },
            new { name = "Quantidade", value = item.ItemCnt, inline = false }
        };

        var embed = new Dictionary<string, object>
        {
            ["title"] = "🔔 Alerta de preço!",
            ["color"] = 3066993,
            ["fields"] = fields.ToArray(),
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };

        if (!string.IsNullOrWhiteSpace(item.DatabaseImgPath))
        {
            embed["thumbnail"] = new { url = item.DatabaseImgPath };
        }

        var message = new Dictionary<string, object>
        {
            ["embeds"] = new[] { embed }
        };

        if (!string.IsNullOrWhiteSpace(mentionUserId))
        {
            message["content"] = $"<@{mentionUserId}>";
            message["allowed_mentions"] = new { users = new[] { mentionUserId } };
        }

        return new { embeds = new[] { embed } };
    }
}
