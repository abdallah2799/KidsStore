namespace WebUI.Models.ViewModels
{
    public class PurchaseInvoiceViewModel
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string? VendorName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PurchaseItemViewModel> Items { get; set; } = new();
    }

    public class PurchaseItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; } // Added for easier product lookup
        public int ProductVariantId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
        public string? Color { get; set; }
        public int Size { get; set; }
        public int Quantity { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; } // Added for validation
        public decimal? DiscountLimit { get; set; } // Added for validation
        public decimal Subtotal => Quantity * BuyingPrice;
    }
}
