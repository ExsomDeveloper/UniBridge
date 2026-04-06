namespace UniBridge
{
    public class ProductData
    {
        public string ProductId { get; set; }
        public ProductType Type { get; set; }
        public string LocalizedTitle { get; set; }
        public string LocalizedDescription { get; set; }
        public string LocalizedPriceString { get; set; }
        public decimal LocalizedPrice { get; set; }
        public string IsoCurrencyCode { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
}
