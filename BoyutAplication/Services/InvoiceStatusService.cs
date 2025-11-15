using BoyutAplication.Data;
using BoyutAplication.Entities;
using BoyutAplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BoyutAplication.Services
{
    public class InvoiceStatusService : IInvoiceStatusService
    {
        private readonly IMockIntegratorService _mockIntegratorService;
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<InvoiceStatusService> _logger;

        public InvoiceStatusService(
            IMockIntegratorService mockIntegratorService,
            AppDbContext dbContext,
            IMemoryCache cache,
            ILogger<InvoiceStatusService> logger)
        {
            _mockIntegratorService = mockIntegratorService;
            _dbContext = dbContext;
            _cache = cache;
            _logger = logger;
        }

        public async Task<InvoiceCheckResponse> CheckInvoiceAsync(
            InvoiceCheckRequest request,
            string correlationId)
        {
            var cacheKey = $"{request.TaxNumber}-{request.InvoiceNumber}";



    // Cache kontrolü
    if (_cache.TryGetValue(cacheKey, out InvoiceCheckResponse? cachedResponse) 
        && cachedResponse is not null)
    {
        _logger.LogInformation(
            "CorrelationId: {CorrelationId} - Cache hit for key {CacheKey}",
            correlationId, cacheKey);

        return cachedResponse;
    }

            // Mock entegratör cevabı
            var mockRecord = _mockIntegratorService.GetInvoiceStatus(request.TaxNumber, request.InvoiceNumber);

            var responseCode = mockRecord?.ResponseCode ?? "REJECTED";
            var responseMessage = mockRecord?.Message ?? "Fatura kaydı bulunamadı";

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Mock response: Invoice={Invoice}, Tax={Tax}, Code={Code}, Message={Message}",
                correlationId,
                request.InvoiceNumber,
                request.TaxNumber,
                responseCode,
                responseMessage);

            // DB’ye log yaz
            var log = new InvoiceStatusLog
            {
                InvoiceNumber = request.InvoiceNumber,
                TaxNumber = request.TaxNumber,
                ResponseCode = responseCode,
                ResponseMessage = responseMessage,
                RequestTime = DateTime.UtcNow
            };

            _dbContext.InvoiceStatusLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Log saved with Id={LogId}",
                correlationId, log.Id);

            // Son 2 kayda bak: aynı VKN + Fatura için üst üste 2 REJECTED mi?
            var lastTwo = await _dbContext.InvoiceStatusLogs
                .Where(x =>
                    x.InvoiceNumber == request.InvoiceNumber &&
                    x.TaxNumber == request.TaxNumber)
                .OrderByDescending(x => x.RequestTime)
                .Take(2)
                .ToListAsync();

            InvoiceCheckResponse finalResponse;

            if (lastTwo.Count == 2 &&
                lastTwo[0].ResponseCode == "REJECTED" &&
                lastTwo[1].ResponseCode == "REJECTED")
            {
                finalResponse = new InvoiceCheckResponse
                {
                    Status = "BLOCKED",
                    Message = "Bu faturaya ait art arda 2 red cevabı alındı. Manuel inceleme gerekiyor."
                };

                _logger.LogWarning(
                    "CorrelationId: {CorrelationId} - BLOCKED triggered for Invoice={Invoice}, Tax={Tax}",
                    correlationId,
                    request.InvoiceNumber,
                    request.TaxNumber);
            }
            else
            {
                finalResponse = new InvoiceCheckResponse
                {
                    Status = responseCode,
                    Message = responseMessage
                };
            }

            // Cevabı 1 dk cache’le
            _cache.Set(cacheKey, finalResponse, TimeSpan.FromMinutes(1));

            return finalResponse;
        }
    }
}
