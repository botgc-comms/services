using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using Cronos;
using MediatR;
using Microsoft.Extensions.Options;


namespace BOTGC.API.Services.Events;

public sealed class EventPublisher(
    IEventStore eventStore,
    IQueueService<EventEnvelope> mainQueue,
    JsonSerializerOptions json,
    ILogger<EventPublisher> logger) : IEventPublisher
{
    private readonly IEventStore _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    private readonly IQueueService<EventEnvelope> _mainQueue = mainQueue ?? throw new ArgumentNullException(nameof(mainQueue));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));
    private readonly ILogger<EventPublisher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : IEvent
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        var clrType = EventTypeKey.For(evt.GetType());

        var payloadJson = JsonSerializer.Serialize(evt, evt.GetType(), _json);

        var envelope = new EventEnvelope
        {
            EventId = evt.EventId,
            MemberId = evt.MemberId,
            OccurredAtUtc = evt.OccurredAtUtc,
            EventClrType = clrType,
            PayloadJson = payloadJson
        };

        await _eventStore.AppendAsync(envelope, cancellationToken);
        await _mainQueue.EnqueueAsync(envelope, cancellationToken);

        _logger.LogInformation("Published event {EventId} ({EventClrType}).", evt.EventId, clrType);
    }
}

public sealed class EventDispatcher(
    IQueueService<EventEnvelope> mainQueue,
    ISubscriberCatalogue catalogue,
    ISubscriberQueueFactory subscriberQueues,
    IEventTypeRegistry eventTypes,
    IOptions<EventPipelineOptions> options,
    ILogger<EventDispatcher> logger) : BackgroundService
{
    private readonly IQueueService<EventEnvelope> _mainQueue = mainQueue ?? throw new ArgumentNullException(nameof(mainQueue));
    private readonly ISubscriberCatalogue _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
    private readonly ISubscriberQueueFactory _subscriberQueues = subscriberQueues ?? throw new ArgumentNullException(nameof(subscriberQueues));
    private readonly IEventTypeRegistry _eventTypes = eventTypes ?? throw new ArgumentNullException(nameof(eventTypes));
    private readonly EventPipelineOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<EventDispatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Exception? lastError = default;

        while (!cancellationToken.IsCancellationRequested)
        {
            var messages = await _mainQueue.ReceiveMessagesAsync(_options.DispatcherBatchSize, cancellationToken: cancellationToken);

            foreach (var message in messages)
            {
                var envelope = message.Payload;
                var msg = message.Message;

                if (envelope is null)
                {
                    _logger.LogWarning("Failed to deserialise event envelope for message {MessageId}.", msg.MessageId);
                    await _mainQueue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                    continue;
                }

                try
                {
                    if (msg.DequeueCount > _options.MaxAttempts)
                    {
                        await _mainQueue.DeadLetterEnqueueAsync(envelope, msg.DequeueCount, DateTime.UtcNow, lastError, cancellationToken);
                        await _mainQueue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                        continue;
                    }

                    var eventClrType = _eventTypes.Resolve(envelope.EventClrType);
                    if (eventClrType is null)
                    {
                        lastError = new InvalidOperationException($"Unknown event type key '{envelope.EventClrType}'.");
                        await _mainQueue.DeadLetterEnqueueAsync(envelope, msg.DequeueCount, DateTime.UtcNow, lastError, cancellationToken);
                        await _mainQueue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                        continue;
                    }

                    var subscriberTypes = _catalogue.GetSubscribersFor(eventClrType);

                    foreach (var subscriberType in subscriberTypes)
                    {
                        var q = _subscriberQueues.Create<EventEnvelope>(subscriberType);
                        await q.EnqueueAsync(envelope, cancellationToken);
                    }

                    await _mainQueue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogError(ex, "Dispatcher failed for event {EventId}.", envelope.EventId);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, msg.DequeueCount)), cancellationToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.DispatcherPollDelaySeconds), cancellationToken);
        }
    }
}


