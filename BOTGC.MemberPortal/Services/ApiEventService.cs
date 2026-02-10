using BOTGC.MemberPortal.Interfaces;

namespace BOTGC.MemberPortal.Services;

public sealed class ApiEventService : IApiEventService
{
    private readonly HttpClient _client;
    private readonly ILogger<ApiEventService> _logger;

    public ApiEventService(IHttpClientFactory httpClientFactory, ILogger<ApiEventService> logger)
    {
        _client = httpClientFactory.CreateClient("Api");
        _logger = logger;
    }

    public void TriggerDetector(string detectorName, int memberId, string? correlationId = null, int quietPeriodSeconds = 5)
    {
        if (string.IsNullOrWhiteSpace(detectorName))
        {
            _logger.LogWarning("Detector trigger skipped: detectorName missing. MemberId={MemberId}", memberId);
            return;
        }

        if (memberId <= 0)
        {
            _logger.LogWarning("Detector trigger skipped: invalid memberId. DetectorName={DetectorName} MemberId={MemberId}", detectorName, memberId);
            return;
        }

        if (quietPeriodSeconds <= 0) quietPeriodSeconds = 5;
        if (quietPeriodSeconds > 300) quietPeriodSeconds = 300;

        correlationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId.Trim();

        var payload = new TriggerDetectorRequest
        {
            DetectorName = detectorName.Trim(),
            MemberId = memberId,
            CorrelationId = correlationId,
            QuietPeriodSeconds = quietPeriodSeconds
        };

        _ = SendAsync(payload, CancellationToken.None);
    }

    private async Task SendAsync(TriggerDetectorRequest payload, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var response = await _client.PostAsJsonAsync("api/events/detectors/trigger", payload, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Detector trigger scheduled. DetectorName={DetectorName} MemberId={MemberId} CorrelationId={CorrelationId}",
                    payload.DetectorName, payload.MemberId, payload.CorrelationId);
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cts.Token);

            _logger.LogWarning(
                "Detector trigger failed. Status={StatusCode} DetectorName={DetectorName} MemberId={MemberId} CorrelationId={CorrelationId} Body={Body}",
                (int)response.StatusCode, payload.DetectorName, payload.MemberId, payload.CorrelationId, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Detector trigger threw. DetectorName={DetectorName} MemberId={MemberId} CorrelationId={CorrelationId}",
                payload.DetectorName, payload.MemberId, payload.CorrelationId);
        }
    }

    private sealed class TriggerDetectorRequest
    {
        public string DetectorName { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string? CorrelationId { get; set; }
        public int? QuietPeriodSeconds { get; set; }
    }
}