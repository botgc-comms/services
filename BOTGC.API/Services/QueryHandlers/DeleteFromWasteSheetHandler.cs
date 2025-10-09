using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public partial class GetWasteSheetHandler
{
    public class DeleteFromWasteSheetHandler(IOptions<AppSettings> settings,
                                             ILogger<DeleteFromWasteSheetHandler> logger,
                                             IServiceScopeFactory serviceScopeFactory)
    : QueryHandlerBase<DeleteFromWasteSheetCommand, DeleteResultDto>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<DeleteFromWasteSheetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        public async override Task<DeleteResultDto> Handle(DeleteFromWasteSheetCommand request, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var key = $"WasteSheet:{request.Date:yyyyMMdd}";
            var ttl = TimeSpan.FromDays(180);

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var sheet = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
                if (sheet == null || !sheet.Entries.ContainsKey(request.EntryId))
                {
                    return new DeleteResultDto(request.EntryId, false);
                }

                sheet.Entries.Remove(request.EntryId);
                await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                var check = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
                if (check != null && !check.Entries.ContainsKey(request.EntryId))
                {
                    _logger.LogInformation("Deleted waste entry {EntryId} from {Date}.", request.EntryId, request.Date);
                    return new DeleteResultDto(request.EntryId, true);
                }
            }

            _logger.LogWarning("After retries, entry {EntryId} may still exist for {Date}.", request.EntryId, request.Date);
            return new DeleteResultDto(request.EntryId, false);
        }
    }

}