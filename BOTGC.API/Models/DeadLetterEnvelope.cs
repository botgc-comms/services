namespace BOTGC.API.Models;

public class DeadLetterEnvelope<T>
{
    public T? OriginalMessage { get; set; } = default!;
    public long DequeueCount { get; set; }
    public DateTime FailedAt { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }

    public DeadLetterEnvelope() { }

    public DeadLetterEnvelope(T originalMessage, long dequeueCount, DateTime failedAtUtc, Exception? exception)
    {
        OriginalMessage = originalMessage;
        DequeueCount = dequeueCount;
        FailedAt = failedAtUtc;

        if (exception != null)
        {
            ExceptionType = exception.GetType().FullName;
            ExceptionMessage = exception.Message;
            StackTrace = exception.StackTrace;
        }
    }
}
