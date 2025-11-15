using BoyutAplication.Models;

namespace BoyutAplication.Services
{
    public interface IInvoiceStatusService
    {
        Task<InvoiceCheckResponse> CheckInvoiceAsync(InvoiceCheckRequest request, string correlationId);
    }
}
