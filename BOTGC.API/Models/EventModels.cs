using System;
using System.Text;
using Azure;
using Azure.Data.Tables;
using BOTGC.API.Services.Events;

namespace BOTGC.API.Models;

public sealed class EventEnvelope
{
    public Guid EventId { get; set; }
    public int MemberId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public string EventClrType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}

public sealed class DetectorStateEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string? StateJson { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class EventStreamEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public Guid EventId { get; set; }
    public int MemberId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public string EventClrType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}

public sealed class DetectorState<TState> where TState : class, new()
{
    public TState Value { get; set; } = new TState();
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EventPipelineOptions
{
    public int MaxAttempts { get; set; } = 5;

    public int DetectorPollDelaySeconds { get; set; } = 30;
    public int DispatcherPollDelaySeconds { get; set; } = 5;
    public int SubscriberPollDelaySeconds { get; set; } = 5;

    public int DispatcherBatchSize { get; set; } = 10;
    public int SubscriberBatchSize { get; set; } = 10;

    public string DetectorStateTableName { get; set; } = "DetectorState";
    public string EventStreamTableName { get; set; } = "MemberEventStream";

    public string DispatchQueueName { get; set; } = "event-dispatch";
}

public static class QueueNames
{
    public static string DispatchQueue(string dispatchQueueName) => dispatchQueueName;

    public static string DispatchDeadLetter(string dispatchQueueName) => $"{dispatchQueueName}-dlq";

    public static string SubscriberQueue(Type subscriberClrType)
    {
        if (subscriberClrType is null)
        {
            throw new ArgumentNullException(nameof(subscriberClrType));
        }

        return SubscriberQueueName.ForSubscriberType(subscriberClrType);
    }

    public static string SubscriberDeadLetter(Type subscriberClrType)
    {
        if (subscriberClrType is null)
        {
            throw new ArgumentNullException(nameof(subscriberClrType));
        }

        return SubscriberQueueName.ForSubscriberType(subscriberClrType) + "-dlq";
    }

}

public sealed record JuniorCategoryProgressResult(
    int MemberId,
    string Category,
    double PercentComplete,
    IReadOnlyList<JuniorPillarProgressResult> Pillars);

public sealed record JuniorPillarProgressResult(
    string PillarKey,
    double WeightPercent,
    bool MilestoneAchieved,
    double PillarCompletionFraction,
    double ContributionPercent);

