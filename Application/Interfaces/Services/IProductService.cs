using Domain.Entities;

namespace Application.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllWithDetailsAsync();
        Task<Product?> GetByIdWithDetailsAsync(int id);
        Task<Product?> AddAsync(Product product, IEnumerable<ProductVariant> variants);
        Task<Product?> UpdateAsync(Product product, IEnumerable<ProductVariant> variants);
        Task<bool> DeleteAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<Dictionary<int, DateTime?>> GetLastSoldDatesAsync(IEnumerable<int> productIds);
        Task<bool> SetActiveAsync(int id, bool isActive);
    }
}
