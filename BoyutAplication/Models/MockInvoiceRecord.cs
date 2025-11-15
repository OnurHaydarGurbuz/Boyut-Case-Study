namespace BoyutAplication.Models
{
    public class MockInvoiceRecord
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty; // APPROVED / REJECTED
        public string Message { get; set; } = string.Empty;
    }
}