public sealed class SubscriberCatalogue : ISubscriberCatalogue
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>> _map;

    public SubscriberCatalogue(IEnumerable<Type> subscriberTypes)
    {
        var dict = new Dictionary<Type, List<Type>>();

        foreach (var subscriberType in subscriberTypes)
        {
            foreach (var i in subscriberType.GetInterfaces())
            {
                if (!i.IsGenericType)
                {
                    continue;
                }

                if (i.GetGenericTypeDefinition() != typeof(ISubscriber<>))
                {
                    continue;
                }

                var eventClrType = i.GetGenericArguments()[0];
                if (!dict.TryGetValue(eventClrType, out var list))
                {
                    list = new List<Type>();
                    dict[eventClrType] = list;
                }

                list.Add(subscriberType);
            }
        }

        _map = dict.ToDictionary(k => k.Key, v => (IReadOnlyList<Type>)v.Value);
    }

    public IReadOnlyList<Type> GetSubscribersFor(Type eventClrType)
    {
        if (_map.TryGetValue(eventClrType, out var list))
        {
            return list;
        }

        return Array.Empty<Type>();
    }
}

public sealed class TableDetectorStateStore(
    ITableStore<DetectorStateEntity> table,
    JsonSerializerOptions json) : IDetectorStateStore
{
    private readonly ITableStore<DetectorStateEntity> _table = table ?? throw new ArgumentNullException(nameof(table));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    public async Task<DetectorState<TState>> GetAsync<TState>(string detectorName, string scopeKey, CancellationToken cancellationToken)
        where TState : class, new()
    {
        var entity = await _table.GetAsync(detectorName, scopeKey, cancellationToken);

        if (entity is null)
        {
            return new DetectorState<TState>();
        }

        var state = new DetectorState<TState>
        {
            UpdatedUtc = entity.UpdatedUtc
        };

        if (!string.IsNullOrWhiteSpace(entity.StateJson))
        {
            state.Value = JsonSerializer.Deserialize<TState>(entity.StateJson, _json) ?? new TState();
        }

        return state;
    }

    public async Task UpsertAsync<TState>(string detectorName, string scopeKey, DetectorState<TState> state, CancellationToken cancellationToken)
        where TState : class, new()
    {
        var entity = new DetectorStateEntity
        {
            PartitionKey = detectorName,
            RowKey = scopeKey,
            UpdatedUtc = state.UpdatedUtc,
            StateJson = JsonSerializer.Serialize(state.Value, _json)
        };

        await _table.UpsertAsync(entity, cancellationToken);
    }
}

public static class EventTypeKey
{
    public static string For<TEvent>() where TEvent : IEvent
    {
        return For(typeof(TEvent));
    }

    public static string For(Type eventType)
    {
        var attr = eventType.GetCustomAttribute<EventTypeAttribute>(inherit: false);
        if (attr is not null)
        {
            return $"{attr.Name}:v{attr.Version}";
        }

        var fullName = eventType.FullName ?? throw new InvalidOperationException("Event type must have a full name.");
        var asmName = eventType.Assembly.GetName().Name ?? throw new InvalidOperationException("Event type must have an assembly name.");
        return $"{fullName}, {asmName}";
    }
}


public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _map;

    public EventTypeRegistry(params Assembly[] assemblies)
    {
        var dict = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var asm in assemblies.Distinct())
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                {
                    continue;
                }

                if (!typeof(IEvent).IsAssignableFrom(t))
                {
                    continue;
                }

                var key = EventTypeKey.For(t);

                if (dict.TryGetValue(key, out var existing) && existing != t)
                {
                    throw new InvalidOperationException($"Duplicate event type key '{key}' for '{existing.FullName}' and '{t.FullName}'.");
                }

                dict[key] = t;
            }
        }

        _map = dict;
    }

    public Type? Resolve(string eventTypeKey)
    {
        if (string.IsNullOrWhiteSpace(eventTypeKey))
        {
            return null;
        }

        return _map.TryGetValue(eventTypeKey, out var t) ? t : null;
    }
}


public sealed class TableEventStore(ITableStore<EventStreamEntity> table) : IEventStore
{
    private readonly ITableStore<EventStreamEntity> _table = table ?? throw new ArgumentNullException(nameof(table));

    public async Task AppendAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var invertedTicks = DateTimeOffset.MaxValue.UtcTicks - envelope.OccurredAtUtc.UtcTicks;

