namespace WebUI.Models.ViewModels
{
    public class VendorPurchaseStatsViewModel
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalPurchased { get; set; }
        public decimal TotalReturned { get; set; }
        public decimal NetPurchased => TotalPurchased - TotalReturned;
        public int InvoiceCount { get; set; }
        public int ProductsSoldCount { get; set; } // How many of vendor's products were sold
        public decimal ProductsSoldValue { get; set; } // Total sales value of vendor's products
    }
}
