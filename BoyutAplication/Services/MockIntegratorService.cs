using BoyutAplication.Models;

namespace BoyutAplication.Services
{
    public class MockIntegratorService : IMockIntegratorService
    {
        // In-memory mock liste (veritabanı simülasyonu) 
        private static readonly List<MockInvoiceRecord> _records = new()
        {
            new MockInvoiceRecord
            {
                InvoiceNumber = "FAT20251411001",
                TaxNumber = "1234567890",
                ResponseCode = "APPROVED",
                Message = "Fatura onaylandı"
            },
            new MockInvoiceRecord
            {
                InvoiceNumber = "FAT20251411002",
                TaxNumber = "1234567890",
                ResponseCode = "REJECTED",
                Message = "Hatalı imza"
            }
        };

        public MockInvoiceRecord? GetInvoiceStatus(string taxNumber, string invoiceNumber)
        {
            // Request’e uygun ilk kaydı döndür
            return _records.FirstOrDefault(x =>
                x.TaxNumber == taxNumber &&
                x.InvoiceNumber == invoiceNumber);
        }
    }
}
