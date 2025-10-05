//using BOTGC.API.Common;
//using BOTGC.API.Dto;
//using BOTGC.API.Interfaces;
//using BOTGC.API.Models;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//namespace BOTGC.API.Services.BackgroundServices
//{
//    public class WasteSheetDailyFlusher(IOptions<AppSettings> settings,
//                                        ILogger<WasteSheetDailyFlusher> logger,
//                                        IServiceScopeFactory scopeFactory) : BackgroundService
//    {
//        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
//        private readonly ILogger<WasteSheetDailyFlusher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await ProcessUnprocessedSheetsAsync(stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "WasteSheetDailyFlusher run failed.");
//                }

//                var delay = GetDelayUntilNextLocalTime(3, 0);
//                await Task.Delay(delay, stoppingToken);
//            }
//        }

//        private static TimeSpan GetDelayUntilNextLocalTime(int hourLocal, int minuteLocal)
//        {
//            var now = DateTimeOffset.Now;
//            var next = new DateTimeOffset(now.Year, now.Month, now.Day, hourLocal, minuteLocal, 0, now.Offset);
//            if (now >= next) next = next.AddDays(1);
//            return next - now;
//        }

//        private async Task ProcessUnprocessedSheetsAsync(CancellationToken cancellationToken)
//        {
//            using var scope = _scopeFactory.CreateScope();
//            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
//            var mapping = scope.ServiceProvider.GetRequiredService<IStockMappingService>();
//            var queue = scope.ServiceProvider.GetRequiredService<IQueueService<WasteEntryCommandDto>>();

//            var lookbackDays = Math.Max(1, _settings.Waste?.LookbackDays ?? 14);
//            var ttl = TimeSpan.FromDays(180);

//            for (var i = 1; i <= lookbackDays; i++)
//            {
//                var date = DateTime.UtcNow.Date.AddDays(-i);
//                var key = $"WasteSheet:{date:yyyyMMdd}";

//                var sheet = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
//                if (sheet is null) continue;
//                if (string.Equals(sheet.Status, "Completed", StringComparison.OrdinalIgnoreCase)) continue;

//                if (sheet.Entries.Count == 0)
//                {
//                    sheet.Status = "Completed";
//                    await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);
//                    continue;
//                }

//                foreach (var e in sheet.Entries.Values)
//                {
//                    var (productNumericId, stockRoomId) = await mapping.ResolveForWastageAsync(e.ProductId, cancellationToken);
//                    var qty = Convert.ToInt32(Math.Round(e.Quantity, MidpointRounding.AwayFromZero));

//                    var dto = new WasteEntryCommandDto(
//                        WastageDateUtc: date,
//                        ProductId: productNumericId,
//                        StockRoomId: stockRoomId,
//                        Quantity: qty,
//                        Reason: e.Reason,
//                        ClientEntryId: e.ClientEntryId,
//                        OperatorId: e.OperatorId,
//                        DeviceId: e.DeviceId,
//                        ProductName: e.ProductName
//                    );

//                    await queue.EnqueueAsync(dto, cancellationToken);
//                }

//                sheet.Status = "Completed";
//                await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);
//                _logger.LogInformation("Flushed {Count} entries from waste sheet {Date} to queue.", sheet.Entries.Count, date.ToString("yyyy-MM-dd"));
//            }
//        }
//    }
//}
