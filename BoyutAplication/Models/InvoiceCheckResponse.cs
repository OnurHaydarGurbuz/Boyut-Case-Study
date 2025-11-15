namespace BoyutAplication.Models
{
    public class InvoiceCheckResponse
    {
        public string Status { get; set; } = string.Empty;  // APPROVED, REJECTED, BLOCKED
        public string Message { get; set; } = string.Empty;
    }
}