        var entity = new EventStreamEntity
        {
            PartitionKey = envelope.MemberId.ToString(),
            RowKey = $"{invertedTicks:D19}_{envelope.EventId:N}",

            EventId = envelope.EventId,
            MemberId = envelope.MemberId,
            OccurredAtUtc = envelope.OccurredAtUtc,
            EventClrType = envelope.EventClrType,
            PayloadJson = envelope.PayloadJson
        };

        await _table.UpsertAsync(entity, cancellationToken);
    }

    public async Task<bool> ExistsAsync<TEvent>(int memberId, CancellationToken cancellationToken) where TEvent : IEvent
    {
        var eventTypeKey = EventTypeKey.For<TEvent>();
        var partitionKey = memberId.ToString();

        var results = await _table.QueryAsync(
            predicate: e => e.PartitionKey == partitionKey && e.EventClrType == eventTypeKey,
            take: 1,
            cancellationToken: cancellationToken);

        return results.Count > 0;
    }
}

public sealed class EventDispatchQueueService : BaseQueueService<EventEnvelope>
{
    public EventDispatchQueueService(
        QueueServiceClient queueServiceClient,
        IOptions<EventPipelineOptions> options,
        ILogger<EventDispatchQueueService> logger)
        : base(
            queueClient: queueServiceClient.GetQueueClient(QueueNames.DispatchQueue(options?.Value.DispatchQueueName ?? throw new ArgumentNullException(nameof(options)))),
            deadLetterQueueClient: queueServiceClient.GetQueueClient(QueueNames.DispatchDeadLetter(options!.Value.DispatchQueueName)),
            logger: logger)
    {
    }
}


public sealed class SubscriberQueueFactory(
    QueueServiceClient queueServiceClient,
    ILoggerFactory loggerFactory) : ISubscriberQueueFactory
{
    private readonly QueueServiceClient _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IQueueService<TPayload> Create<TPayload>(Type subscriberClrType)
    {
        if (subscriberClrType is null)
        {
            throw new ArgumentNullException(nameof(subscriberClrType));
        }

        var logger = _loggerFactory.CreateLogger<SubscriberQueueService<TPayload>>();

        return new SubscriberQueueService<TPayload>(
            _queueServiceClient,
            subscriberClrType,
            logger);
    }
}

public sealed class SubscriberQueueService<TPayload> : BaseQueueService<TPayload>
{
    public SubscriberQueueService(
        QueueServiceClient queueServiceClient,
        Type subscriberClrType,
        ILogger<SubscriberQueueService<TPayload>> logger)
        : base(
            queueClient: queueServiceClient.GetQueueClient(QueueNames.SubscriberQueue(subscriberClrType)),
            deadLetterQueueClient: queueServiceClient.GetQueueClient(QueueNames.SubscriberDeadLetter(subscriberClrType)),
            logger: logger)
    {
    }
}

