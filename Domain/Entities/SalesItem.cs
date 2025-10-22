using Domain.Entities;

public class SalesItem : BaseEntity
{
    public int SalesInvoiceId { get; set; }
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal DiscountValue { get; set; } // fixed amount after rules
    public decimal Total => (SellingPrice - DiscountValue) * Quantity;

    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
