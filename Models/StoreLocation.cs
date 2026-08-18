namespace LatamPriceChecker.Models
{
    public class StoreLocation
    {
        public string? Xpos { get; set; }
        public string? Ypos { get; set; }
    }
    public class StoreLocationResponse
    {
        public StoreLocation? Data { get; set; }
        public bool Success { get; set; }
    }
}