public abstract class SubscriberBase<TEvent, TSubscriber>(
    ILogger logger,
    ISubscriberQueueFactory queueFactory,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json) : BackgroundService, ISubscriber<TEvent>
    where TEvent : class, IEvent
    where TSubscriber : class, ISubscriber<TEvent>
{
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly EventPipelineOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    private readonly IQueueService<EventEnvelope> _queue =
        queueFactory?.Create<EventEnvelope>(typeof(TSubscriber)) ?? throw new ArgumentNullException(nameof(queueFactory));

    public string Name => typeof(TSubscriber).Name;

    public Task HandleAsync(TEvent evt, CancellationToken cancellationToken)
    {
        return ProcessAsync(evt, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Exception? lastError = default;

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _queue.ReceiveMessagesAsync(
                maxMessages: _options.SubscriberBatchSize,
                visibilityTimeout: null,
                cancellationToken: stoppingToken);

            foreach (var msg in messages)
            {
                var env = msg.Payload;

                if (env is null)
                {
                    _logger.LogWarning("Failed to deserialise subscriber envelope for message {MessageId} ({Subscriber}).", msg.Message.MessageId, Name);
                    await _queue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                    continue;
                }

                try
                {
                    if (msg.Message.DequeueCount > _options.MaxAttempts)
                    {
                        await _queue.DeadLetterEnqueueAsync(env, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                        await _queue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    var evt = JsonSerializer.Deserialize<TEvent>(env.PayloadJson, _json);
                    if (evt is null)
                    {
                        lastError = new InvalidOperationException($"Failed to deserialise {typeof(TEvent).FullName} from envelope payload.");
                        await _queue.DeadLetterEnqueueAsync(env, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                        await _queue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    await ProcessAsync(evt, stoppingToken);
                    await _queue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogError(ex, "Error in subscriber {Subscriber} for event {EventId}.", Name, env.EventId);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, msg.Message.DequeueCount)), stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SubscriberPollDelaySeconds), stoppingToken);
        }
    }

    protected abstract Task ProcessAsync(TEvent evt, CancellationToken cancellationToken);
}


public abstract class DetectorBase : IDetector
{
    protected DetectorBase(IEventPublisher publisher)
    {
        Publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    protected IEventPublisher Publisher { get; }

    public abstract string Name { get; }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var events = await DetectAsync(cancellationToken);
        foreach (var evt in events)
        {
            await Publisher.PublishAsync(evt, cancellationToken);
        }
    }

    protected abstract Task<IReadOnlyList<IEvent>> DetectAsync(CancellationToken cancellationToken);
}

public readonly record struct MemberScope(
    int MemberNumber,
    int? PlayerId)
{
    public string StableKey => $"m:{MemberNumber}";

    public override string ToString()
    {
        return PlayerId.HasValue
            ? $"MemberNumber={MemberNumber}, PlayerId={PlayerId.Value}"
            : $"MemberNumber={MemberNumber}, PlayerId=null";
    }
}

public abstract class MemberDetectorBase<TState>(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger logger)
    : DetectorBase<TState, MemberScope>(stateStore, publisher, mediator, lockManager, appSettings, logger)
    where TState : class, new()
{
    protected override async Task<IReadOnlyList<MemberScope>> GetScopesAsync(CancellationToken cancellationToken)
    {
        var members = await Mediator.Send(new GetJuniorMembersQuery(), cancellationToken);
        var parents = await Mediator.Send(new GetParentsQuery(), cancellationToken);

        var childScopes = members
            .Where(m => m.MemberNumber.HasValue && m.MemberNumber.Value > 0)
            .Select(m => new MemberScope(
                MemberNumber: m.MemberNumber!.Value,
                PlayerId: m.PlayerId))
            .ToList();

        var childMemberNumbers = childScopes
            .Select(s => s.MemberNumber)
            .ToHashSet();

        var parentMemberNumbers = parents
            .Where(p => p.Children != null && p.Children.Any(c => childMemberNumbers.Contains(c)))
            .Select(p => p.ParentMemberId)
            .Where(n => n > 0)
            .Distinct()
            .ToHashSet();

        var parentScopes = parentMemberNumbers
            .Select(n => new MemberScope(
                MemberNumber: n,
                PlayerId: null))
            .ToList();

        return childScopes
            .Concat(parentScopes)
            .GroupBy(s => s.MemberNumber)
            .Select(g => g.First())
            .ToArray();
    }

    protected override string GetScopeKey(MemberScope scope)
    {
        return scope.StableKey;
    }

    public async Task RunForMemberAsync(int memberNumber, CancellationToken cancellationToken)
    {
        if (memberNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memberNumber));
        }

        var scopes = await GetScopesAsync(cancellationToken);

        var scope = scopes.FirstOrDefault(s => s.MemberNumber == memberNumber);

        if (scope.MemberNumber <= 0)
        {
            Logger.LogDebug("Detector {Detector} skipping member {MemberNumber} because they are not eligible.", Name, memberNumber);
            return;
        }

        await ProcessScopeAsync(scope, cancellationToken);
    }
}

