namespace BOTGC.API.Interfaces
{
    public interface ICacheKeyProvider<in TRequest>
    {
        string GetCacheKey(TRequest request);
    }
}
