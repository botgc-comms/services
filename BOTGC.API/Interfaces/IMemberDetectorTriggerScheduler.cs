namespace BOTGC.API.Interfaces;

public interface IMemberDetectorTriggerScheduler
{
    void Schedule(
        string detectorName,
        int memberId,
        string? correlationId,
        TimeSpan quietPeriod,
        CancellationToken stoppingToken);
}