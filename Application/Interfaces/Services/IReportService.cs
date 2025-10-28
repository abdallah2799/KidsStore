namespace Application.Interfaces.Services
{
    public interface IReportService
    {
        /// <summary>
        /// Generates a PDF report from the provided data
        /// </summary>
        /// <param name="reportData">The data object containing report information</param>
        /// <param name="templateName">The template name to use for report generation</param>
        /// <returns>Byte array of the generated PDF</returns>
        Task<byte[]> GeneratePdfAsync(object reportData, string templateName);

        /// <summary>
        /// Generates an Excel report from the provided data
        /// </summary>
        /// <param name="reportData">The data object containing report information</param>
        /// <param name="sheetName">The name of the Excel sheet</param>
        /// <returns>Byte array of the generated Excel file</returns>
        Task<byte[]> GenerateExcelAsync(object reportData, string sheetName);

        /// <summary>
        /// Generates a sales invoice receipt PDF
        /// </summary>
        /// <param name="invoiceId">The ID of the sales invoice</param>
        /// <returns>Byte array of the generated PDF receipt</returns>
        Task<byte[]> GenerateSalesReceiptAsync(int invoiceId);

        /// <summary>
        /// Generates a sales invoice receipt HTML (synchronous)
        /// </summary>
        /// <param name="invoiceId">The ID of the sales invoice</param>
        /// <returns>HTML string of the receipt</returns>
        string GenerateSalesReceiptHtml(int invoiceId);

        /// <summary>
        /// Generates a purchase invoice PDF
        /// </summary>
        /// <param name="invoiceId">The ID of the purchase invoice</param>
        /// <returns>Byte array of the generated PDF</returns>
        Task<byte[]> GeneratePurchaseInvoiceAsync(int invoiceId);

        /// <summary>
        /// Generates a return invoice receipt HTML
        /// </summary>
        /// <param name="returnInvoice">The return invoice entity</param>
        /// <returns>HTML string of the return receipt</returns>
        string GenerateReturnReceipt(Domain.Entities.ReturnInvoice returnInvoice);
    }
}
