using System;

namespace BoyutAplication.Entities
{
    public class InvoiceStatusLog
    {
        public int Id { get; set; }  // Primary key

        public string InvoiceNumber { get; set; } = string.Empty;

        public string TaxNumber { get; set; } = string.Empty;

        public string ResponseCode { get; set; } = string.Empty;

        public string ResponseMessage { get; set; } = string.Empty;

        public DateTime RequestTime { get; set; }
    }
}
