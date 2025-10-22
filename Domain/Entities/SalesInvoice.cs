using Domain.Entities;

public class SalesInvoice : BaseEntity
{
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash / Transaction
    public bool IsReturned { get; set; } = false;

    public virtual ICollection<SalesItem> Items { get; set; } = [];
}
