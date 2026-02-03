using Azure.Data.Tables;
using Azure;

namespace BOTGC.API.Models;

public sealed class LearningPackProgressEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string PackId { get; set; } = string.Empty;

    public DateTimeOffset? FirstViewedAtUtc { get; set; }
    public DateTimeOffset? LastViewedAtUtc { get; set; }
    public int PagesViewedCount { get; set; }

    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? CompletedContentVersion { get; set; }

    public string? ReadPageIdsCsv { get; set; }
}