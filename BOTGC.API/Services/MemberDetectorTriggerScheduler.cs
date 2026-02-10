using System.Collections.Concurrent;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.EventBus.Detectors;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services;

public sealed class MemberDetectorTriggerScheduler : IMemberDetectorTriggerScheduler
{
    private readonly ConcurrentDictionary<DetectorMemberKey, CancellationTokenSource> _pending = new();
    private readonly IQueueService<DetectorTriggerCommand> _triggerQueue;
    private readonly ILogger<MemberDetectorTriggerScheduler> _logger;

    public MemberDetectorTriggerScheduler(
        IQueueService<DetectorTriggerCommand> triggerQueue,
        ILogger<MemberDetectorTriggerScheduler> logger)
    {
        _triggerQueue = triggerQueue ?? throw new ArgumentNullException(nameof(triggerQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public void Schedule(
        string detectorName,
        int memberId,
        string? correlationId,
        TimeSpan quietPeriod,
        CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(detectorName)) throw new ArgumentNullException(nameof(detectorName));
        if (memberId <= 0) throw new ArgumentOutOfRangeException(nameof(memberId));
        if (quietPeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(quietPeriod));

        var key = new DetectorMemberKey(detectorName.Trim(), memberId);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        _pending.AddOrUpdate(
            key,
            addValueFactory: _ => cts,
            updateValueFactory: (_, existing) =>
            {
                try { existing.Cancel(); } catch { }
                existing.Dispose();
                return cts;
            });

        _ = RunAsync(key, correlationId, quietPeriod, cts);
    }

    private async Task RunAsync(
        DetectorMemberKey key,
        string? correlationId,
        TimeSpan quietPeriod,
        CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(quietPeriod, cts.Token);

            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            var cmd = new DetectorTriggerCommand(
                DetectorName: key.DetectorName,
                MemberNumber: key.MemberId,
                CorrelationId: correlationId,
                RequestedAtUtc: DateTimeOffset.UtcNow);

            await _triggerQueue.EnqueueAsync(cmd, cts.Token);

            _logger.LogInformation(
                "Debounced detector trigger enqueued. Detector={Detector} Member={Member} CorrelationId={CorrelationId}.",
                key.DetectorName,
                key.MemberId,
                correlationId);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Debounced detector trigger failed. Detector={Detector} Member={Member} CorrelationId={CorrelationId}.",
                key.DetectorName,
                key.MemberId,
                correlationId);
        }
        finally
        {
            if (_pending.TryGetValue(key, out var current) && ReferenceEquals(current, cts))
            {
                _pending.TryRemove(key, out _);
            }

            cts.Dispose();
        }
    }

    private readonly record struct DetectorMemberKey(string DetectorName, int MemberId);
}