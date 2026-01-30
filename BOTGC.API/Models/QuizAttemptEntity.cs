using Azure.Data.Tables;
using Azure;

namespace BOTGC.API.Models;

public sealed class QuizAttemptEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string AttemptId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int PassMark { get; set; }

    public string ContentVersion { get; set; } = string.Empty;
    public string QuestionIdsJson { get; set; } = "[]";
    public string Difficulty { get; set; } = string.Empty;
}
