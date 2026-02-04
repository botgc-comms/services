using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services;

public sealed class ConsoleIGScrapeTelemetry : IIGScrapeTelemetry
{
    private static readonly ConcurrentDictionary<string, long> Counters = new(StringComparer.OrdinalIgnoreCase);

    public Activity Start(string dtoType, string reportUrl, string method, bool hasCacheKey, bool useCachedHtml)
    {
        var activity = new Activity("ig.scrape");
        activity.SetIdFormat(ActivityIdFormat.W3C);

        activity
            .AddTag("ig.dtoType", dtoType)
            .AddTag("ig.method", method)
            .AddTag("ig.reportUrl", reportUrl)
            .AddTag("ig.hasCacheKey", hasCacheKey)
            .AddTag("ig.useCachedHtml", useCachedHtml);

        activity.Start();

        WriteLine($"IG SCRAPE START traceId={activity.TraceId} spanId={activity.SpanId} dto={dtoType} method={method} url={reportUrl} cacheKey={hasCacheKey} cachedHtml={useCachedHtml}");
        Increment($"ig.start|dto={dtoType}|method={method}");

        return activity;
    }

    public void TrackCacheHit(string dtoType, string reportUrl, string cacheKind, int? itemCount)
    {
        WriteLine($"IG SCRAPE CACHE_HIT dto={dtoType} kind={cacheKind} items={(itemCount.HasValue ? itemCount.Value.ToString() : "n/a")} url={reportUrl}");
        Increment($"ig.cache_hit|dto={dtoType}|kind={cacheKind}");
    }

    public void TrackFetch(string dtoType, string reportUrl, string method, long bytes, long elapsedMs, bool success)
    {
        WriteLine($"IG SCRAPE FETCH dto={dtoType} method={method} ms={elapsedMs} bytes={bytes} success={success} url={reportUrl}");
        Increment($"ig.fetch|dto={dtoType}|method={method}|success={success}");
        Add($"ig.fetch.bytes|dto={dtoType}|method={method}|success={success}", bytes);
        Add($"ig.fetch.ms|dto={dtoType}|method={method}|success={success}", elapsedMs);
    }

    public void TrackParse(string dtoType, string reportUrl, int itemCount, long elapsedMs, bool success)
    {
        WriteLine($"IG SCRAPE PARSE dto={dtoType} ms={elapsedMs} items={itemCount} success={success} url={reportUrl}");
        Increment($"ig.parse|dto={dtoType}|success={success}");
        Add($"ig.parse.items|dto={dtoType}|success={success}", itemCount);
        Add($"ig.parse.ms|dto={dtoType}|success={success}", elapsedMs);
    }

    public void TrackFailure(string dtoType, string reportUrl, string stage, string errorType)
    {
        WriteLine($"IG SCRAPE FAILURE dto={dtoType} stage={stage} error={errorType} url={reportUrl}");
        Increment($"ig.failure|dto={dtoType}|stage={stage}|error={errorType}");
    }

    private static void WriteLine(string message)
    {
        Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] {message}");
    }

    private static void Increment(string key)
    {
        Counters.AddOrUpdate(key, 1, (_, v) => v + 1);
    }

    private static void Add(string key, long value)
    {
        Counters.AddOrUpdate(key, value, (_, v) => v + value);
    }

    public static string DumpSnapshot()
    {
        var sb = new StringBuilder();
        sb.AppendLine("IG SCRAPE TELEMETRY SNAPSHOT");
        foreach (var kvp in Counters)
        {
            sb.Append(kvp.Key);
            sb.Append(" = ");
            sb.AppendLine(kvp.Value.ToString());
        }
        return sb.ToString();
    }
}
