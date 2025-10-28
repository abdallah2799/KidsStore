using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;

        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<Product>> GetAllWithDetailsAsync()
        {
            return await _uow.Repository<Product>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.Variants)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdWithDetailsAsync(int id)
        {
            return await _uow.Repository<Product>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> AddAsync(Product product, IEnumerable<ProductVariant> variants)
        {
            var productRepo = _uow.Repository<Product>();
            var variantRepo = _uow.Repository<ProductVariant>();

            await productRepo.AddAsync(product);
            await _uow.SaveChangesAsync();

            // Attach variants
            foreach (var v in variants)
            {
                v.ProductId = product.Id;
                await variantRepo.AddAsync(v);
            }

            await _uow.SaveChangesAsync();
            return await GetByIdWithDetailsAsync(product.Id);
        }

        public async Task<Product?> UpdateAsync(Product product, IEnumerable<ProductVariant> variants)
        {
            var productRepo = _uow.Repository<Product>();
            var variantRepo = _uow.Repository<ProductVariant>();

            var existing = await productRepo.AsQueryable()
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (existing is null)
                return null;

            existing.Code = product.Code;
            existing.Description = product.Description;
            existing.VendorId = product.VendorId;
            existing.BuyingPrice = product.BuyingPrice;
            existing.SellingPrice = product.SellingPrice;
            existing.IsActive = product.IsActive;
            existing.DiscountLimit = product.DiscountLimit;

            // Merge variants: update existing, add new, remove missing
            var incoming = (variants ?? Enumerable.Empty<ProductVariant>()).ToList();
            var existingVariants = existing.Variants?.ToList() ?? new List<ProductVariant>();

            // Index existing by Id for quick lookup
            var existingById = existingVariants.Where(v => v.Id > 0)
                                               .ToDictionary(v => v.Id, v => v);

            // Track Ids that are present in incoming set
            var incomingIds = new HashSet<int>(incoming.Where(v => v.Id > 0).Select(v => v.Id));

            // Update or add
            foreach (var v in incoming)
            {
                if (v.Id > 0 && existingById.TryGetValue(v.Id, out var toUpdate))
                {
                    // Update existing entity fields
                    toUpdate.Color = v.Color;
                    toUpdate.Size = v.Size;
                    toUpdate.Stock = v.Stock;
                    toUpdate.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // New variant
                    var newVar = new ProductVariant
                    {
                        ProductId = existing.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Stock = v.Stock,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await variantRepo.AddAsync(newVar);
                }
            }

            // NOTE: Do not delete variants here to avoid FK constraint issues
            // with PurchaseItems/SalesItems history. Variants omitted from the
            // incoming payload will be kept. Consider adding an IsActive flag
            // on ProductVariant for soft-deletion in the future.

            await _uow.SaveChangesAsync();

            return await GetByIdWithDetailsAsync(existing.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var productRepo = _uow.Repository<Product>();
            var variantRepo = _uow.Repository<ProductVariant>();

            var existing = await productRepo.AsQueryable()
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing is null)
                return false;

            // Remove variants first to avoid FK constraint if cascade not configured
            foreach (var v in existing.Variants.ToList())
            {
                variantRepo.Remove(v);
            }

            productRepo.Remove(existing);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var normalized = code.Trim();
            return await _uow.Repository<Product>()
                .AsQueryable()
                .AnyAsync(p => p.Code == normalized && (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        public async Task<Dictionary<int, DateTime?>> GetLastSoldDatesAsync(IEnumerable<int> productIds)
        {
            var ids = productIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return new Dictionary<int, DateTime?>();

            // Query sales for variants of those products and get max sale date per product
            var q = _uow.Repository<SalesItem>()
                .AsQueryable()
                .Include(si => si.SalesInvoice)
                .Include(si => si.ProductVariant);

            var result = await q
                .Where(si => ids.Contains(si.ProductVariant.ProductId))
                .GroupBy(si => si.ProductVariant.ProductId)
                .Select(g => new { ProductId = g.Key, LastSoldAt = g.Max(x => x.SalesInvoice.SaleDate) })
                .ToListAsync();

            return result.ToDictionary(x => x.ProductId, x => (DateTime?)x.LastSoldAt);
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive)
        {
            var repo = _uow.Repository<Product>();
            var existing = await repo.AsQueryable().FirstOrDefaultAsync(p => p.Id == id);
            if (existing is null) return false;
            existing.IsActive = isActive;
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
