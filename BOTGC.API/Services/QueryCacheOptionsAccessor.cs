using System.Threading;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services;

public sealed class QueryCacheOptionsAccessor : IQueryCacheOptionsAccessor
{
    private static readonly AsyncLocal<CacheOptionsHolder> Holder = new();

    private sealed class CacheOptionsHolder
    {
        public CacheOptions Value { get; set; } = new CacheOptions(NoCache: false, WriteThrough: true);
    }

    public CacheOptions Current
    {
        get
        {
            var h = Holder.Value;
            return h is null ? new CacheOptions(NoCache: false, WriteThrough: true) : h.Value;
        }
        set
        {
            Holder.Value ??= new CacheOptionsHolder();
            Holder.Value.Value = value;
        }
    }
}
