using Domain.Entities;

public class ReturnItem : BaseEntity
{
    public int ReturnInvoiceId { get; set; }
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }

    public virtual ReturnInvoice ReturnInvoice { get; set; } = null!;
    public virtual ProductVariant ProductVariant { get; set; } = null!;
}
