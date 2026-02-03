using System.Net.Sockets;
using Azure;
using Azure.Data.Tables;

namespace BOTGC.API.Services;

public sealed class TableClientInitialiser(TableClient client, ILogger<TableClientInitialiser> logger) : IHostedService
{
    private readonly TableClient _client = client;
    private readonly ILogger _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _client.CreateIfNotExistsAsync(cancellationToken);
                _logger.LogInformation("Ensured table '{TableName}' exists.", _client.Name);
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var delay = ComputeDelay(attempt);
                _logger.LogWarning(ex, "Transient failure ensuring table '{TableName}' exists (attempt {Attempt}/{MaxAttempts}). Retrying in {DelayMs}ms.",
                    _client.Name, attempt, maxAttempts, (int)delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        await _client.CreateIfNotExistsAsync(cancellationToken);
        _logger.LogInformation("Ensured table '{TableName}' exists.", _client.Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static TimeSpan ComputeDelay(int attempt)
    {
        var baseMs = Math.Min(10_000, (int)(250 * Math.Pow(2, attempt - 1)));
        var jitterMs = Random.Shared.Next(0, 250);
        return TimeSpan.FromMilliseconds(baseMs + jitterMs);
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return false;
        }

        if (ex is RequestFailedException rfe)
        {
            if (rfe.Status == 408 || rfe.Status == 429) return true;
            if (rfe.Status >= 500 && rfe.Status <= 599) return true;
        }

        if (ex is HttpRequestException) return true;
        if (ex is IOException) return true;
        if (ex is SocketException) return true;

        if (ex.InnerException != null)
        {
            return IsTransient(ex.InnerException);
        }

        return false;
    }
}
