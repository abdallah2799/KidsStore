using Domain.Entities;

namespace Domain.Entities
{
    public class PurchaseReturnInvoice : BaseEntity
    {
        public int PurchaseInvoiceId { get; set; }
        public int VendorId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public decimal TotalRefund { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Navigation
        public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;
        public virtual Vendor Vendor { get; set; } = null!;
        public virtual ICollection<PurchaseReturnItem> Items { get; set; } = [];
    }
}
