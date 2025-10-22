using Domain.Entities;

public class ReturnInvoice : BaseEntity
{
    public int SalesInvoiceId { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public decimal TotalRefund { get; set; }

    // Navigation
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
    public virtual ICollection<ReturnItem> Items { get; set; } = [];
}
