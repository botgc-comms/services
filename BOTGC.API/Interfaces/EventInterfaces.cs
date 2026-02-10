namespace BOTGC.API.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Services.Events;
using global::BOTGC.API.Models;

public interface IEvent
{
    Guid EventId { get; }
    int MemberId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}

public interface IEventTypeRegistry
{
    Type? Resolve(string eventTypeKey);
}

public interface IDetector
{
    string Name { get; }
    Task RunAsync(CancellationToken cancellationToken);
}

public interface IDetectorEventSink
{
    void Add(IEvent evt);
}

public interface IDetectorStateStore
{
    Task<DetectorState<TState>> GetAsync<TState>(string detectorName, string scopeKey, CancellationToken cancellationToken)
        where TState : class, new();

    Task UpsertAsync<TState>(string detectorName, string scopeKey, DetectorState<TState> state, CancellationToken cancellationToken)
        where TState : class, new();
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : IEvent;
}

public interface IEventStore
{
    Task AppendAsync(EventEnvelope envelope, CancellationToken cancellationToken);
    Task<bool> ExistsAsync<TEvent>(int memberId, CancellationToken cancellationToken) where TEvent : IEvent;
}

public interface ISubscriber<in TEvent> where TEvent : IEvent
{
    string Name { get; }
    Task HandleAsync(TEvent evt, CancellationToken cancellationToken);
}

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task ProcessAsync(TEvent evt, CancellationToken cancellationToken);
}

public interface ISubscriberCatalogue
{
    IReadOnlyList<Type> GetSubscribersFor(Type eventClrType);
}

public interface IDetectorNameRegistry
{
    IReadOnlyCollection<string> GetAll();
    bool IsValid(string detectorName);
}

public interface ISubscriberQueueFactory
{
    IQueueService<TPayload> Create<TPayload>(Type subscriberClrType);
}
public interface IEventPublishHelper
{
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : class, IEvent;
}

public sealed class QueueMessageWithPayload<T>
{
    public QueueMessageWithPayload(string messageId, string popReceipt, int dequeueCount, T? payload)
    {
        MessageId = messageId;
        PopReceipt = popReceipt;
        DequeueCount = dequeueCount;
        Payload = payload;
    }

    public string MessageId { get; }
    public string PopReceipt { get; }
    public int DequeueCount { get; }
    public T? Payload { get; }
}

public interface IMemberEventWindowReader
{
    Task<MemberCategoryWindow> GetLatestCategoryWindowAsync(int memberId, int take, CancellationToken cancellationToken);
}