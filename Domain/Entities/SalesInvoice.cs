using Domain.Entities;

public class SalesInvoice : BaseEntity
{
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash / Transaction
    public string? CustomerName { get; set; } // Optional customer name
    public bool IsReturned { get; set; } = false;

    public int SellerId { get; set; }

    public virtual ICollection<SalesItem> Items { get; set; } = [];
    public virtual User Seller { get; set; } = null!;
}
