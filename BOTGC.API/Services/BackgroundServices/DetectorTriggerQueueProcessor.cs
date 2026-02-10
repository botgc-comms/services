using System.Reflection;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.BackgroundServices;

public sealed class DetectorTriggerQueueProcessor : BackgroundService
{
    private readonly ILogger<DetectorTriggerQueueProcessor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IQueueService<DetectorTriggerCommand> _triggerQueue;

    public DetectorTriggerQueueProcessor(
        ILogger<DetectorTriggerQueueProcessor> logger,
        IServiceScopeFactory scopeFactory,
        IQueueService<DetectorTriggerCommand> triggerQueue)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _triggerQueue = triggerQueue ?? throw new ArgumentNullException(nameof(triggerQueue));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 5;
        Exception? lastError = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _triggerQueue.ReceiveMessagesAsync(5, cancellationToken: stoppingToken);

            foreach (var msg in messages)
            {
                var payload = msg.Payload;

                if (payload is null)
                {
                    _logger.LogWarning("DetectorTrigger: failed to deserialise message {MessageId}.", msg.Message.MessageId);
                    await _triggerQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                    continue;
                }

                try
                {
                    if (msg.Message.DequeueCount > maxAttempts)
                    {
                        _logger.LogWarning(
                            "DetectorTrigger: max attempts exceeded. Dead-lettering. Detector={Detector} Member={Member} CorrelationId={CorrelationId}.",
                            payload.DetectorName,
                            payload.MemberNumber,
                            payload.CorrelationId);

                        await _triggerQueue.DeadLetterEnqueueAsync(payload, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                        await _triggerQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var detectors = scope.ServiceProvider.GetServices<IDetector>();

                        var matches = detectors
                            .Select(d => new { Detector = d, Name = DetectorName.For(d.GetType()) })
                            .Where(x => string.Equals(x.Name, payload.DetectorName, StringComparison.Ordinal))
                            .Select(x => x.Detector)
                            .ToList();

                        if (matches.Count == 0)
                        {
                            throw new InvalidOperationException($"Unknown detector '{payload.DetectorName}'.");
                        }

                        if (matches.Count > 1)
                        {
                            throw new InvalidOperationException($"Multiple detectors registered with name '{payload.DetectorName}'.");
                        }

                        var detector = matches[0];

                        var mi = detector.GetType().GetMethod(
                            "RunForMemberAsync",
                            BindingFlags.Instance | BindingFlags.Public,
                            binder: null,
                            types: new[] { typeof(int), typeof(CancellationToken) },
                            modifiers: null);

                        if (mi is null)
                        {
                            throw new InvalidOperationException($"Detector '{payload.DetectorName}' does not support RunForMemberAsync.");
                        }

                        _logger.LogInformation(
                            "DetectorTrigger: running Detector={Detector} Member={Member} CorrelationId={CorrelationId}.",
                            payload.DetectorName,
                            payload.MemberNumber,
                            payload.CorrelationId);

                        var task = (Task)mi.Invoke(detector, new object[] { payload.MemberNumber, stoppingToken })!;
                        await task;

                        await _triggerQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);

                        _logger.LogInformation(
                            "DetectorTrigger: completed Detector={Detector} Member={Member} CorrelationId={CorrelationId}.",
                            payload.DetectorName,
                            payload.MemberNumber,
                            payload.CorrelationId);
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(
                            ex,
                            "DetectorTrigger: error running Detector={Detector} Member={Member} Attempt={Attempt} CorrelationId={CorrelationId}.",
                            payload.DetectorName,
                            payload.MemberNumber,
                            msg.Message.DequeueCount,
                            payload.CorrelationId);

                        var delaySeconds = Math.Pow(2, Math.Min(msg.Message.DequeueCount, 6));
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogError(ex, "DetectorTrigger: unexpected error handling queue message {MessageId}.", msg.Message.MessageId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}