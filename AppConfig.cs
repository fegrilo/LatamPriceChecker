using LatamPriceChecker.Models;

namespace LatamPriceChecker;

public static class AppConfig
{
    public const string DiscordWebhookUrl = "https://discord.com/api/webhooks/1538949282445660332/oqL2Gx_sbwpKJn5OhX5caLIJiYkqAZzTHHzt1LvGS71E0UuX-M6vAqN2nx5oRDw25h9V";
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);

    public static readonly List<MonitoredItem> MonitoredItems = new()
    {
        new MonitoredItem("Álbum Mágico de Cartas", 750_000),
        new MonitoredItem("Báculo Adulter Fides", 1_000_000)
    };
}
