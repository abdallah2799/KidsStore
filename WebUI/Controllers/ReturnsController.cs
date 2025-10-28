using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin,Cashier")]
    public class ReturnsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportService _reportService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(IUnitOfWork unitOfWork, IReportService reportService, ILogger<ReturnsController> logger)
        {
            _unitOfWork = unitOfWork;
            _reportService = reportService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SearchInvoices(string invoiceNo, string product, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                var query = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .AsEnumerable(); // Switch to in-memory for complex filtering

                // Filter by invoice number
                if (!string.IsNullOrEmpty(invoiceNo))
                {
                    query = query.Where(i => i.Id.ToString().Contains(invoiceNo));
                }

                // Filter by product (search in items)
                if (!string.IsNullOrEmpty(product))
                {
                    query = query.Where(i => i.Items.Any(item =>
                        item.ProductVariant.Product.Code.Contains(product, StringComparison.OrdinalIgnoreCase) ||
                        item.ProductVariant.Product.Description.Contains(product, StringComparison.OrdinalIgnoreCase)));
                }

                // Filter by date range
                if (dateFrom.HasValue)
                {
                    query = query.Where(i => i.SaleDate.Date >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(i => i.SaleDate.Date <= dateTo.Value.Date);
                }

                var invoices = query
                    .OrderByDescending(i => i.SaleDate)
                    .Take(50)
                    .Select(i => new
                    {
                        i.Id,
                        InvoiceNumber = i.Id.ToString(),
                        i.SaleDate,
                        i.CustomerName,
                        i.TotalAmount,
                        ItemsCount = i.Items.Count
                    })
                    .ToList();

                return Json(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching invoices");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetInvoiceDetails(int id)
        {
            try
            {
                var invoice = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.Id == id)
                    .Select(i => new
                    {
                        i.Id,
                        InvoiceNumber = i.Id.ToString(),
                        i.SaleDate,
                        i.CustomerName,
                        SellerName = i.Seller.UserName,
                        i.TotalAmount,
                        Items = i.Items.Select(item => new
                        {
                            item.Id,
                            ProductCode = item.ProductVariant.Product.Code,
                            ProductName = item.ProductVariant.Product.Description,
                            Color = item.ProductVariant.Color,
                            Size = item.ProductVariant.Size,
                            item.SellingPrice,
                            SoldQty = item.Quantity,
                            Discount = item.DiscountValue,
                            item.ProductVariantId
                        })
                    })
                    .FirstOrDefault();

                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found" });
                }

                return Json(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting invoice details for ID {id}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReturn([FromBody] ReturnRequestDto request)
        {
            try
            {
                if (request?.Items == null || !request.Items.Any())
                {
                    return BadRequest(new { success = false, message = "Return must have at least one item" });
                }

                // Get the original sales invoice
                var salesInvoice = await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(request.SalesInvoiceId);
                if (salesInvoice == null)
                {
                    return NotFound(new { success = false, message = "Sales invoice not found" });
                }

                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int processedBy = int.TryParse(userIdClaim, out var id) ? id : salesInvoice.SellerId;

                // Create return invoice
                var returnInvoice = new ReturnInvoice
                {
                    SalesInvoiceId = request.SalesInvoiceId,
                    ReturnDate = DateTime.Now,
                    TotalRefund = request.Items.Sum(i => i.RefundAmount),
                    Items = new List<ReturnItem>()
                };

                // Process each return item
                foreach (var itemDto in request.Items)
                {
                    // Get the original sales item
                    var salesItem = salesInvoice.Items.FirstOrDefault(i => i.Id == itemDto.SalesItemId);
                    if (salesItem == null)
                    {
                        return BadRequest(new { success = false, message = $"Sales item {itemDto.SalesItemId} not found" });
                    }

                    // Validate quantity
                    if (itemDto.Quantity > salesItem.Quantity)
                    {
                        return BadRequest(new { success = false, message = $"Return quantity exceeds sold quantity for item {itemDto.SalesItemId}" });
                    }

                    // Create return item
                    var returnItem = new ReturnItem
                    {
                        ProductVariantId = salesItem.ProductVariantId,
                        Quantity = itemDto.Quantity,
                        RefundAmount = itemDto.RefundAmount
                    };
                    returnInvoice.Items.Add(returnItem);

                    // Update stock (add back the returned quantity)
                    var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(salesItem.ProductVariantId);
                    if (variant != null)
                    {
                        variant.Stock += itemDto.Quantity;
                        _unitOfWork.Repository<ProductVariant>().Update(variant);
                    }

                    // Update sales item quantity (reduce by returned amount)
                    salesItem.Quantity -= itemDto.Quantity;
                    _unitOfWork.Repository<SalesItem>().Update(salesItem);
                }

                // Save return invoice
                await _unitOfWork.Repository<ReturnInvoice>().AddAsync(returnInvoice);
                
                // Update sales invoice total
                salesInvoice.TotalAmount -= returnInvoice.TotalRefund;
                _unitOfWork.Repository<SalesInvoice>().Update(salesInvoice);

                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    returnInvoiceId = returnInvoice.Id,
                    invoiceNumber = returnInvoice.Id.ToString(),
                    message = "Return processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return");
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult PrintReceipt(int id)
        {
            try
            {
                var returnInvoice = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .FirstOrDefault(r => r.Id == id);

                if (returnInvoice == null)
                {
                    return NotFound("Return invoice not found");
                }

                var html = _reportService.GenerateReturnReceipt(returnInvoice);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error printing return receipt for ID {id}");
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }

    // DTOs
    public class ReturnRequestDto
    {
        public int SalesInvoiceId { get; set; }
        public string? Notes { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        public int SalesItemId { get; set; }
        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
