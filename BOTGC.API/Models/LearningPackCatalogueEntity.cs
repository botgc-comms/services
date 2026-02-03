using Azure;
using Azure.Data.Tables;

namespace BOTGC.API.Models;

public sealed class LearningPackCatalogueEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string PackId { get; set; } = string.Empty;
    public string? MandatoryForCsv { get; set; }
    public string? ContentVersion { get; set; }
}