public abstract class DetectorBase<TState, TScope>(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger logger) : IDetector
    where TState : class, new()
{
    protected IDetectorStateStore StateStore { get; } = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    protected IEventPublisher Publisher { get; } = publisher ?? throw new ArgumentNullException(nameof(publisher));
    protected IMediator Mediator { get; } = mediator ?? throw new ArgumentNullException(nameof(mediator));
    protected IDistributedLockManager LockManager { get; } = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly AppSettings _app = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));

    public virtual string Name => DetectorName.For(GetType());

    public virtual int MaxConcurrency
    {
        get
        {
            var v = _app.Events.DetectorMaxConcurrency;
            return v > 0 ? v : 5;
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scopes = await GetScopesAsync(cancellationToken);

        if (scopes.Count == 0)
        {
            Logger.LogDebug("Detector {Detector} found no scopes to process.", Name);
            return;
        }

        using var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

        var tasks = scopes.Select(async scope =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                await ProcessScopeAsync(scope, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Detector {Detector} failed processing scope {Scope}.", Name, scope);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);
    }

    protected async Task ProcessScopeAsync(TScope scope, CancellationToken cancellationToken)
    {
        var scopeKey = GetScopeKey(scope);
        var resource = GetLockResource(scopeKey);

        await using var distLock = await LockManager.AcquireLockAsync(
            resource: resource,
            expiry: null,
            waitTime: null,
            retryTime: null,
            cancellationToken: cancellationToken);

        if (distLock is null || !distLock.IsAcquired)
        {
            Logger.LogDebug("Detector {Detector} skipped scope {ScopeKey} because lock was not acquired.", Name, scopeKey);
            return;
        }

        var state = await StateStore.GetAsync<TState>(Name, scopeKey, cancellationToken);

        var sink = new DetectorEventSink();

        await DetectAsync(scope, state, sink, cancellationToken);

        foreach (var evt in sink.Events)
        {
            await Publisher.PublishAsync(evt, cancellationToken);
        }

        state.UpdatedUtc = DateTimeOffset.UtcNow;
        await StateStore.UpsertAsync(Name, scopeKey, state, cancellationToken);

        Logger.LogDebug("Detector {Detector} completed scope {ScopeKey}. EventsEmitted={Count}.", Name, scopeKey, sink.Events.Count);
    }

    protected virtual Task<IReadOnlyList<TScope>> GetScopesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<TScope>>(Array.Empty<TScope>());
    }

    protected abstract Task DetectAsync(
        TScope scope,
        DetectorState<TState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken);

    protected abstract string GetScopeKey(TScope scope);

    protected virtual string GetLockResource(string scopeKey)
    {
        return $"detector:{Name}:{scopeKey}";
    }
}


public sealed class DetectorEventSink : IDetectorEventSink
{
    private readonly List<IEvent> _events = new();

    public IReadOnlyList<IEvent> Events => _events;

    public void Add(IEvent evt)
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        _events.Add(evt);
    }
}

