using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPurchaseService
    {
        // Purchase Invoices
        Task<List<PurchaseInvoice>> GetAllInvoicesWithDetailsAsync();
        Task<PurchaseInvoice?> GetInvoiceByIdWithDetailsAsync(int id);
        Task<PurchaseInvoice?> AddInvoiceAsync(PurchaseInvoice invoice, IEnumerable<PurchaseItem> items);
        Task<PurchaseInvoice?> UpdateInvoiceAsync(PurchaseInvoice invoice, IEnumerable<PurchaseItem> items);
        Task<bool> DeleteInvoiceAsync(int id);

        // Purchase Returns
        Task<List<PurchaseReturnInvoice>> GetAllReturnsWithDetailsAsync();
        Task<PurchaseReturnInvoice?> GetReturnByIdWithDetailsAsync(int id);
        Task<PurchaseReturnInvoice?> AddReturnAsync(PurchaseReturnInvoice returnInvoice, IEnumerable<PurchaseReturnItem> items);
        Task<bool> DeleteReturnAsync(int id);

        // Statistics
        Task<Dictionary<int, (decimal TotalPurchased, decimal TotalReturned, int InvoiceCount)>> GetVendorPurchaseStatsAsync();
        Task<Dictionary<int, (int ProductsSoldCount, decimal ProductsSoldValue)>> GetVendorSalesStatsAsync();

        // Helper methods for business logic
        Task<List<Product>> GetProductsByVendorAsync(int vendorId);
        Task<ProductVariant?> GetOrCreateVariantAsync(int productId, string color, int size);
    }
}
