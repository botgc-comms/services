namespace BOTGC.MemberPortal.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, bool force = false, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default) where T: class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}
