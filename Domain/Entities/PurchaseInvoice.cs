using Domain.Entities;

public class PurchaseInvoice : BaseEntity
{
    public int VendorId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
    public virtual ICollection<PurchaseItem> Items { get; set; } = [];
}