public sealed class DetectorSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    IOptions<AppSettings> appSettings,
    ILogger<DetectorSchedulerHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    private readonly AppSettings _app = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    private readonly ILogger<DetectorSchedulerHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private sealed record ScheduledDetector(
        string Name,
        Type DetectorClrType,
        CronExpression Cron,
        bool RunOnStartup);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _nextDueUtc = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _runGates = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var scheduled = await BuildScheduleAsync(stoppingToken);

            if (scheduled.Count == 0)
            {
                _logger.LogWarning("Detector scheduler found no detectors with {Attribute}.", nameof(DetectorScheduleAttribute));
                return;
            }

            var nowUtc = DateTimeOffset.UtcNow;

            foreach (var sd in scheduled)
            {
                _runGates.TryAdd(sd.Name, new SemaphoreSlim(1, 1));

                if (sd.RunOnStartup)
                {
                    _nextDueUtc[sd.Name] = nowUtc;
                    continue;
                }

                var next = sd.Cron.GetNextOccurrence(nowUtc.UtcDateTime, TimeZoneInfo.Utc);
                if (next is null)
                {
                    _logger.LogWarning("Detector {Detector} has no next occurrence for cron {Cron}.", sd.Name, sd.Cron.ToString());
                    continue;
                }

                _nextDueUtc[sd.Name] = new DateTimeOffset(DateTime.SpecifyKind(next.Value, DateTimeKind.Utc));
            }

            _logger.LogInformation(
                "Detector scheduler started with {Count} detectors. DevOverride={Override}.",
                scheduled.Count,
                GetDevOverrideCron() ?? "(none)");

            while (!stoppingToken.IsCancellationRequested)
            {
                nowUtc = DateTimeOffset.UtcNow;

                var due = scheduled
                    .Where(sd => _nextDueUtc.TryGetValue(sd.Name, out var next) && next <= nowUtc)
                    .ToList();

                foreach (var sd in due)
                {
                    _ = RunDetectorAsync(sd, stoppingToken);
                    ScheduleNext(sd, nowUtc);
                }

                var nextWake = scheduled
                    .Select(sd => _nextDueUtc.TryGetValue(sd.Name, out var next) ? next : (DateTimeOffset?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .DefaultIfEmpty(nowUtc.AddSeconds(30))
                    .Min();

                var delay = nextWake - DateTimeOffset.UtcNow;
                if (delay < TimeSpan.FromMilliseconds(250))
                {
                    delay = TimeSpan.FromMilliseconds(250);
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task<List<ScheduledDetector>> BuildScheduleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var detectors = scope.ServiceProvider.GetServices<IDetector>().ToList();
        if (detectors.Count == 0)
        {
            return new List<ScheduledDetector>();
        }

        var scheduled = new List<ScheduledDetector>(detectors.Count);

        foreach (var detector in detectors)
        {
            var detectorType = detector.GetType();

            var attr = (DetectorScheduleAttribute?)Attribute.GetCustomAttribute(detectorType, typeof(DetectorScheduleAttribute));
            if (attr is null)
            {
                continue;
            }

            var cron = ResolveCron(attr.Cron, detector.Name);

            scheduled.Add(new ScheduledDetector(
                Name: detector.Name,
                DetectorClrType: detectorType,
                Cron: cron,
                RunOnStartup: attr.RunOnStartup));
        }

        return scheduled;
    }

    private CronExpression ResolveCron(string attributeCron, string detectorName)
    {
        var overrideCron = GetDevOverrideCron();

        if (!string.IsNullOrWhiteSpace(overrideCron))
        {
            try
            {
                _logger.LogInformation("Detector {Detector} using DEV cron override {Cron}.", detectorName, overrideCron);
                return CronExpression.Parse(overrideCron, CronFormat.Standard);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Detector {Detector} has invalid DEV cron override {Cron}; falling back to attribute cron {AttrCron}.",
                    detectorName,
                    overrideCron,
                    attributeCron);
            }
        }

        return CronExpression.Parse(attributeCron, CronFormat.Standard);
    }

    private string? GetDevOverrideCron()
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return null;
        }

        var cron = _app.Events?.DetectorFrequencyOverrideCron;
        return string.IsNullOrWhiteSpace(cron) ? null : cron;
    }

    private void ScheduleNext(ScheduledDetector sd, DateTimeOffset fromUtc)
    {
        var next = sd.Cron.GetNextOccurrence(fromUtc.UtcDateTime, TimeZoneInfo.Utc);
        if (next is null)
        {
            _logger.LogWarning("Detector {Detector} has no next occurrence for cron {Cron}.", sd.Name, sd.Cron.ToString());
            _nextDueUtc.TryRemove(sd.Name, out _);
            return;
        }

        _nextDueUtc[sd.Name] = new DateTimeOffset(DateTime.SpecifyKind(next.Value, DateTimeKind.Utc));
    }

    private async Task RunDetectorAsync(ScheduledDetector sd, CancellationToken cancellationToken)
    {
        if (!_runGates.TryGetValue(sd.Name, out var gate))
        {
            gate = _runGates.GetOrAdd(sd.Name, _ => new SemaphoreSlim(1, 1));
        }

        if (!await gate.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Skipping detector {Detector} because it is already running.", sd.Name);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var detector = scope.ServiceProvider
                .GetServices<IDetector>()
                .FirstOrDefault(d => d.Name == sd.Name && d.GetType() == sd.DetectorClrType);

            if (detector is null)
            {
                _logger.LogError("Detector {Detector} could not be resolved at runtime.", sd.Name);
                return;
            }

            _logger.LogInformation("Running detector {Detector}.", detector.Name);
            await detector.RunAsync(cancellationToken);
            _logger.LogInformation("Completed detector {Detector}.", detector.Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detector {Detector} failed.", sd.Name);
        }
        finally
        {
            gate.Release();
        }
    }
}

