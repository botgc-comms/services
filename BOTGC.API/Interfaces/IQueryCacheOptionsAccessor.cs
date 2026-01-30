using BOTGC.API.Services.Queries;

namespace BOTGC.API.Interfaces;

public interface IQueryCacheOptionsAccessor
{
    CacheOptions Current { get; set; }
}