using BoyutAplication.Models;

namespace BoyutAplication.Services
{
    public interface IMockIntegratorService
    {
        MockInvoiceRecord? GetInvoiceStatus(string taxNumber, string invoiceNumber);
    }
}
