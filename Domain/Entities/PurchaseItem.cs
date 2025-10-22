using Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public int PurchaseInvoiceId { get; set; }
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal BuyingPrice { get; set; }

    // Navigation
    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;
    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
