namespace Domain.Entities
{
    public class PurchaseReturnItem : BaseEntity
    {
        public int PurchaseReturnInvoiceId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal RefundPrice { get; set; }

        // Navigation
        public virtual PurchaseReturnInvoice PurchaseReturnInvoice { get; set; } = null!;
        public virtual ProductVariant ProductVariant { get; set; } = null!;
    }
}
