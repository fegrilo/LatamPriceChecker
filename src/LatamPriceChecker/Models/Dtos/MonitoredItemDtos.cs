namespace LatamPriceChecker.Models.Dtos
{
    public record CreateMonitoredItemDto(string SearchWord, long TargetPrice);

    public record UpdateMonitoredItemDto(string SearchWord, long TargetPrice);
}