public sealed record MemberCategoryWindow(
    int MemberId,
    string? Category,
    DateTimeOffset? WindowStartUtc,
    DateTimeOffset? WindowEndUtc,
    IReadOnlyList<EventStreamEntity> EventsNewestFirst);

public sealed class TableMemberEventWindowReader(
    ITableStore<EventStreamEntity> eventStreamTable,
    JsonSerializerOptions json) : IMemberEventWindowReader
{
    private readonly ITableStore<EventStreamEntity> _eventStreamTable = eventStreamTable ?? throw new ArgumentNullException(nameof(eventStreamTable));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    public async Task<MemberCategoryWindow> GetLatestCategoryWindowAsync(int memberId, int take, CancellationToken cancellationToken)
    {
        var partitionKey = memberId.ToString(CultureInfo.InvariantCulture);

        var window = new List<EventStreamEntity>(256);

        string? categoryFromEvents = null;
        DateTimeOffset? windowEndUtc = null;
        DateTimeOffset? windowStartUtc = null;

        var read = 0;

        await foreach (var e in _eventStreamTable.QueryByPartitionAsync(partitionKey, odataFilter: null, cancellationToken))
        {
            if (read++ >= take)
            {
                break;
            }

            windowEndUtc ??= e.OccurredAtUtc;
            windowStartUtc = e.OccurredAtUtc;

            if (IsEventType<MembershipCategoryChangedEvent>(e.EventClrType))
            {
                var catEvt = Deserialize<MembershipCategoryChangedEvent>(e.PayloadJson);
                if (catEvt is not null && !string.IsNullOrWhiteSpace(catEvt.ToCategory))
                {
                    categoryFromEvents = catEvt.ToCategory;
                }

                break;
            }

            window.Add(e);
        }

        return new MemberCategoryWindow(
            MemberId: memberId,
            Category: categoryFromEvents,
            WindowStartUtc: windowStartUtc,
            WindowEndUtc: windowEndUtc,
            EventsNewestFirst: window);
    }

    private static bool IsEventType<T>(string eventClrType) where T : IEvent
    {
        var key = EventTypeKey.For(typeof(T));
        return string.Equals(eventClrType, key, StringComparison.Ordinal);
    }

    private T? Deserialize<T>(string? payloadJson) where T : class
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, _json);
        }
        catch
        {
            return null;
        }
    }
}

public static class DetectorName
{
    public static string For(Type detectorClrType)
    {
        if (detectorClrType is null)
        {
            throw new ArgumentNullException(nameof(detectorClrType));
        }

        var attr = detectorClrType.GetCustomAttribute<DetectorNameAttribute>(inherit: false);

        if (attr is not null && !string.IsNullOrWhiteSpace(attr.Name))
        {
            return attr.Name.Trim();
        }

        return detectorClrType.Name;
    }
}

public static class SubscriberQueueName
{
    public static string ForSubscriberType(Type subscriberType)
    {
        if (subscriberType is null) throw new ArgumentNullException(nameof(subscriberType));

        var explicitName = subscriberType.GetCustomAttribute<SubscriberNameAttribute>()?.Name;

        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return EnsureValidQueueName("sub-" + explicitName);
        }

