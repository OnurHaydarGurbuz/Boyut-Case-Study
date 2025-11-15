using BoyutAplication.Models;
using BoyutAplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoyutAplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceStatusService _invoiceStatusService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(
            IInvoiceStatusService invoiceStatusService,
            ILogger<InvoiceController> logger)
        {
            _invoiceStatusService = invoiceStatusService;
            _logger = logger;
        }

        [HttpPost("check")]
        public async Task<ActionResult<InvoiceCheckResponse>> Check([FromBody] InvoiceCheckRequest request)
        {
            // Her istek için CorrelationId üret
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Incoming request: Invoice={Invoice}, Tax={Tax}",
                correlationId,
                request.InvoiceNumber,
                request.TaxNumber);

            // Header'a CorrelationId ekle
            HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;

            var response = await _invoiceStatusService.CheckInvoiceAsync(request, correlationId);

            return Ok(response);
        }
    }
}
