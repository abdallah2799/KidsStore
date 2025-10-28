using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Repositories;
using Domain.Entities;
using System.Diagnostics;
using System.Security.Claims;
using WebUI.Models;

namespace WebUI.Controllers
{
    // 🔒 Require login for this controller
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // Read data directly from claims (created at login)
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";

            // If somehow not authenticated, fallback to login
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction("Login", "Account");

            ViewBag.UserRole = role;
            return View();
        }

        // Dashboard Data Endpoints
        [HttpGet]
        public IActionResult GetDashboardSummary()
        {
            try
            {
                var today = DateTime.Now.Date;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var salesQuery = _unitOfWork.Repository<SalesInvoice>().AsQueryable();
                
                // Filter by cashier if not admin
                if (userRole == "Cashier")
                {
                    salesQuery = salesQuery.Where(i => i.SellerId == userId);
                }

                var todaySales = salesQuery
                    .Where(i => i.SaleDate.Date == today)
                    .Sum(i => i.TotalAmount);

                var monthSales = salesQuery
                    .Where(i => i.SaleDate >= thisMonth)
                    .Sum(i => i.TotalAmount);

                var todayInvoices = salesQuery
                    .Count(i => i.SaleDate.Date == today);

                var todayReturns = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .Where(r => r.ReturnDate.Date == today)
                    .Sum(r => r.TotalRefund);

                var lowStockCount = _unitOfWork.Repository<ProductVariant>()
                    .AsQueryable()
                    .Count(v => v.Stock <= 5);

                var summary = new
                {
                    TodaySales = todaySales,
                    MonthSales = monthSales,
                    TodayInvoices = todayInvoices,
                    TodayReturns = todayReturns,
                    LowStockCount = lowStockCount,
                    MyInvoicesCount = todayInvoices // For cashiers, this shows their invoices today
                };

                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetRecentSales(int count = 10)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var salesQuery = _unitOfWork.Repository<SalesInvoice>().AsQueryable();
                
                if (userRole == "Cashier")
                {
                    salesQuery = salesQuery.Where(i => i.SellerId == userId);
                }

                var sales = salesQuery
                    .OrderByDescending(i => i.SaleDate)
                    .Take(count)
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

                return Json(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sales");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetTopProducts(int count = 5)
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
                    .Take(count)
                    .ToList();

                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetSalesChart(int days = 7)
        {
            try
            {
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var startDate = DateTime.Now.AddDays(-days);
                var salesQuery = _unitOfWork.Repository<SalesInvoice>().AsQueryable();
                
                if (userRole == "Cashier")
                {
                    salesQuery = salesQuery.Where(i => i.SellerId == userId);
                }

                var data = salesQuery
                    .Where(i => i.SaleDate >= startDate)
                    .GroupBy(i => i.SaleDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { 
                        Date = g.Key.ToString("yyyy-MM-dd"), 
                        Total = g.Sum(i => i.TotalAmount),
                        Count = g.Count()
                    })
                    .ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetLowStockProducts()
        {
            try
            {
                var products = _unitOfWork.Repository<ProductVariant>()
                    .AsQueryable()
                    .Where(v => v.Stock < 15) // Show items with stock below 15
                    .Select(v => new
                    {
                        ProductCode = v.Product.Code,
                        ProductName = v.Product.Description,
                        v.Color,
                        v.Size,
                        v.Stock
                    })
                    .OrderBy(v => v.Stock)
                    .Take(10)
                    .ToList();

                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetRecentPurchases(int count = 10)
        {
            try
            {
                var purchases = _unitOfWork.Repository<PurchaseInvoice>()
                    .AsQueryable()
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(count)
                    .Select(p => new
                    {
                        p.Id,
                        p.PurchaseDate,
                        VendorName = p.Vendor != null ? p.Vendor.Name : "Unknown",
                        p.TotalAmount
                    })
                    .ToList();

                return Json(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent purchases");
                return BadRequest(new { error = ex.Message });
            }
        }

        // Report View Methods
        [HttpGet]
        public async Task<IActionResult> ViewSalesReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddMonths(-1);
                var end = (endDate ?? DateTime.Today).AddDays(1).AddSeconds(-1); // Include full end day

                var sales = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => new
                    {
                        s.Id,
                        s.SaleDate,
                        s.CustomerName,
                        s.PaymentMethod,
                        s.TotalAmount,
                        ItemCount = s.Items != null ? s.Items.Count : 0
                    })
                    .ToList();

                var totalSales = sales.Sum(s => s.TotalAmount);
                var totalInvoices = sales.Count;

                var html = GenerateSalesReportHtml(sales, start, end, totalSales, totalInvoices);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                return BadRequest($"Error generating report: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewInventoryReport()
        {
            try
            {
                var products = _unitOfWork.Repository<ProductVariant>()
                    .AsQueryable()
                    .OrderBy(pv => pv.Stock)
                    .Select(pv => new
                    {
                        ProductName = pv.Product != null ? pv.Product.Description : "Unknown",
                        pv.Color,
                        pv.Size,
                        pv.Stock,
                        BuyingPrice = pv.Product != null ? pv.Product.BuyingPrice : 0,
                        SellingPrice = pv.Product != null ? pv.Product.SellingPrice : 0,
                        TotalValue = pv.Stock * (pv.Product != null ? pv.Product.SellingPrice : 0)
                    })
                    .ToList();

                var totalItems = products.Sum(p => p.Stock);
                var totalValue = products.Sum(p => p.TotalValue);

                var html = GenerateInventoryReportHtml(products, totalItems, totalValue);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report");
                return BadRequest($"Error generating report: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewReturnsReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddMonths(-1);
                var end = (endDate ?? DateTime.Today).AddDays(1).AddSeconds(-1); // Include full end day

                var returns = _unitOfWork.Repository<ReturnInvoice>()
                    .AsQueryable()
                    .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
                    .OrderByDescending(r => r.ReturnDate)
                    .Select(r => new
                    {
                        r.Id,
                        r.ReturnDate,
                        r.TotalRefund,
                        OriginalInvoiceId = r.SalesInvoiceId,
                        ItemCount = r.Items != null ? r.Items.Count : 0
                    })
                    .ToList();

                var totalReturns = returns.Sum(r => r.TotalRefund);
                var totalCount = returns.Count;

                var html = GenerateReturnsReportHtml(returns, start, end, totalReturns, totalCount);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating returns report");
                return BadRequest($"Error generating report: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewCashierReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddMonths(-1);
                var end = (endDate ?? DateTime.Today).AddDays(1).AddSeconds(-1); // Include full end day

                var cashierStats = _unitOfWork.Repository<SalesInvoice>()
                    .AsQueryable()
                    .Where(s => s.SaleDate >= start && s.SaleDate <= end && s.Seller != null)
                    .GroupBy(s => new { s.SellerId, s.Seller!.UserName })
                    .Select(g => new
                    {
                        CashierName = g.Key.UserName,
                        TotalSales = g.Sum(s => s.TotalAmount),
                        InvoiceCount = g.Count(),
                        AverageTransaction = g.Average(s => s.TotalAmount)
                    })
                    .OrderByDescending(c => c.TotalSales)
                    .ToList();

                var totalSales = cashierStats.Sum(c => c.TotalSales);
                var totalInvoices = cashierStats.Sum(c => c.InvoiceCount);

                var html = GenerateCashierReportHtml(cashierStats, start, end, totalSales, totalInvoices);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cashier report");
                return BadRequest($"Error generating report: {ex.Message}");
            }
        }

        // Optional: shared route for testing authorization
        [Authorize(Roles = "Cashier,Admin")]
        public IActionResult TestAuth()
        {
            return Content($"✅ Authorized as {User.Identity?.Name} ({User.FindFirstValue(ClaimTypes.Role)})");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // HTML Report Generation Helper Methods
        private string GenerateSalesReportHtml(dynamic sales, DateTime startDate, DateTime endDate, decimal totalSales, int totalInvoices)
        {
            var rows = "";
            int counter = 1;
            foreach (var sale in sales)
            {
                rows += $@"
                <tr>
                    <td>{counter++}</td>
                    <td>#{sale.Id}</td>
                    <td>{Convert.ToDateTime(sale.SaleDate):MMM dd, yyyy hh:mm tt}</td>
                    <td>{sale.CustomerName ?? "Walk-in"}</td>
                    <td>{sale.PaymentMethod}</td>
                    <td>{sale.ItemCount}</td>
                    <td class='text-end'><strong>${sale.TotalAmount:F2}</strong></td>
                </tr>";
            }

            return GenerateReportHtml(
                "Sales Report",
                startDate,
                endDate,
                $@"
                <table class='report-table'>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Invoice</th>
                            <th>Date</th>
                            <th>Customer</th>
                            <th>Payment</th>
                            <th>Items</th>
                            <th class='text-end'>Amount</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>",
                $@"
                <div class='summary-card'>
                    <h4>📊 Summary</h4>
                    <div class='summary-row'>
                        <span>Total Invoices:</span>
                        <strong>{totalInvoices}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Total Sales:</span>
                        <strong class='text-success'>${totalSales:F2}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Average Transaction:</span>
                        <strong>${(totalInvoices > 0 ? totalSales / totalInvoices : 0):F2}</strong>
                    </div>
                </div>"
            );
        }

        private string GenerateInventoryReportHtml(dynamic products, int totalItems, decimal totalValue)
        {
            var rows = "";
            int counter = 1;
            foreach (var product in products)
            {
                var stockClass = product.Stock < 5 ? "text-danger" : (product.Stock < 10 ? "text-warning" : "");
                rows += $@"
                <tr>
                    <td>{counter++}</td>
                    <td>{product.ProductName}</td>
                    <td>{product.Color}</td>
                    <td>{product.Size}</td>
                    <td class='{stockClass}'><strong>{product.Stock}</strong></td>
                    <td>${product.BuyingPrice:F2}</td>
                    <td>${product.SellingPrice:F2}</td>
                    <td class='text-end'><strong>${product.TotalValue:F2}</strong></td>
                </tr>";
            }

            return GenerateReportHtml(
                "Inventory Report",
                null,
                null,
                $@"
                <table class='report-table'>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Product</th>
                            <th>Color</th>
                            <th>Size</th>
                            <th>Stock</th>
                            <th>Buying Price</th>
                            <th>Selling Price</th>
                            <th class='text-end'>Total Value</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>",
                $@"
                <div class='summary-card'>
                    <h4>📦 Summary</h4>
                    <div class='summary-row'>
                        <span>Total Items in Stock:</span>
                        <strong>{totalItems}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Total Inventory Value:</span>
                        <strong class='text-primary'>${totalValue:F2}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Product Variants:</span>
                        <strong>{products.Count}</strong>
                    </div>
                </div>"
            );
        }

        private string GenerateReturnsReportHtml(dynamic returns, DateTime startDate, DateTime endDate, decimal totalReturns, int totalCount)
        {
            var rows = "";
            int counter = 1;
            foreach (var ret in returns)
            {
                rows += $@"
                <tr>
                    <td>{counter++}</td>
                    <td>#{ret.Id}</td>
                    <td>{Convert.ToDateTime(ret.ReturnDate):MMM dd, yyyy hh:mm tt}</td>
                    <td>Invoice #{ret.OriginalInvoiceId}</td>
                    <td>{ret.ItemCount}</td>
                    <td class='text-end'><strong class='text-danger'>${ret.TotalRefund:F2}</strong></td>
                </tr>";
            }

            return GenerateReportHtml(
                "Returns Report",
                startDate,
                endDate,
                $@"
                <table class='report-table'>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Return ID</th>
                            <th>Date</th>
                            <th>Original Invoice</th>
                            <th>Items</th>
                            <th class='text-end'>Refund Amount</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>",
                $@"
                <div class='summary-card'>
                    <h4>🔄 Summary</h4>
                    <div class='summary-row'>
                        <span>Total Returns:</span>
                        <strong>{totalCount}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Total Refunded:</span>
                        <strong class='text-danger'>${totalReturns:F2}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Average Return:</span>
                        <strong>${(totalCount > 0 ? totalReturns / totalCount : 0):F2}</strong>
                    </div>
                </div>"
            );
        }

        private string GenerateCashierReportHtml(dynamic cashiers, DateTime startDate, DateTime endDate, decimal totalSales, int totalInvoices)
        {
            var rows = "";
            int counter = 1;
            foreach (var cashier in cashiers)
            {
                var percentage = totalSales > 0 ? (cashier.TotalSales / totalSales * 100) : 0;
                rows += $@"
                <tr>
                    <td>{counter++}</td>
                    <td><strong>{cashier.CashierName}</strong></td>
                    <td>{cashier.InvoiceCount}</td>
                    <td class='text-end'><strong>${cashier.TotalSales:F2}</strong></td>
                    <td>${cashier.AverageTransaction:F2}</td>
                    <td><span class='badge bg-info'>{percentage:F1}%</span></td>
                </tr>";
            }

            return GenerateReportHtml(
                "Cashier Performance Report",
                startDate,
                endDate,
                $@"
                <table class='report-table'>
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Cashier</th>
                            <th>Transactions</th>
                            <th class='text-end'>Total Sales</th>
                            <th>Avg. Transaction</th>
                            <th>% of Total</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>",
                $@"
                <div class='summary-card'>
                    <h4>👥 Summary</h4>
                    <div class='summary-row'>
                        <span>Total Cashiers:</span>
                        <strong>{cashiers.Count}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Total Transactions:</span>
                        <strong>{totalInvoices}</strong>
                    </div>
                    <div class='summary-row'>
                        <span>Total Sales:</span>
                        <strong class='text-success'>${totalSales:F2}</strong>
                    </div>
                </div>"
            );
        }

        private string GenerateReportHtml(string title, DateTime? startDate, DateTime? endDate, string tableHtml, string summaryHtml)
        {
            var dateRange = startDate.HasValue && endDate.HasValue
                ? $"<p class='date-range'>Period: {startDate.Value:MMM dd, yyyy} - {endDate.Value:MMM dd, yyyy}</p>"
                : "<p class='date-range'>Current Inventory Status</p>";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            padding: 30px;
            background: #f5f7fa;
        }}
        .report-container {{
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .report-header {{
            text-align: center;
            border-bottom: 3px solid #4facfe;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .report-header h1 {{
            color: #2c3e50;
            font-size: 32px;
            margin-bottom: 10px;
        }}
        .date-range {{
            color: #7f8c8d;
            font-size: 16px;
        }}
        .report-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .report-table th {{
            background: #4facfe;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: 600;
        }}
        .report-table td {{
            padding: 12px;
            border-bottom: 1px solid #ecf0f1;
        }}
        .report-table tbody tr:hover {{
            background: #f8f9fa;
        }}
        .text-end {{
            text-align: right;
        }}
        .text-danger {{
            color: #e74c3c;
        }}
        .text-warning {{
            color: #f39c12;
        }}
        .text-success {{
            color: #27ae60;
        }}
        .text-primary {{
            color: #3498db;
        }}
        .summary-card {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin-top: 30px;
        }}
        .summary-card h4 {{
            color: #2c3e50;
            margin-bottom: 15px;
            font-size: 20px;
        }}
        .summary-row {{
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #dee2e6;
        }}
        .summary-row:last-child {{
            border-bottom: none;
        }}
        .summary-row strong {{
            color: #2c3e50;
            font-size: 18px;
        }}
        .badge {{
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: 600;
        }}
        .bg-info {{
            background: #3498db;
            color: white;
        }}
        .print-button {{
            text-align: center;
            margin: 30px 0;
        }}
        .print-btn {{
            background: #4facfe;
            color: white;
            border: none;
            padding: 15px 40px;
            border-radius: 5px;
            cursor: pointer;
            font-size: 16px;
            font-weight: 600;
            box-shadow: 0 2px 5px rgba(0,0,0,0.2);
        }}
        .print-btn:hover {{
            background: #3b8fd6;
        }}
        .footer {{
            text-align: center;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            color: #95a5a6;
            font-size: 12px;
        }}
        @@media print {{
            body {{
                background: white;
                padding: 0;
            }}
            .report-container {{
                box-shadow: none;
                padding: 20px;
            }}
            .print-button {{
                display: none !important;
            }}
            .print-btn {{
                display: none !important;
            }}
            .report-table th {{
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }}
        }}
    </style>
</head>
<body>
    <div class='report-container'>
        <div class='report-header'>
            <h1>Kids Store</h1>
            <h2>{title}</h2>
            {dateRange}
        </div>

        {tableHtml}

        {summaryHtml}

        <div class='footer'>
            <p>Report generated on {DateTime.Now:MMMM dd, yyyy hh:mm tt}</p>
            <p>Kids Store Management System © 2025</p>
        </div>
    </div>

    <div class='print-button'>
        <button class='print-btn' onclick='window.print()'>Print Report</button>
    </div>

    <script>
        window.onload = function() {{
            document.querySelector('.print-btn').focus();
        }};
    </script>
</body>
</html>";
        }
    }
}
