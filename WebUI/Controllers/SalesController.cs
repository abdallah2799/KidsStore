using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Services;
using Application.Interfaces.Repositories;
using Domain.Entities;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin,Cashier")]
    public class SalesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportService _reportService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(IUnitOfWork unitOfWork, IReportService reportService, ILogger<SalesController> logger)
        {
            _unitOfWork = unitOfWork;
            _reportService = reportService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Point of Sale";
            return View();
        }

            [HttpGet]
            public IActionResult Invoices()
            {
                ViewBag.Title = "Invoices";
                return View();
            }

        [HttpGet]
        public IActionResult SearchSalesInvoices(string query)
        {
            var invoices = _unitOfWork.Repository<SalesInvoice>()
                .AsQueryable()
                .Where(i => string.IsNullOrEmpty(query) || i.Id.ToString().Contains(query))
                .OrderByDescending(i => i.Id)
                .Take(50)
                .Select(i => new {
                    i.Id,
                    i.SaleDate,
                    i.CustomerName,
                    i.TotalAmount
                }).ToList();
            return Json(invoices);
        }

        [HttpGet]
        public IActionResult SearchReturnInvoices(string query)
        {
            var invoices = _unitOfWork.Repository<ReturnInvoice>()
                .AsQueryable()
                .Where(i => string.IsNullOrEmpty(query) || i.Id.ToString().Contains(query))
                .OrderByDescending(i => i.Id)
                .Take(50)
                .Select(i => new {
                    i.Id,
                    i.ReturnDate,
                    i.TotalRefund
                }).ToList();
            return Json(invoices);
        }

        [HttpGet]
        public IActionResult GetSalesInvoiceDetails(int id)
        {
            var invoice = _unitOfWork.Repository<SalesInvoice>()
                .AsQueryable()
                .Where(i => i.Id == id)
                .Select(i => new {
                    i.Id,
                    i.SaleDate,
                    i.CustomerName,
                    i.TotalAmount,
                    i.PaymentMethod,
                    SellerName = i.Seller.UserName,
                    Items = i.Items.Select(item => new {
                        ProductCode = item.ProductVariant.Product.Code,
                        ProductName = item.ProductVariant.Product.Description,
                        Color = item.ProductVariant.Color,
                        Size = item.ProductVariant.Size,
                        item.Quantity,
                        item.SellingPrice,
                        item.DiscountValue
                    })
                }).FirstOrDefault();
            if (invoice == null) return NotFound();
            return Json(invoice);
        }

        [HttpGet]
        public IActionResult GetReturnInvoiceDetails(int id)
        {
            var invoice = _unitOfWork.Repository<ReturnInvoice>()
                .AsQueryable()
                .Where(i => i.Id == id)
                .Select(i => new {
                    i.Id,
                    i.ReturnDate,
                    i.TotalRefund,
                    Items = i.Items.Select(item => new {
                        ProductCode = item.ProductVariant.Product.Code,
                        ProductName = item.ProductVariant.Product.Description,
                        Color = item.ProductVariant.Color,
                        Size = item.ProductVariant.Size,
                        item.Quantity,
                        item.RefundAmount
                    })
                }).FirstOrDefault();
            if (invoice == null) return NotFound();
            return Json(invoice);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesInvoiceDto invoiceDto)
        {
            try
            {
                if (invoiceDto?.Items == null || !invoiceDto.Items.Any())
                {
                    return BadRequest("Invoice must have at least one item");
                }

                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int sellerId = int.TryParse(userIdClaim, out var id) ? id : invoiceDto.SellerId;

                // Create invoice entity
                var invoice = new SalesInvoice
                {
                    SellerId = sellerId,
                    PaymentMethod = invoiceDto.PaymentMethod ?? "Cash",
                    CustomerName = invoiceDto.CustomerName,
                    TotalAmount = invoiceDto.TotalAmount,
                    SaleDate = DateTime.Now,
                    Items = new List<SalesItem>()
                };

                // Add items
                foreach (var itemDto in invoiceDto.Items)
                {
                    var item = new SalesItem
                    {
                        ProductVariantId = itemDto.ProductVariantId,
                        Quantity = itemDto.Quantity,
                        SellingPrice = itemDto.SellingPrice,
                        DiscountValue = itemDto.DiscountValue
                    };
                    invoice.Items.Add(item);

                    // Update stock
                    var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(itemDto.ProductVariantId);
                    if (variant != null)
                    {
                        variant.Stock -= itemDto.Quantity;
                        _unitOfWork.Repository<ProductVariant>().Update(variant);
                    }
                }

                // Save to database
                await _unitOfWork.Repository<SalesInvoice>().AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { id = invoice.Id, message = "Invoice saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales invoice");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateReceipt(int id)
        {
            try
            {
                var receiptBytes = await _reportService.GenerateSalesReceiptAsync(id);
                
                // For now, the service returns HTML. In a production environment, you would convert this to PDF
                // You can use libraries like SelectPdf, IronPdf, or Puppeteer Sharp for HTML to PDF conversion
                
                return File(receiptBytes, "text/html", $"Receipt_{id}.html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt");
                return BadRequest($"Error generating receipt: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult ViewReceipt(int id)
        {
            try
            {
                var receiptHtml = _reportService.GenerateSalesReceiptHtml(id);
                return Content(receiptHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing receipt");
                return BadRequest($"Error viewing receipt: {ex.Message}");
            }
        }
    }

    // DTOs
    public class SalesInvoiceDto
    {
        public int SellerId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SalesItemDto> Items { get; set; } = new();
    }

    public class SalesItemDto
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountValue { get; set; }
    }
}
