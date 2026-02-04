using System.Diagnostics;

namespace BOTGC.API.Interfaces;

public interface IIGScrapeTelemetry
{
    Activity? Start(string dtoType, string url, string method, bool cacheEnabled, bool useCachedHtml);
    void TrackCacheHit(string dtoType, string url, string cacheMode, int? itemsCount);
    void TrackFetch(string dtoType, string url, string method, long bytes, long elapsedMs, bool isSuccess);
    void TrackParse(string dtoType, string url, int itemsCount, long elapsedMs, bool isSuccess);
    void TrackFailure(string dtoType, string url, string stage, string exceptionType);
}