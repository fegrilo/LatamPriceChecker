namespace LatamPriceChecker.Models
{
    public class MonitoredItem
    {
        public int Id { get; set; }
        public string SearchWord { get; set; } = string.Empty;
        public long TargetPrice { get; set; }
    }
}
