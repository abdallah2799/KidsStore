using Application.Interfaces.Services;
using Application.Interfaces.Repositories;
using Domain.Entities;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<byte[]> GeneratePdfAsync(object reportData, string templateName)
        {
            // This is a placeholder implementation
            // In a real-world scenario, you would use a PDF library like QuestPDF, iTextSharp, or similar
            throw new NotImplementedException("PDF generation requires a PDF library like QuestPDF");
        }

        public async Task<byte[]> GenerateExcelAsync(object reportData, string sheetName)
        {
            // This is a placeholder implementation
            // In a real-world scenario, you would use a library like ClosedXML or EPPlus
            throw new NotImplementedException("Excel generation requires a library like ClosedXML");
        }

        public async Task<byte[]> GenerateSalesReceiptAsync(int invoiceId)
        {
            // Fetch the sales invoice with all related data
            var invoice = await _unitOfWork.SalesInvoices
                .AsQueryable()
                .Where(si => si.Id == invoiceId)
                .Include(si => si.Items)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(si => si.Seller)
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                throw new ArgumentException($"Sales invoice with ID {invoiceId} not found.");
            }

            // Generate a simple HTML-based receipt that can be converted to PDF
            var html = GenerateSalesReceiptHtml(invoice);
            
            // Convert HTML to PDF
            // This requires a library like SelectPdf, IronPdf, or similar
            // For now, return HTML as bytes
            return Encoding.UTF8.GetBytes(html);
        }

        public string GenerateSalesReceiptHtml(int invoiceId)
        {
            var invoice = _unitOfWork.SalesInvoices
                .AsQueryable()
                .Where(si => si.Id == invoiceId)
                .Include(si => si.Items)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(si => si.Seller)
                .FirstOrDefault();

            if (invoice == null)
            {
                return "<html><body><h1>Invoice not found</h1></body></html>";
            }

            return GenerateSalesReceiptHtml(invoice);
        }

        public async Task<byte[]> GeneratePurchaseInvoiceAsync(int invoiceId)
        {
            // Fetch the purchase invoice with all related data
            var invoice = await _unitOfWork.PurchaseInvoices
                .AsQueryable()
                .Where(pi => pi.Id == invoiceId)
                .Include(pi => pi.Items)
                    .ThenInclude(item => item.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(pi => pi.Vendor)
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                throw new ArgumentException($"Purchase invoice with ID {invoiceId} not found.");
            }

            // Generate a simple HTML-based invoice that can be converted to PDF
            var html = GeneratePurchaseInvoiceHtml(invoice);
            
            return Encoding.UTF8.GetBytes(html);
        }

        private string GenerateSalesReceiptHtml(SalesInvoice invoice)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html><head>");
            sb.Append("<meta charset='utf-8'>");
            sb.Append("<title>Sales Receipt</title>");
            sb.Append("<style>");
            sb.Append("* { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.Append("body { font-family: Arial, sans-serif; margin: 40px; background: #fff; }");
            sb.Append(".header { text-align: center; margin-bottom: 30px; }");
            sb.Append(".header h1 { margin: 0; color: #333; font-size: 28px; }");
            sb.Append(".header p { color: #666; font-size: 16px; margin-top: 5px; }");
            sb.Append(".info { margin-bottom: 20px; }");
            sb.Append(".info-row { display: flex; justify-content: space-between; margin: 5px 0; padding: 5px 0; }");
            sb.Append("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.Append("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            sb.Append("th { background-color: #4CAF50; color: white; }");
            sb.Append(".color-box { display: inline-block; width: 30px; height: 30px; border: 1px solid #333; border-radius: 4px; vertical-align: middle; }");
            sb.Append(".total-section { margin-top: 20px; float: right; width: 300px; }");
            sb.Append(".total-row { display: flex; justify-content: space-between; padding: 5px 0; }");
            sb.Append(".total-row.grand { font-weight: bold; font-size: 1.2em; border-top: 2px solid #333; padding-top: 10px; margin-top: 10px; }");
            sb.Append("-webkit-print-color-adjust: exact; print-color-adjust: exact;");
            sb.Append(".footer { text-align: center; margin-top: 80px; font-size: 0.9em; color: #666; clear: both; padding-top: 20px; border-top: 1px solid #ddd; }");
            sb.Append(".print-btn { padding: 10px 20px; background: #4CAF50; color: white; border: none; border-radius: 5px; cursor: pointer; font-size: 16px; margin: 20px auto; display: block; }");
            sb.Append(".print-btn:hover { background: #45a049; }");
            sb.Append("@media print {");
            sb.Append("  body { margin: 20px; }");
            sb.Append("  .no-print, .print-btn { display: none !important; }");
            sb.Append("  @page { margin: 1cm; size: A4 portrait; }");
            sb.Append("}");
            sb.Append("</style>");
            sb.Append("<script>");
            sb.Append("function printReceipt() { window.print(); }");
            sb.Append("</script>");
            sb.Append("</head><body>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>5alo Store - خالو ستور</h1>");
            sb.Append("<p>Sales Receipt</p>");
            sb.Append("</div>");
            
            // Print button (hidden when printing)
            sb.Append("<button class='print-btn no-print' onclick='printReceipt()'>🖨️ Print Receipt</button>");

            // Invoice Info
            sb.Append("<div class='info'>");
            sb.Append($"<div class='info-row'><strong>Receipt No:</strong> <span>{invoice.Id}</span></div>");
            sb.Append($"<div class='info-row'><strong>Date:</strong> <span>{invoice.SaleDate:dd/MM/yyyy HH:mm}</span></div>");
            
            if (!string.IsNullOrWhiteSpace(invoice.CustomerName))
            {
                sb.Append($"<div class='info-row'><strong>Customer:</strong> <span>{invoice.CustomerName}</span></div>");
            }
            
            sb.Append($"<div class='info-row'><strong>Cashier:</strong> <span>{invoice.Seller?.UserName ?? "N/A"}</span></div>");
            sb.Append($"<div class='info-row'><strong>Payment Method:</strong> <span>{invoice.PaymentMethod}</span></div>");
            sb.Append("</div>");

            // Items Table
            sb.Append("<table>");
            sb.Append("<thead><tr>");
            sb.Append("<th>Product</th>");
            sb.Append("<th>Color</th>");
            sb.Append("<th>Size</th>");
            sb.Append("<th>Unit Price</th>");
            sb.Append("<th>Qty</th>");
            sb.Append("<th>Discount</th>");
            sb.Append("<th>Total</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            decimal subtotal = 0;
            decimal totalDiscount = 0;

            foreach (var item in invoice.Items)
            {
                var itemTotal = item.SellingPrice * item.Quantity;
                var itemDiscount = itemTotal * (item.DiscountValue / 100);
                var itemNetTotal = itemTotal - itemDiscount;

                subtotal += itemTotal;
                totalDiscount += itemDiscount;

                sb.Append("<tr>");
                sb.Append($"<td>{item.ProductVariant?.Product?.Description ?? "N/A"}</td>");
                
                // Display color as a colored box
                var colorHex = item.ProductVariant?.Color ?? "#CCCCCC";
                sb.Append($"<td><span class='color-box' style='background-color: {colorHex};'></span></td>");
                
                sb.Append($"<td>{item.ProductVariant?.Size ?? 0}</td>");
                sb.Append($"<td>{item.SellingPrice:F2} EGP</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td>{item.DiscountValue:F2}%</td>");
                sb.Append($"<td>{itemNetTotal:F2} EGP</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            // Totals
            sb.Append("<div class='total-section'>");
            sb.Append($"<div class='total-row'><span>Subtotal:</span><span>{subtotal:F2} EGP</span></div>");
            sb.Append($"<div class='total-row'><span>Total Discount:</span><span>{totalDiscount:F2} EGP</span></div>");
            sb.Append($"<div class='total-row grand'><span>Net Total:</span><span>{invoice.TotalAmount:F2} EGP</span></div>");
            sb.Append("</div>");

            // Footer
            sb.Append("<div style='clear:both;'></div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p>Thank you for your purchase!</p>");
            sb.Append("<p>Kids Store Management System</p>");
            sb.Append("</div>");

            sb.Append("</body></html>");

            return sb.ToString();
        }

        private string GeneratePurchaseInvoiceHtml(PurchaseInvoice invoice)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html><head>");
            sb.Append("<meta charset='utf-8'>");
            sb.Append("<title>Purchase Invoice</title>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; margin: 40px; }");
            sb.Append(".header { text-align: center; margin-bottom: 30px; }");
            sb.Append(".header h1 { margin: 0; color: #333; }");
            sb.Append(".info { margin-bottom: 20px; }");
            sb.Append(".info-row { display: flex; justify-content: space-between; margin: 5px 0; }");
            sb.Append("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.Append("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            sb.Append("th { background-color: #2196F3; color: white; }");
            sb.Append(".total-section { margin-top: 20px; float: right; width: 300px; }");
            sb.Append(".total-row { display: flex; justify-content: space-between; padding: 5px 0; }");
            sb.Append(".total-row.grand { font-weight: bold; font-size: 1.2em; border-top: 2px solid #333; padding-top: 10px; }");
            sb.Append(".footer { text-align: center; margin-top: 50px; font-size: 0.9em; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head><body>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>Kids Store</h1>");
            sb.Append("<p>Purchase Invoice</p>");
            sb.Append("</div>");

            // Invoice Info
            sb.Append("<div class='info'>");
            sb.Append($"<div class='info-row'><strong>Invoice No:</strong> <span>{invoice.Id}</span></div>");
            sb.Append($"<div class='info-row'><strong>Date:</strong> <span>{invoice.PurchaseDate:dd/MM/yyyy}</span></div>");
            sb.Append($"<div class='info-row'><strong>Vendor:</strong> <span>{invoice.Vendor?.Name ?? "N/A"}</span></div>");
            sb.Append("</div>");

            // Items Table
            sb.Append("<table>");
            sb.Append("<thead><tr>");
            sb.Append("<th>Product</th>");
            sb.Append("<th>Color</th>");
            sb.Append("<th>Size</th>");
            sb.Append("<th>Unit Price</th>");
            sb.Append("<th>Qty</th>");
            sb.Append("<th>Total</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            decimal total = 0;

            foreach (var item in invoice.Items)
            {
                var itemTotal = item.BuyingPrice * item.Quantity;
                total += itemTotal;

                sb.Append("<tr>");
                sb.Append($"<td>{item.ProductVariant?.Product?.Description ?? "N/A"}</td>");
                sb.Append($"<td>{item.ProductVariant?.Color ?? "N/A"}</td>");
                sb.Append($"<td>{item.ProductVariant?.Size ?? 0}</td>");
                sb.Append($"<td>{item.BuyingPrice:F2} EGP</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td>{itemTotal:F2} EGP</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            // Totals
            sb.Append("<div class='total-section'>");
            sb.Append($"<div class='total-row grand'><span>Total:</span><span>{invoice.TotalAmount:F2} EGP</span></div>");
            sb.Append("</div>");

            // Footer
            sb.Append("<div style='clear:both;'></div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p>Kids Store Management System</p>");
            sb.Append("</div>");

            sb.Append("</body></html>");

            return sb.ToString();
        }

        public string GenerateReturnReceipt(ReturnInvoice returnInvoice)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html><head>");
            sb.Append("<meta charset='utf-8'>");
            sb.Append("<title>Return Receipt</title>");
            sb.Append("<style>");
            sb.Append("* { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.Append("body { font-family: Arial, sans-serif; margin: 40px; background: #fff; }");
            sb.Append(".header { text-align: center; margin-bottom: 30px; }");
            sb.Append(".header h1 { margin: 0; color: #d32f2f; font-size: 28px; }");
            sb.Append(".header p { color: #666; font-size: 16px; margin-top: 5px; }");
            sb.Append(".info { margin-bottom: 20px; background: #fff3e0; padding: 15px; border-left: 4px solid #ff9800; }");
            sb.Append(".info-row { display: flex; justify-content: space-between; margin: 5px 0; padding: 5px 0; }");
            sb.Append("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.Append("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            sb.Append("th { background-color: #d32f2f; color: white; }");
            sb.Append(".color-box { display: inline-block; width: 30px; height: 30px; border: 1px solid #333; border-radius: 4px; vertical-align: middle; }");
            sb.Append(".total-section { margin-top: 20px; float: right; width: 300px; background: #ffebee; padding: 15px; border-radius: 5px; }");
            sb.Append(".total-row { display: flex; justify-content: space-between; padding: 5px 0; }");
            sb.Append(".total-row.grand { font-weight: bold; font-size: 1.3em; color: #d32f2f; border-top: 2px solid #d32f2f; padding-top: 10px; margin-top: 10px; }");
            sb.Append("-webkit-print-color-adjust: exact; print-color-adjust: exact;");
            sb.Append(".footer { text-align: center; margin-top: 80px; font-size: 0.9em; color: #666; clear: both; padding-top: 20px; border-top: 1px solid #ddd; }");
            sb.Append(".print-btn { padding: 10px 20px; background: #d32f2f; color: white; border: none; border-radius: 5px; cursor: pointer; font-size: 16px; margin: 20px auto; display: block; }");
            sb.Append(".print-btn:hover { background: #b71c1c; }");
            sb.Append("@media print {");
            sb.Append("  body { margin: 20px; }");
            sb.Append("  .no-print, .print-btn { display: none !important; }");
            sb.Append("  @page { margin: 1cm; size: A4 portrait; }");
            sb.Append("}");
            sb.Append("</style>");
            sb.Append("<script>");
            sb.Append("function printReceipt() { window.print(); }");
            sb.Append("</script>");
            sb.Append("</head><body>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>5alo Store - خالو ستور</h1>");
            sb.Append("<p>⚠️ RETURN RECEIPT ⚠️</p>");
            sb.Append("</div>");
            
            // Print button (hidden when printing)
            sb.Append("<button class='print-btn no-print' onclick='printReceipt()'>🖨️ Print Receipt</button>");

            // Return Info
            sb.Append("<div class='info'>");
            sb.Append($"<div class='info-row'><strong>Return Receipt No:</strong> <span>R-{returnInvoice.Id}</span></div>");
            sb.Append($"<div class='info-row'><strong>Original Invoice No:</strong> <span>{returnInvoice.SalesInvoiceId}</span></div>");
            sb.Append($"<div class='info-row'><strong>Return Date:</strong> <span>{returnInvoice.ReturnDate:dd/MM/yyyy HH:mm}</span></div>");
            
            if (returnInvoice.SalesInvoice != null && !string.IsNullOrWhiteSpace(returnInvoice.SalesInvoice.CustomerName))
            {
                sb.Append($"<div class='info-row'><strong>Customer:</strong> <span>{returnInvoice.SalesInvoice.CustomerName}</span></div>");
            }
            
            sb.Append("</div>");

            // Items Table
            sb.Append("<table>");
            sb.Append("<thead><tr>");
            sb.Append("<th>Product</th>");
            sb.Append("<th>Color</th>");
            sb.Append("<th>Size</th>");
            sb.Append("<th>Qty Returned</th>");
            sb.Append("<th>Refund Amount</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            foreach (var item in returnInvoice.Items)
            {
                sb.Append("<tr>");
                sb.Append($"<td><strong>{item.ProductVariant?.Product?.Code ?? "N/A"}</strong><br><small>{item.ProductVariant?.Product?.Description ?? "N/A"}</small></td>");
                
                // Display color as a colored box
                var colorHex = item.ProductVariant?.Color ?? "#CCCCCC";
                sb.Append($"<td><span class='color-box' style='background-color: {colorHex};'></span></td>");
                
                sb.Append($"<td>{item.ProductVariant?.Size ?? 0}</td>");
                sb.Append($"<td>{item.Quantity}</td>");
                sb.Append($"<td>{item.RefundAmount:F2} EGP</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");

            // Total Refund
            sb.Append("<div class='total-section'>");
            sb.Append($"<div class='total-row grand'><span>Total Refund:</span><span>{returnInvoice.TotalRefund:F2} EGP</span></div>");
            sb.Append("</div>");

            // Footer
            sb.Append("<div style='clear:both;'></div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p>Thank you for your business!</p>");
            sb.Append("<p>Kids Store Management System</p>");
            sb.Append("</div>");

            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}

