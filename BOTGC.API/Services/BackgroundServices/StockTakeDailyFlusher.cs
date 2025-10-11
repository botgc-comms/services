using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Dto;
using MediatR;
using BOTGC.API.Services.Queries;
using Cronos;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices;

public sealed class StockTakeDailyFlusher : BackgroundService
{
    private readonly AppSettings _settings;
    private readonly ILogger<StockTakeDailyFlusher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLockManager _lockManager;

    private readonly CronExpression _cron;
    private readonly TimeZoneInfo _tz;
    private readonly bool _runOnStartup;

    public StockTakeDailyFlusher(
        IOptions<AppSettings> settings,
        ILogger<StockTakeDailyFlusher> logger,
        IServiceScopeFactory scopeFactory,
        IDistributedLockManager lockManager)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

        // Fallbacks mirror your WasteSheetDailyFlusher approach
        var cronText = _settings.StockTake?.Cron ?? "0 3 * * *"; // default 03:00 local
        _cron = CronExpression.Parse(cronText, CronFormat.Standard);

        var tzId = _settings.StockTake?.TimeZone ?? "Europe/London";
        _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        _runOnStartup = _settings.StockTake?.RunOnStartup ?? true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_runOnStartup)
        {
            try { await FlushPendingAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { _logger.LogError(ex, "Initial StockTake flush failed."); }
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
                    _logger.LogWarning("Cron '{Cron}' produced no next occurrence; defaulting to 1 minute.", _settings.StockTake?.Cron);
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

            try { await FlushPendingAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Scheduled StockTake flush failed."); }
        }
    }

    private static string CacheKeyFor(DateTime d, string division)
        => $"StockTakeSheet:{d:yyyyMMdd}:{division?.Trim() ?? string.Empty}";

    private async Task FlushPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var queue = scope.ServiceProvider.GetRequiredService<IQueueService<StockTakeSheetProcessCommandDto>>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Decide which divisions to scan. Here we ask for the current product plan and
        // take distinct divisions. If you have a dedicated query, replace with that.
        var divisions = await GetDivisionsAsync(mediator, cancellationToken);
        if (divisions.Count == 0)
        {
            _logger.LogInformation("No divisions returned; skipping stock-take flush pass.");
            return;
        }

        var lookbackDays = Math.Clamp(_settings.StockTake?.DaysToLookBack ?? 10, 1, 10);
        var ttl = TimeSpan.FromDays(180);

        for (var i = 1; i <= lookbackDays; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var date = DateTime.UtcNow.Date.AddDays(-i);

            foreach (var division in divisions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = CacheKeyFor(date, division);
                var lockKey = $"locks:{key}";

                await using var theLock = await _lockManager.AcquireLockAsync(
                    lockKey,
                    expiry: null,
                    waitTime: TimeSpan.FromSeconds(5),
                    retryTime: TimeSpan.FromMilliseconds(250),
                    cancellationToken: cancellationToken);

                if (!theLock.IsAcquired) continue;

                var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);
                if (sheet is null) continue;

                if (string.Equals(sheet.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Construct the DTO we’ll put on the processor queue
                var dto = new StockTakeSheetDto(
                    Date: date,
                    Division: division,
                    Status: sheet.Status ?? "Open",
                    Entries: sheet.Entries.Values.ToList() // StockTakeEntryDto list
                );

                var message = new StockTakeSheetProcessCommandDto(
                    Sheet: dto,
                    CorrelationId: Guid.NewGuid().ToString("N"),
                    EnqueuedAtUtc: DateTime.UtcNow
                );

                await queue.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

                // Mark this sheet Completed after enqueue (idempotency is fine: lock + status)
                sheet.Status = "Completed";
                sheet.Date = date;
                sheet.Division = division;
                await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                _logger.LogInformation(
                    "Enqueued stock-take sheet {Date}/{Division} with {Count} items and marked Completed.",
                    date.ToString("yyyy-MM-dd"), division, dto.Entries.Count);
            }
        }
    }

    private static async Task<List<string>> GetDivisionsAsync(IMediator mediator, CancellationToken ct)
    {
        try
        {
            // Reuse your existing prepared-product list to infer divisions
            var products = await mediator.Send(new GetStockTakeProductsQuery(), ct);
            if (products is null || products.Count == 0) return new List<string>();

            // products: List<DivisionPlanDto> (Division + Products)
            return products
                .Select(p => (p?.Division ?? string.Empty).Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            // Fallback to a single empty division to avoid crashing the flusher
            return new List<string>();
        }
    }
}

