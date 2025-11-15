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

        private const string CODE_REJECTED = "REJECTED";
        private const string CODE_BLOCKED = "BLOCKED";

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
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Incoming request Invoice={Invoice}, Tax={Tax}",
                correlationId, request.InvoiceNumber, request.TaxNumber);

            // CAche check
            var alreadyBlocked = await _dbContext.InvoiceStatusLogs
                .AnyAsync(x =>
                    x.InvoiceNumber == request.InvoiceNumber &&
                    x.TaxNumber == request.TaxNumber &&
                    x.ResponseCode == CODE_BLOCKED);

            if (alreadyBlocked)
            {
                var blockedResponse = new InvoiceCheckResponse
                {
                    Status = CODE_BLOCKED,
                    Message = "Bu faturaya ait art arda 2 red cevabı alındı. Manuel inceleme gerekiyor."
                };

                var blockedLog = new InvoiceStatusLog
                {
                    InvoiceNumber = request.InvoiceNumber,
                    TaxNumber = request.TaxNumber,
                    ResponseCode = CODE_BLOCKED,
                    ResponseMessage = blockedResponse.Message,
                    RequestTime = now
                };

                _dbContext.InvoiceStatusLogs.Add(blockedLog);
                await _dbContext.SaveChangesAsync();


                _cache.Set(cacheKey, blockedResponse, TimeSpan.FromMinutes(1));

                _logger.LogWarning(
                    "CorrelationId: {CorrelationId} - PERMANENT BLOCK active for Invoice={Invoice}, Tax={Tax}",
                    correlationId, request.InvoiceNumber, request.TaxNumber);

                return blockedResponse;
            }

            // Henüz block yok → önce cache’e bak
            InvoiceCheckResponse finalResponse;

            if (_cache.TryGetValue(cacheKey, out InvoiceCheckResponse? cachedResponse))
            {
                _logger.LogInformation(
                    "CorrelationId: {CorrelationId} - Cache HIT for {CacheKey}",
                    correlationId, cacheKey);

                finalResponse = cachedResponse!;
            }
            else
            {
                _logger.LogInformation(
                    "CorrelationId: {CorrelationId} - Cache MISS for {CacheKey}",
                    correlationId, cacheKey);

                // Mock entegratör cevabı
                var mockRecord = _mockIntegratorService.GetInvoiceStatus(
                    request.TaxNumber,
                    request.InvoiceNumber);

                var responseCode = mockRecord?.ResponseCode ?? CODE_REJECTED;
                var responseMessage = mockRecord?.Message ?? "Fatura kaydı bulunamadı";

                _logger.LogInformation(
                    "CorrelationId: {CorrelationId} - Mock response: Invoice={Invoice}, Tax={Tax}, Code={Code}, Message={Message}",
                    correlationId,
                    request.InvoiceNumber,
                    request.TaxNumber,
                    responseCode,
                    responseMessage);

                finalResponse = new InvoiceCheckResponse
                {
                    Status = responseCode,
                    Message = responseMessage
                };

                // Cevabı cache’e koy (1 dakika)
                _cache.Set(cacheKey, finalResponse, TimeSpan.FromMinutes(1));
            }

            // Mock veya cache’ten gelen cevabı LOG tablosuna yaz
            var log = new InvoiceStatusLog
            {
                InvoiceNumber = request.InvoiceNumber,
                TaxNumber = request.TaxNumber,
                ResponseCode = finalResponse.Status,
                ResponseMessage = finalResponse.Message,
                RequestTime = now
            };

            _dbContext.InvoiceStatusLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "CorrelationId: {CorrelationId} - Log saved with Id={LogId}",
                correlationId, log.Id);

            // Eğer bu cevap REJECTED ise → son 1 dakikadaki reject sayısına bak
            if (finalResponse.Status == CODE_REJECTED)
            {
                var rejectedCountInWindow = await _dbContext.InvoiceStatusLogs
                    .Where(x =>
                        x.InvoiceNumber == request.InvoiceNumber &&
                        x.TaxNumber == request.TaxNumber &&
                        x.ResponseCode == CODE_REJECTED &&
                        x.RequestTime >= oneMinuteAgo)
                    .CountAsync();

                // Eğer bu 1 dakikalık pencerede 2+ REJECTED olduysa → kalıcı BLOCK işaretle
                if (rejectedCountInWindow >= 2)
                {
                    var blockedMarkLog = new InvoiceStatusLog
                    {
                        InvoiceNumber = request.InvoiceNumber,
                        TaxNumber = request.TaxNumber,
                        ResponseCode = CODE_BLOCKED,
                        ResponseMessage = "Bu faturaya ait art arda 2 red cevabı alındı. Manuel inceleme gerekiyor.",
                        RequestTime = now
                    };

                    _dbContext.InvoiceStatusLogs.Add(blockedMarkLog);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogWarning(
                        "CorrelationId: {CorrelationId} - PERMANENT BLOCK set for Invoice={Invoice}, Tax={Tax} (2 REJECTED in 1 min)",
                        correlationId, request.InvoiceNumber, request.TaxNumber);

                    // 2. gene rejected dönecek
                }
            }

            return finalResponse;
        }
    }
}
