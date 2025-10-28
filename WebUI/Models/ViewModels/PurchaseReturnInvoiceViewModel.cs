namespace WebUI.Models.ViewModels
{
    public class PurchaseReturnInvoiceViewModel
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public int VendorId { get; set; }
        public string? VendorName { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal TotalRefund { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<PurchaseReturnItemViewModel> Items { get; set; } = new();
    }

    public class PurchaseReturnItemViewModel
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
        public string? Color { get; set; }
        public int Size { get; set; }
        public int Quantity { get; set; }
        public decimal RefundPrice { get; set; }
        public decimal Subtotal => Quantity * RefundPrice;
    }
}
