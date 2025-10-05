using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public partial class GetWasteSheetHandler
{
    public class AddToWasteSheetHandler(IOptions<AppSettings> settings,
                                        ILogger<AddToWasteSheetHandler> logger,
                                        IServiceScopeFactory serviceScopeFactory)
        : QueryHandlerBase<AddToWasteSheetCommand, AddResultDto>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<AddToWasteSheetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        public async override Task<AddResultDto> Handle(AddToWasteSheetCommand request, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var CacheKey = (DateTime d) => $"WasteSheet:{d:yyyyMMdd}";

            var date = request.Date.Date;
            var key = CacheKey(date);
            var ttl = TimeSpan.FromDays(180);

            // Light retry loop to reduce overwrite races in multi-device scenarios
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var sheet = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false)
                            ?? new WasteSheetCacheModel();

                if (sheet.Entries.ContainsKey(request.ClientEntryId))
                {
                    _logger.LogInformation("Duplicate waste entry {ClientEntryId} ignored for {Date}.", request.ClientEntryId, date.ToString("yyyy-MM-dd"));
                    return new AddResultDto(request.ClientEntryId, true);
                }

                var now = DateTimeOffset.UtcNow;

                var entry = new WasteEntryDto(
                    request.ClientEntryId,
                    now,
                    request.OperatorId,
                    request.ProductId, 
                    request.IGProductId, 
                    request.Unit, 
                    request.ProductName,
                    request.Reason,
                    request.Quantity,
                    request.DeviceId
                );

                sheet.Status = "Open";
                sheet.Entries[request.ClientEntryId] = entry;

                await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                // Best-effort: read-back to ensure our entry persisted
                var check = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
                if (check != null && check.Entries.ContainsKey(request.ClientEntryId))
                {
                    _logger.LogInformation("Added waste entry {ClientEntryId} for {Date}: {ProductName} x{Qty}.",
                        request.ClientEntryId, date.ToString("yyyy-MM-dd"), request.ProductName, request.Quantity);

                    return new AddResultDto(request.ClientEntryId, false);
                }
            }

            _logger.LogWarning("After retries, entry {ClientEntryId} may not have persisted for {Date}. Treating as idempotent success.",
                request.ClientEntryId, date.ToString("yyyy-MM-dd"));
            return new AddResultDto(request.ClientEntryId, false);
        }
    }

}