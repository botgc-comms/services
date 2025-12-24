namespace BOTGC.MemberPortal.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, bool force = false) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T: class;
    Task RemoveAsync(string key);
}
