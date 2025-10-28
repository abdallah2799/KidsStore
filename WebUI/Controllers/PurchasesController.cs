using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Models.ViewModels;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PurchasesController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly IVendorService _vendorService;
        private readonly ILogger<PurchasesController> _logger;

        public PurchasesController(IPurchaseService purchaseService, IVendorService vendorService, ILogger<PurchasesController> logger)
        {
            _purchaseService = purchaseService;
            _vendorService = vendorService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Purchases";
            return View();
        }

        // ==================== HELPER ENDPOINTS ====================

        [HttpGet]
        public async Task<IActionResult> GetProductsByVendor(int vendorId)
        {
            try
            {
                var products = await _purchaseService.GetProductsByVendorAsync(vendorId);
                var result = products.Select(p => new
                {
                    id = p.Id,
                    code = p.Code,
                    description = p.Description,
                    buyingPrice = p.BuyingPrice,
                    sellingPrice = p.SellingPrice,
                    discountLimit = p.DiscountLimit,
                    variants = p.Variants.Select(v => new
                    {
                        id = v.Id,
                        color = v.Color,
                        size = v.Size,
                        stock = v.Stock
                    }).ToList()
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for vendor {VendorId}", vendorId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateVariant([FromBody] CreateVariantRequest request)
        {
            try
            {
                var variant = await _purchaseService.GetOrCreateVariantAsync(request.ProductId, request.Color, request.Size);
                if (variant == null)
                    return BadRequest("Failed to create variant");

                return Json(new
                {
                    id = variant.Id,
                    productId = variant.ProductId,
                    color = variant.Color,
                    size = variant.Size,
                    stock = variant.Stock
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating variant");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== PURCHASE INVOICES ====================

        [HttpGet]
        public async Task<IActionResult> GetAllInvoices()
        {
            var invoices = await _purchaseService.GetAllInvoicesWithDetailsAsync();

            var result = invoices.Select(inv => new PurchaseInvoiceViewModel
            {
                Id = inv.Id,
                VendorId = inv.VendorId,
                VendorName = inv.Vendor?.Name,
                PurchaseDate = inv.PurchaseDate,
                TotalAmount = inv.TotalAmount,
                Items = inv.Items?.Select(item => new PurchaseItemViewModel
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,
                    ProductCode = item.ProductVariant?.Product?.Code,
                    ProductDescription = item.ProductVariant?.Product?.Description,
                    Color = item.ProductVariant?.Color,
                    Size = item.ProductVariant?.Size ?? 0,
                    Quantity = item.Quantity,
                    BuyingPrice = item.BuyingPrice
                }).ToList() ?? new List<PurchaseItemViewModel>()
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesByVendor(int vendorId)
        {
            try
            {
                var invoices = await _purchaseService.GetAllInvoicesWithDetailsAsync();
                var vendorInvoices = invoices.Where(inv => inv.VendorId == vendorId).ToList();

                var result = vendorInvoices.Select(inv => new
                {
                    id = inv.Id,
                    purchaseDate = inv.PurchaseDate.ToString("yyyy-MM-dd"),
                    totalAmount = inv.TotalAmount,
                    itemCount = inv.Items?.Count ?? 0,
                    items = inv.Items?.Select(item => new
                    {
                        id = item.Id,
                        productVariantId = item.ProductVariantId,
                        productCode = item.ProductVariant?.Product?.Code ?? "",
                        productDescription = item.ProductVariant?.Product?.Description ?? "",
                        color = item.ProductVariant?.Color ?? "",
                        size = item.ProductVariant?.Size ?? 0,
                        quantity = item.Quantity,
                        buyingPrice = item.BuyingPrice,
                        subtotal = item.Quantity * item.BuyingPrice
                    }).ToList()
                }).OrderByDescending(x => x.id).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices by vendor");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddInvoice([FromBody] PurchaseInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid invoice data");

            try
            {
                var invoice = new PurchaseInvoice
                {
                    VendorId = model.VendorId,
                    PurchaseDate = model.PurchaseDate,
                    TotalAmount = model.TotalAmount
                };

                var items = model.Items.Select(i => new PurchaseItem
                {
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    BuyingPrice = i.BuyingPrice
                }).ToList();

                var added = await _purchaseService.AddInvoiceAsync(invoice, items);
                if (added is null) return BadRequest("Could not add invoice");

                var result = new PurchaseInvoiceViewModel
                {
                    Id = added.Id,
                    VendorId = added.VendorId,
                    VendorName = added.Vendor?.Name,
                    PurchaseDate = added.PurchaseDate,
                    TotalAmount = added.TotalAmount,
                    Items = added.Items?.Select(item => new PurchaseItemViewModel
                    {
                        Id = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        ProductCode = item.ProductVariant?.Product?.Code,
                        ProductDescription = item.ProductVariant?.Product?.Description,
                        Color = item.ProductVariant?.Color,
                        Size = item.ProductVariant?.Size ?? 0,
                        Quantity = item.Quantity,
                        BuyingPrice = item.BuyingPrice
                    }).ToList() ?? new List<PurchaseItemViewModel>()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding purchase invoice");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInvoice([FromBody] PurchaseInvoiceViewModel model)
        {
            if (!ModelState.IsValid || model.Id <= 0)
                return BadRequest("Invalid invoice data");

            try
            {
                var invoice = new PurchaseInvoice
                {
                    Id = model.Id,
                    VendorId = model.VendorId,
                    PurchaseDate = model.PurchaseDate,
                    TotalAmount = model.TotalAmount
                };

                var items = model.Items.Select(i => new PurchaseItem
                {
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    BuyingPrice = i.BuyingPrice
                }).ToList();

                var updated = await _purchaseService.UpdateInvoiceAsync(invoice, items);
                if (updated is null) return NotFound();

                var result = new PurchaseInvoiceViewModel
                {
                    Id = updated.Id,
                    VendorId = updated.VendorId,
                    VendorName = updated.Vendor?.Name,
                    PurchaseDate = updated.PurchaseDate,
                    TotalAmount = updated.TotalAmount,
                    Items = updated.Items?.Select(item => new PurchaseItemViewModel
                    {
                        Id = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        ProductCode = item.ProductVariant?.Product?.Code,
                        ProductDescription = item.ProductVariant?.Product?.Description,
                        Color = item.ProductVariant?.Color,
                        Size = item.ProductVariant?.Size ?? 0,
                        Quantity = item.Quantity,
                        BuyingPrice = item.BuyingPrice
                    }).ToList() ?? new List<PurchaseItemViewModel>()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase invoice");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var ok = await _purchaseService.DeleteInvoiceAsync(id);
                return ok ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase invoice");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== PURCHASE RETURNS ====================

        [HttpGet]
        public async Task<IActionResult> GetAllReturns()
        {
            var returns = await _purchaseService.GetAllReturnsWithDetailsAsync();

            var result = returns.Select(ret => new PurchaseReturnInvoiceViewModel
            {
                Id = ret.Id,
                PurchaseInvoiceId = ret.PurchaseInvoiceId,
                VendorId = ret.VendorId,
                VendorName = ret.Vendor?.Name,
                ReturnDate = ret.ReturnDate,
                TotalRefund = ret.TotalRefund,
                Reason = ret.Reason,
                Items = ret.Items?.Select(item => new PurchaseReturnItemViewModel
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,
                    ProductCode = item.ProductVariant?.Product?.Code,
                    ProductDescription = item.ProductVariant?.Product?.Description,
                    Color = item.ProductVariant?.Color,
                    Size = item.ProductVariant?.Size ?? 0,
                    Quantity = item.Quantity,
                    RefundPrice = item.RefundPrice
                }).ToList() ?? new List<PurchaseReturnItemViewModel>()
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddReturn([FromBody] PurchaseReturnInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid return data");

            try
            {
                var returnInvoice = new PurchaseReturnInvoice
                {
                    PurchaseInvoiceId = model.PurchaseInvoiceId,
                    VendorId = model.VendorId,
                    ReturnDate = model.ReturnDate,
                    TotalRefund = model.TotalRefund,
                    Reason = model.Reason
                };

                var items = model.Items.Select(i => new PurchaseReturnItem
                {
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    RefundPrice = i.RefundPrice
                }).ToList();

                var added = await _purchaseService.AddReturnAsync(returnInvoice, items);
                if (added is null) return BadRequest("Could not add return");

                var result = new PurchaseReturnInvoiceViewModel
                {
                    Id = added.Id,
                    PurchaseInvoiceId = added.PurchaseInvoiceId,
                    VendorId = added.VendorId,
                    VendorName = added.Vendor?.Name,
                    ReturnDate = added.ReturnDate,
                    TotalRefund = added.TotalRefund,
                    Reason = added.Reason,
                    Items = added.Items?.Select(item => new PurchaseReturnItemViewModel
                    {
                        Id = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        ProductCode = item.ProductVariant?.Product?.Code,
                        ProductDescription = item.ProductVariant?.Product?.Description,
                        Color = item.ProductVariant?.Color,
                        Size = item.ProductVariant?.Size ?? 0,
                        Quantity = item.Quantity,
                        RefundPrice = item.RefundPrice
                    }).ToList() ?? new List<PurchaseReturnItemViewModel>()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding purchase return");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteReturn(int id)
        {
            try
            {
                var ok = await _purchaseService.DeleteReturnAsync(id);
                return ok ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase return");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== STATISTICS ====================

        [HttpGet]
        public async Task<IActionResult> GetVendorStats()
        {
            try
            {
                var purchaseStats = await _purchaseService.GetVendorPurchaseStatsAsync() 
                                    ?? new Dictionary<int, (decimal TotalPurchased, decimal TotalReturned, int InvoiceCount)>();
                var salesStats = await _purchaseService.GetVendorSalesStatsAsync() 
                                 ?? new Dictionary<int, (int ProductsSoldCount, decimal ProductsSoldValue)>();
                var vendors = await _vendorService.GetAllAsync() ?? new List<Vendor>();

                // Union of vendor IDs present in either purchases or sales
                var vendorIds = new HashSet<int>(purchaseStats.Keys);
                foreach (var vid in salesStats.Keys) vendorIds.Add(vid);

                var result = vendorIds
                    .Select(vid =>
                    {
                        var hasPurchase = purchaseStats.TryGetValue(vid, out var pVal);
                        var hasSales = salesStats.TryGetValue(vid, out var sVal);

                        return new VendorPurchaseStatsViewModel
                        {
                            VendorId = vid,
                            VendorName = vendors.FirstOrDefault(v => v.Id == vid)?.Name ?? "Unknown",
                            TotalPurchased = hasPurchase ? pVal.TotalPurchased : 0,
                            TotalReturned = hasPurchase ? pVal.TotalReturned : 0,
                            InvoiceCount = hasPurchase ? pVal.InvoiceCount : 0,
                            ProductsSoldCount = hasSales ? sVal.ProductsSoldCount : 0,
                            ProductsSoldValue = hasSales ? sVal.ProductsSoldValue : 0
                        };
                    })
                    .OrderBy(r => r.VendorName)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor stats");
                // Do not break the UI: return an empty array with 200 OK while logging the error
                return Json(Array.Empty<object>());
            }
        }
    }
}
