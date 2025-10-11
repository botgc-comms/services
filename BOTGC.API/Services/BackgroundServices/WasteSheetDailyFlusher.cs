using Cronos;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;

namespace BOTGC.API.Services.BackgroundServices;

public sealed partial class WasteSheetDailyFlusher : BackgroundService
{
    private readonly AppSettings _settings;
    private readonly ILogger<WasteSheetDailyFlusher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLockManager _lockManager;

    private readonly CronExpression _cron;
    private readonly TimeZoneInfo _tz;
    private readonly bool _runOnStartup;

    public WasteSheetDailyFlusher(
        IOptions<AppSettings> settings,
        ILogger<WasteSheetDailyFlusher> logger,
        IServiceScopeFactory scopeFactory,
        IDistributedLockManager lockManager)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

        var cronText = _settings.Waste?.Cron ?? "0 3 * * *";
        _cron = CronExpression.Parse(cronText, CronFormat.Standard);

        var tzId = _settings.Waste?.TimeZone ?? "Europe/London";
        _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        _runOnStartup = _settings.Waste?.RunOnStartup ?? true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_runOnStartup)
        {
            try { await ProcessUnprocessedSheetsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogError(ex, "Initial WasteSheet flush failed."); }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var next = _cron.GetNextOccurrence(nowUtc, _tz);
                if (next is null)
                {
                    _logger.LogWarning("Cron '{Cron}' produced no next occurrence; defaulting to 1 minute.", _settings.Waste?.Cron);
                    delay = TimeSpan.FromMinutes(1);
                }
                else
                {
                    delay = next.Value - nowUtc;
                    if (delay <= TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute next cron occurrence; defaulting to 1 minute.");
                delay = TimeSpan.FromMinutes(1);
            }

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }

            try { await ProcessUnprocessedSheetsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Scheduled WasteSheet flush failed."); }
        }
    }

    private static string CacheKeyFor(DateTime d) => $"WasteSheet:{d:yyyyMMdd}";

    private async Task ProcessUnprocessedSheetsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var queue = scope.ServiceProvider.GetRequiredService<IQueueService<WasteEntryCommandDto>>();

        var lookbackDays = Math.Clamp(_settings.Waste?.DaysToLookBack ?? 10, 5, 10);
        var ttl = TimeSpan.FromDays(180);

        for (var i = 1; i <= lookbackDays; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var date = DateTime.UtcNow.Date.AddDays(-i);
            var key = CacheKeyFor(date);
            var lockKey = $"locks:{key}";

            await using var theLock = await _lockManager.AcquireLockAsync(
                lockKey,
                expiry: null,
                waitTime: TimeSpan.FromSeconds(5),
                retryTime: TimeSpan.FromMilliseconds(250),
                cancellationToken: cancellationToken);

            if (!theLock.IsAcquired) continue;

            var sheet = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
            if (sheet is null) continue;

            if (string.Equals(sheet.Status, "Completed", StringComparison.OrdinalIgnoreCase)) continue;

            if (sheet.Entries.Count == 0)
            {
                sheet.Status = "Completed";
                await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);
                continue;
            }

            var count = 0;

            foreach (var e in sheet.Entries.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var qty = Convert.ToInt32(Math.Round(e.Quantity, MidpointRounding.AwayFromZero));

                var dto = new WasteEntryCommandDto(
                    WastageDateUtc: date,
                    ProductId: e.IGProductId,
                    StockRoomId: null,
                    Quantity: qty,
                    Reason: e.Reason,
                    ClientEntryId: e.ClientEntryId,
                    OperatorId: e.OperatorId,
                    ProductName: e.ProductName
                );

                await queue.EnqueueAsync(dto, cancellationToken).ConfigureAwait(false);
                count++;
            }

            sheet.Status = "Completed";
            await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

            _logger.LogInformation("Flushed {Count} entries from {Key} and marked Completed.", count, key);
        }
    }
}
