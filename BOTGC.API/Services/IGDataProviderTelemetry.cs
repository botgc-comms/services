using System.Diagnostics;
using System.Text;
using BOTGC.API.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace BOTGC.API.Services;

public sealed class IGDataProviderTelemetry : IIGScrapeTelemetry
{
    private static readonly ActivitySource ActivitySource = new("BOTGC.API.IGDataProvider");
    private readonly TelemetryClient _telemetry;

    public IGDataProviderTelemetry(TelemetryClient telemetry)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    public Activity? Start(string dtoType, string url, string method, bool cacheEnabled, bool useCachedHtml)
    {
        var activity = ActivitySource.StartActivity("IG Scrape", ActivityKind.Internal);

        activity?.SetTag("ig.dtoType", dtoType);
        activity?.SetTag("ig.url", url);
        activity?.SetTag("ig.method", method);
        activity?.SetTag("ig.cacheEnabled", cacheEnabled);
        activity?.SetTag("ig.useCachedHtml", useCachedHtml);

        return activity;
    }

    public void TrackCacheHit(string dtoType, string url, string cacheMode, int? itemsCount)
    {
        var props = BuildProps(dtoType, url);
        props["cacheMode"] = cacheMode;

        if (itemsCount.HasValue)
        {
            _telemetry.TrackMetric("IG.CacheHit.Items", itemsCount.Value, props);
        }

        _telemetry.TrackMetric("IG.CacheHit.Count", 1, props);
    }

    public void TrackFetch(string dtoType, string url, string method, long bytes, long elapsedMs, bool isSuccess)
    {
        var props = BuildProps(dtoType, url);
        props["method"] = method;
        props["success"] = isSuccess ? "true" : "false";

        _telemetry.TrackMetric("IG.Fetch.Count", 1, props);
        _telemetry.TrackMetric("IG.Fetch.Bytes", bytes, props);
        _telemetry.TrackMetric("IG.Fetch.DurationMs", elapsedMs, props);
    }

    public void TrackParse(string dtoType, string url, int itemsCount, long elapsedMs, bool isSuccess)
    {
        var props = BuildProps(dtoType, url);
        props["success"] = isSuccess ? "true" : "false";

        _telemetry.TrackMetric("IG.Parse.Count", 1, props);
        _telemetry.TrackMetric("IG.Parse.Items", itemsCount, props);
        _telemetry.TrackMetric("IG.Parse.DurationMs", elapsedMs, props);
    }

    public void TrackFailure(string dtoType, string url, string stage, string exceptionType)
    {
        var props = BuildProps(dtoType, url);
        props["stage"] = stage;
        props["exceptionType"] = exceptionType;

        _telemetry.TrackMetric("IG.Failure.Count", 1, props);

        var evt = new EventTelemetry("IG Failure");
        foreach (var kv in props) evt.Properties[kv.Key] = kv.Value;
        _telemetry.TrackEvent(evt);
    }

    public static long GetUtf8ByteCount(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        return Encoding.UTF8.GetByteCount(s);
    }

    private static Dictionary<string, string> BuildProps(string dtoType, string url)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dtoType"] = dtoType,
            ["url"] = url
        };
    }
}