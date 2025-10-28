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

        [HttpGet]
        public async Task<IActionResult> ViewPurchaseInvoice(int id)
        {
            try
            {
                var invoice = await _purchaseService.GetInvoiceByIdWithDetailsAsync(id);
                if (invoice == null)
                {
                    return NotFound();
                }

                var html = GeneratePurchaseInvoiceHtml(invoice);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing purchase invoice");
                return BadRequest($"Error viewing invoice: {ex.Message}");
            }
        }

        private string GeneratePurchaseInvoiceHtml(PurchaseInvoice invoice)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Purchase Invoice #{invoice.Id}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 800px;
            margin: 20px auto;
            padding: 20px;
            background: #f5f5f5;
        }}
        .invoice {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #4facfe;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            color: #4facfe;
            margin: 0;
            font-size: 32px;
        }}
        .header p {{
            color: #666;
            margin: 5px 0;
        }}
        .info-section {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 30px;
        }}
        .info-box {{
            padding: 15px;
            background: #f8f9fa;
            border-radius: 5px;
        }}
        .info-box h3 {{
            margin: 0 0 10px 0;
            color: #333;
            font-size: 14px;
            text-transform: uppercase;
        }}
        .info-box p {{
            margin: 5px 0;
            color: #666;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        th {{
            background: #4facfe;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: 600;
        }}
        td {{
            padding: 12px;
            border-bottom: 1px solid #dee2e6;
        }}
        tr:hover {{
            background: #f8f9fa;
        }}
        .totals {{
            text-align: right;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 2px solid #dee2e6;
        }}
        .totals .total-row {{
            display: flex;
            justify-content: flex-end;
            margin: 10px 0;
        }}
        .totals .total-label {{
            margin-right: 20px;
            font-weight: 600;
            color: #666;
        }}
        .totals .total-value {{
            min-width: 150px;
            text-align: right;
            font-weight: 700;
            color: #333;
        }}
        .grand-total {{
            font-size: 24px;
            color: #4facfe !important;
            padding: 15px;
            background: #f8f9fa;
            border-radius: 5px;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            text-align: center;
            color: #999;
            font-size: 12px;
        }}
        .no-print {{
            text-align: center;
            margin: 20px 0;
        }}
        .print-btn {{
            background: #4facfe;
            color: white;
            border: none;
            padding: 12px 30px;
            border-radius: 5px;
            cursor: pointer;
            font-size: 16px;
            font-weight: 600;
        }}
        .print-btn:hover {{
            background: #3b8fd6;
        }}
        @media print {{
            body {{
                background: white;
                margin: 0;
                padding: 0;
            }}
            .invoice {{
                box-shadow: none;
                padding: 20px;
            }}
            .no-print {{
                display: none;
            }}
        }}
    </style>
</head>
<body>
    <div class='invoice'>
        <div class='header'>
            <h1>🛒 PURCHASE INVOICE</h1>
            <p>Invoice #: {invoice.Id}</p>
            <p>Date: {invoice.PurchaseDate:MMMM dd, yyyy - hh:mm tt}</p>
        </div>

        <div class='info-section'>
            <div class='info-box'>
                <h3>📦 Store Information</h3>
                <p><strong>Kids Store</strong></p>
                <p>Business Management System</p>
            </div>
            <div class='info-box'>
                <h3>🏢 Vendor Information</h3>
                <p><strong>{invoice.Vendor?.Name ?? "N/A"}</strong></p>
                <p>{invoice.Vendor?.ContactInfo ?? "N/A"}</p>
            </div>
        </div>

        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Product</th>
                    <th>Color</th>
                    <th>Size</th>
                    <th>Quantity</th>
                    <th>Unit Price</th>
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>";

            int itemNumber = 1;
            foreach (var item in invoice.Items ?? Enumerable.Empty<PurchaseItem>())
            {
                var total = item.Quantity * item.BuyingPrice;
                var productName = item.ProductVariant?.Product?.Description ?? "Unknown Product";
                var color = item.ProductVariant?.Color ?? "N/A";
                var size = item.ProductVariant?.Size ?? 0;

                html += $@"
                <tr>
                    <td>{itemNumber++}</td>
                    <td>{productName}</td>
                    <td>{color}</td>
                    <td>{size}</td>
                    <td>{item.Quantity}</td>
                    <td>${item.BuyingPrice:F2}</td>
                    <td>${total:F2}</td>
                </tr>";
            }

            html += $@"
            </tbody>
        </table>

        <div class='totals'>
            <div class='total-row grand-total'>
                <span class='total-label'>TOTAL AMOUNT:</span>
                <span class='total-value'>${invoice.TotalAmount:F2}</span>
            </div>
        </div>

        <div class='footer'>
            <p>Thank you for your business!</p>
            <p>Purchase Invoice generated on {DateTime.Now:MMMM dd, yyyy}</p>
        </div>
    </div>

    <div class='no-print'>
        <button class='print-btn' onclick='window.print()'>🖨️ Print Invoice</button>
    </div>

    <script>
        // Auto focus for printing
        window.onload = function() {{
            document.querySelector('.print-btn').focus();
        }};
    </script>
</body>
</html>";

            return html;
        }
    }
}
