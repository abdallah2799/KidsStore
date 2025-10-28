using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IUnitOfWork _uow;

        public PurchaseService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ==================== PURCHASE INVOICES ====================

        public async Task<List<PurchaseInvoice>> GetAllInvoicesWithDetailsAsync()
        {
            return await _uow.Repository<PurchaseInvoice>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        public async Task<PurchaseInvoice?> GetInvoiceByIdWithDetailsAsync(int id)
        {
            return await _uow.Repository<PurchaseInvoice>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PurchaseInvoice?> AddInvoiceAsync(PurchaseInvoice invoice, IEnumerable<PurchaseItem> items)
        {
            var invoiceRepo = _uow.Repository<PurchaseInvoice>();
            var itemRepo = _uow.Repository<PurchaseItem>();
            var variantRepo = _uow.Repository<ProductVariant>();
            var productRepo = _uow.Repository<Product>();

            await invoiceRepo.AddAsync(invoice);
            await _uow.SaveChangesAsync();

            // Add items and update stock
            foreach (var item in items)
            {
                item.PurchaseInvoiceId = invoice.Id;
                await itemRepo.AddAsync(item);

                // Update stock: increase variant stock
                var variant = await variantRepo.AsQueryable()
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock += item.Quantity;

                    // Sync buying price to product if changed
                    if (variant.Product != null && variant.Product.BuyingPrice != item.BuyingPrice)
                    {
                        variant.Product.BuyingPrice = item.BuyingPrice;
                        // Note: selling price and discount are left unchanged unless explicitly modified
                    }
                }
            }

            await _uow.SaveChangesAsync();
            return await GetInvoiceByIdWithDetailsAsync(invoice.Id);
        }

        public async Task<PurchaseInvoice?> UpdateInvoiceAsync(PurchaseInvoice invoice, IEnumerable<PurchaseItem> items)
        {
            var invoiceRepo = _uow.Repository<PurchaseInvoice>();
            var itemRepo = _uow.Repository<PurchaseItem>();
            var variantRepo = _uow.Repository<ProductVariant>();

            var existing = await invoiceRepo.AsQueryable()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == invoice.Id);

            if (existing is null) return null;

            // Revert old stock changes
            foreach (var oldItem in existing.Items)
            {
                var variant = await variantRepo.AsQueryable()
                    .FirstOrDefaultAsync(v => v.Id == oldItem.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock -= oldItem.Quantity;
                }
                itemRepo.Remove(oldItem);
            }

            // Update invoice
            existing.VendorId = invoice.VendorId;
            existing.PurchaseDate = invoice.PurchaseDate;
            existing.TotalAmount = invoice.TotalAmount;

            // Add new items and update stock
            foreach (var item in items)
            {
                item.PurchaseInvoiceId = existing.Id;
                await itemRepo.AddAsync(item);

                var variant = await variantRepo.AsQueryable()
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock += item.Quantity;
                }
            }

            await _uow.SaveChangesAsync();
            return await GetInvoiceByIdWithDetailsAsync(existing.Id);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoiceRepo = _uow.Repository<PurchaseInvoice>();
            var itemRepo = _uow.Repository<PurchaseItem>();
            var variantRepo = _uow.Repository<ProductVariant>();

            var existing = await invoiceRepo.AsQueryable()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing is null) return false;

            // Revert stock changes - subtract the quantities that were added
            foreach (var item in existing.Items)
            {
                var variant = await variantRepo.AsQueryable()
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
                
                if (variant != null)
                {
                    // Subtract the quantity to revert the purchase
                    variant.Stock -= item.Quantity;
                    
                    // Validate stock doesn't go negative (safety check)
                    if (variant.Stock < 0)
                    {
                        throw new InvalidOperationException(
                            $"Cannot delete invoice: Reverting would result in negative stock for variant {variant.Id}. " +
                            $"Current stock: {variant.Stock + item.Quantity}, attempting to remove: {item.Quantity}");
                    }
                }
                itemRepo.Remove(item);
            }

            invoiceRepo.Remove(existing);
            await _uow.SaveChangesAsync();
            return true;
        }

        // ==================== PURCHASE RETURNS ====================

        public async Task<List<PurchaseReturnInvoice>> GetAllReturnsWithDetailsAsync()
        {
            return await _uow.Repository<PurchaseReturnInvoice>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseInvoice)
                .Include(p => p.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(p => p.ReturnDate)
                .ToListAsync();
        }

        public async Task<PurchaseReturnInvoice?> GetReturnByIdWithDetailsAsync(int id)
        {
            return await _uow.Repository<PurchaseReturnInvoice>()
                .AsQueryable()
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseInvoice)
                .Include(p => p.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PurchaseReturnInvoice?> AddReturnAsync(PurchaseReturnInvoice returnInvoice, IEnumerable<PurchaseReturnItem> items)
        {
            var returnRepo = _uow.Repository<PurchaseReturnInvoice>();
            var itemRepo = _uow.Repository<PurchaseReturnItem>();
            var variantRepo = _uow.Repository<ProductVariant>();

            await returnRepo.AddAsync(returnInvoice);
            await _uow.SaveChangesAsync();

            // Add items and decrease stock
            foreach (var item in items)
            {
                item.PurchaseReturnInvoiceId = returnInvoice.Id;
                await itemRepo.AddAsync(item);

                var variant = await variantRepo.AsQueryable()
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock -= item.Quantity;
                }
            }

            await _uow.SaveChangesAsync();
            return await GetReturnByIdWithDetailsAsync(returnInvoice.Id);
        }

        public async Task<bool> DeleteReturnAsync(int id)
        {
            var returnRepo = _uow.Repository<PurchaseReturnInvoice>();
            var itemRepo = _uow.Repository<PurchaseReturnItem>();
            var variantRepo = _uow.Repository<ProductVariant>();

            var existing = await returnRepo.AsQueryable()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing is null) return false;

            // Restore stock
            foreach (var item in existing.Items)
            {
                var variant = await variantRepo.AsQueryable()
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock += item.Quantity;
                }
                itemRepo.Remove(item);
            }

            returnRepo.Remove(existing);
            await _uow.SaveChangesAsync();
            return true;
        }

        // ==================== STATISTICS ====================

        public async Task<Dictionary<int, (decimal TotalPurchased, decimal TotalReturned, int InvoiceCount)>> GetVendorPurchaseStatsAsync()
        {
            var purchases = await _uow.Repository<PurchaseInvoice>()
                .AsQueryable()
                .GroupBy(p => p.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    TotalPurchased = g.Sum(p => p.TotalAmount),
                    InvoiceCount = g.Count()
                })
                .ToListAsync();

            var returns = await _uow.Repository<PurchaseReturnInvoice>()
                .AsQueryable()
                .GroupBy(r => r.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    TotalReturned = g.Sum(r => r.TotalRefund)
                })
                .ToListAsync();

            var result = new Dictionary<int, (decimal TotalPurchased, decimal TotalReturned, int InvoiceCount)>();

            foreach (var p in purchases)
            {
                var returned = returns.FirstOrDefault(r => r.VendorId == p.VendorId)?.TotalReturned ?? 0;
                result[p.VendorId] = (p.TotalPurchased, returned, p.InvoiceCount);
            }

            return result;
        }

        public async Task<Dictionary<int, (int ProductsSoldCount, decimal ProductsSoldValue)>> GetVendorSalesStatsAsync()
        {
            // Compute in the database to avoid large in-memory scans and reduce errors
            var grouped = await _uow.Repository<SalesItem>()
                .AsQueryable()
                .Include(si => si.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Where(si => si.ProductVariant != null && si.ProductVariant.Product != null)
                .GroupBy(si => si.ProductVariant.Product.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    ProductsSoldCount = g.Sum(si => si.Quantity),
                    ProductsSoldValue = g.Sum(si => (si.SellingPrice - si.DiscountValue) * si.Quantity)
                })
                .ToListAsync();

            var result = new Dictionary<int, (int ProductsSoldCount, decimal ProductsSoldValue)>();
            foreach (var row in grouped)
            {
                result[row.VendorId] = (row.ProductsSoldCount, row.ProductsSoldValue);
            }
            return result;
        }

        // ==================== HELPER METHODS ====================

        public async Task<List<Product>> GetProductsByVendorAsync(int vendorId)
        {
            return await _uow.Repository<Product>()
                .AsQueryable()
                .Include(p => p.Variants)
                .Where(p => p.VendorId == vendorId)
                .OrderBy(p => p.Code)
                .ToListAsync();
        }

        public async Task<ProductVariant?> GetOrCreateVariantAsync(int productId, string color, int size)
        {
            var variantRepo = _uow.Repository<ProductVariant>();

            // Try to find existing variant
            var existing = await variantRepo.AsQueryable()
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.Color == color && v.Size == size);

            if (existing != null)
                return existing;

            // Create new variant with 0 stock (will be updated by purchase)
            var newVariant = new ProductVariant
            {
                ProductId = productId,
                Color = color,
                Size = size,
                Stock = 0
            };

            await variantRepo.AddAsync(newVariant);
            await _uow.SaveChangesAsync();

            return newVariant;
        }
    }
}
