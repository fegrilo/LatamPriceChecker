namespace LatamPriceChecker.Models
{
    public class ShopItem
    {
        public string? ItemName { get; set; }
        public long ItemPrice { get; set; }
        public string? StoreName { get; set; }
        public string? ItemSellerCharName { get; set; }
        public int ItemCnt { get; set; }
        public string? StoreTypeName { get; set; }
    }
}