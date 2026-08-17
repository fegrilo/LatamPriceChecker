using System.Text;
using System.Text.Json;
using LatamPriceChecker.Models;

namespace LatamPriceChecker.Services;

public class DiscordNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public DiscordNotifier(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _webhookUrl = configuration["Discord:WebhookUrl"]
            ?? throw new InvalidOperationException(
                "Configuração 'Discord:WebhookUrl' não encontrada. Defina-a em appsettings.json, variável de ambiente ou user-secrets.");
    }

    public async Task SendPriceAlertAsync(ShopItem item, long targetPrice)
    {
        var payload = BuildPayload(item, targetPrice);
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

    private static object BuildPayload(ShopItem item, long targetPrice)
    {
        return new
        {
            embeds = new[]
            {
                new
                {
                    title = "🔔 Alerta de preço!",
                    color = 3066993,
                    fields = new[]
                    {
                        new { name = "Item", value = item.ItemName ?? "?", inline = false },
                        new { name = "Preço encontrado", value = $"{item.ItemPrice:N0} zeny", inline = true },
                        new { name = "Preço alvo", value = $"{targetPrice:N0} zeny", inline = true },
                        new { name = "Loja", value = item.StoreName ?? "?", inline = true },
                        new { name = "Vendedor", value = item.ItemSellerCharName ?? "?", inline = true }
                    },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };
    }
}
