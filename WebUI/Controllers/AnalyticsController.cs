using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Repositories;
using Domain.Entities;
using System.Linq;

namespace WebUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IUnitOfWork unitOfWork, ILogger<AnalyticsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Chart Data Endpoints
        [HttpGet]
        public IActionResult SalesOverTimeData(int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);
                var invoices = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.SaleDate >= startDate)
                    .GroupBy(i => i.SaleDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { 
                        Date = g.Key.ToString("yyyy-MM-dd"), 
                        Total = g.Sum(i => i.TotalAmount) 
                    })
                    .ToList();
                return Json(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales over time data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TopProductsData(int top = 10)
        {
            try
            {
                var items = _unitOfWork.Repository<SalesItem>()
                    .AsQueryable()
                    .GroupBy(item => new { 
                        item.ProductVariant.Product.Code,
                        item.ProductVariant.Product.Description 
                    })
                    .Select(g => new { 
                        Product = g.Key.Code + " - " + g.Key.Description,
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.SellingPrice * x.Quantity - x.DiscountValue)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(top)
                    .ToList();
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ReturnsOverTimeData(int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);
                var returns = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .Where(r => r.ReturnDate >= startDate)
                    .GroupBy(r => r.ReturnDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { 
                        Date = g.Key.ToString("yyyy-MM-dd"), 
                        Total = g.Sum(r => r.TotalRefund),
                        Count = g.Count()
                    })
                    .ToList();
                return Json(returns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting returns over time data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CashierPerformanceData()
        {
            try
            {
                var invoices = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .GroupBy(i => i.Seller.UserName)
                    .Select(g => new { 
                        Cashier = g.Key,
                        TotalSales = g.Sum(i => i.TotalAmount),
                        InvoiceCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToList();
                return Json(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cashier performance data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PaymentMethodsData()
        {
            try
            {
                var invoices = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .GroupBy(i => i.PaymentMethod)
                    .Select(g => new { 
                        Method = g.Key,
                        Total = g.Sum(i => i.TotalAmount),
                        Count = g.Count()
                    })
                    .ToList();
                return Json(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PurchasesOverTimeData(int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);
                var purchases = _unitOfWork.Repository<PurchaseInvoice>()
                    .AsQueryable()
                    .Where(p => p.PurchaseDate >= startDate)
                    .GroupBy(p => p.PurchaseDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { 
                        Date = g.Key.ToString("yyyy-MM-dd"), 
                        Total = g.Sum(p => p.TotalAmount)
                    })
                    .ToList();
                return Json(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchases over time data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult StockLevelsData()
        {
            try
            {
                var products = _unitOfWork.Repository<Product>()
                    .AsQueryable()
                    .Select(p => new {
                        Product = p.Code + " - " + p.Description,
                        Stock = p.Variants.Sum(v => v.Stock)
                    })
                    .OrderBy(p => p.Stock)
                    .Take(20)
                    .ToList();
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock levels data");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DashboardSummary()
        {
            try
            {
                var today = DateTime.Now.Date;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var todaySales = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.SaleDate.Date == today)
                    .Sum(i => i.TotalAmount);

                var monthSales = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.SaleDate >= thisMonth)
                    .Sum(i => i.TotalAmount);

                var todayReturns = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .Where(r => r.ReturnDate.Date == today)
                    .Sum(r => r.TotalRefund);

                var lowStockCount = _unitOfWork.Repository<ProductVariant>()
                    .AsQueryable()
                    .Count(v => v.Stock < 10);

                var summary = new
                {
                    TodaySales = todaySales,
                    MonthSales = monthSales,
                    TodayReturns = todayReturns,
                    LowStockItems = lowStockCount
                };

                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return BadRequest(new { error = ex.Message });
            }
        }

        // Exportable Reports Endpoint
        [HttpGet]
        public IActionResult ExportSalesReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var end = endDate ?? DateTime.Now;

                var sales = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.SaleDate >= start && i.SaleDate <= end)
                    .OrderByDescending(i => i.SaleDate)
                    .Select(i => new
                    {
                        i.Id,
                        i.SaleDate,
                        Cashier = i.Seller.UserName,
                        i.CustomerName,
                        i.PaymentMethod,
                        i.TotalAmount
                    })
                    .ToList();

                // For now, return as JSON. Later we'll add PDF/Excel export
                return Json(new { 
                    reportType = "Sales Report",
                    period = $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}",
                    data = sales,
                    total = sales.Sum(s => s.TotalAmount)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportInventoryReport()
        {
            try
            {
                var inventory = _unitOfWork.Repository<ProductVariant>()
                    .AsQueryable()
                    .Select(v => new
                    {
                        ProductCode = v.Product.Code,
                        ProductName = v.Product.Description,
                        v.Color,
                        v.Size,
                        v.Stock,
                        BuyingPrice = v.Product.BuyingPrice,
                        SellingPrice = v.Product.SellingPrice,
                        Value = v.Stock * v.Product.BuyingPrice
                    })
                    .OrderBy(v => v.ProductCode)
                    .ToList();

                return Json(new { 
                    reportType = "Inventory Report",
                    generatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    data = inventory,
                    totalValue = inventory.Sum(i => i.Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportReturnsReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var end = endDate ?? DateTime.Now;

                var returns = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
                    .OrderByDescending(r => r.ReturnDate)
                    .Select(r => new
                    {
                        r.Id,
                        r.ReturnDate,
                        OriginalInvoice = r.SalesInvoiceId,
                        CustomerName = r.SalesInvoice.CustomerName,
                        r.TotalRefund,
                        ItemCount = r.Items.Count
                    })
                    .ToList();

                return Json(new { 
                    reportType = "Returns Report",
                    period = $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}",
                    data = returns,
                    totalRefund = returns.Sum(r => r.TotalRefund)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting returns report");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportCashierReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var end = endDate ?? DateTime.Now;

                var cashierStats = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(i => i.SaleDate >= start && i.SaleDate <= end)
                    .GroupBy(i => i.Seller.UserName)
                    .Select(g => new
                    {
                        Cashier = g.Key,
                        TotalSales = g.Sum(i => i.TotalAmount),
                        InvoiceCount = g.Count(),
                        AverageInvoice = g.Average(i => i.TotalAmount),
                        TotalDiscount = g.SelectMany(i => i.Items).Sum(item => item.DiscountValue)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToList();

                return Json(new { 
                    reportType = "Cashier Performance Report",
                    period = $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}",
                    data = cashierStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting cashier report");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