        return EnsureValidQueueName("sub-" + subscriberType.Name);
    }

    private static string EnsureValidQueueName(string raw)
    {
        var normalised = Normalise(raw);

        if (normalised.Length < 3)
        {
            normalised = (normalised + "xxx").Substring(0, 3);
        }

        if (normalised.Length <= 63)
        {
            return normalised;
        }

        var hash = ShortHash(normalised);
        var maxPrefixLen = 63 - 1 - hash.Length;
        if (maxPrefixLen < 3)
        {
            maxPrefixLen = 3;
        }

        var prefix = normalised.Substring(0, maxPrefixLen).Trim('-');
        if (prefix.Length < 3)
        {
            prefix = (prefix + "xxx").Substring(0, 3);
        }

        var candidate = $"{prefix}-{hash}";
        if (candidate.Length > 63)
        {
            candidate = candidate.Substring(0, 63).Trim('-');
        }

        return candidate;
    }

    private static string Normalise(string input)
    {
        var s = (input ?? string.Empty).Trim().ToLowerInvariant();

        var sb = new StringBuilder(s.Length);
        var lastWasDash = false;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];

            var isAlphaNum = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
            if (isAlphaNum)
            {
                sb.Append(c);
                lastWasDash = false;
                continue;
            }

            if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        var result = sb.ToString().Trim('-');

        if (result.Length == 0)
        {
            result = "sub";
        }

        return result;
    }

    private static string ShortHash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));

        var sb = new StringBuilder(12);
        for (var i = 0; i < 6; i++)
        {
            sb.Append(bytes[i].ToString("x2"));
        }

        return sb.ToString();
    }
}

public static class QueueName
{
    public static string ForSubscriber(Type subscriberType, string? suffix = null)
    {
        if (subscriberType is null) throw new ArgumentNullException(nameof(subscriberType));

        var explicitName = subscriberType.GetCustomAttribute<SubscriberNameAttribute>(inherit: false)?.Name;
        var baseName = !string.IsNullOrWhiteSpace(explicitName)
            ? "sub-" + explicitName.Trim()
            : "sub-" + subscriberType.Name;

        return EnsureValidQueueName(baseName, suffix);
    }

    public static string EnsureValidQueueName(string rawBase, string? suffix = null)
    {
        var basePart = Normalise(rawBase);
        var suffixPart = string.IsNullOrWhiteSpace(suffix) ? string.Empty : Normalise(suffix);

        if (basePart.Length < 3) basePart = (basePart + "xxx").Substring(0, 3);

        var fullCandidate = string.IsNullOrWhiteSpace(suffixPart)
            ? basePart
            : $"{basePart}-{suffixPart}";

        if (fullCandidate.Length <= 63)
        {
            return fullCandidate;
        }

        var hash = ShortHash(fullCandidate);

        var reserved = 1 + hash.Length;
        var suffixWithDash = string.IsNullOrWhiteSpace(suffixPart) ? string.Empty : "-" + suffixPart;
        reserved += suffixWithDash.Length;

        var maxPrefixLen = 63 - reserved;
        if (maxPrefixLen < 3) maxPrefixLen = 3;

        var prefix = basePart.Length > maxPrefixLen ? basePart.Substring(0, maxPrefixLen) : basePart;
        prefix = prefix.Trim('-');
        if (prefix.Length < 3) prefix = (prefix + "xxx").Substring(0, 3);

        var finalName = string.IsNullOrWhiteSpace(suffixPart)
            ? $"{prefix}-{hash}"
            : $"{prefix}{suffixWithDash}-{hash}";

        if (finalName.Length > 63)
        {
            finalName = finalName.Substring(0, 63).Trim('-');
            if (finalName.Length < 3) finalName = (finalName + "xxx").Substring(0, 3);
        }

        return finalName;
    }

    private static string Normalise(string input)
    {
        var s = (input ?? string.Empty).Trim().ToLowerInvariant();

        var sb = new StringBuilder(s.Length);
        var lastWasDash = false;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            var isAlphaNum = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

            if (isAlphaNum)
            {
                sb.Append(c);
                lastWasDash = false;
                continue;
            }

            if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        var result = sb.ToString().Trim('-');
        return result.Length == 0 ? "q" : result;
    }

    private static string ShortHash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));

        var sb = new StringBuilder(12);
        for (var i = 0; i < 6; i++)
        {
            sb.Append(bytes[i].ToString("x2"));
        }

        return sb.ToString();
    }
}


public sealed class DetectorNameRegistry : IDetectorNameRegistry
{
    private readonly HashSet<string> _names;

    public DetectorNameRegistry(IEnumerable<Type> detectorTypes)
    {
        _names = detectorTypes
            .Select(t => DetectorName.For(t))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetAll()
    {
        return _names.ToArray();
    }

    public bool IsValid(string detectorName)
    {
        if (string.IsNullOrWhiteSpace(detectorName)) return false;
        return _names.Contains(detectorName.Trim());
    }
}