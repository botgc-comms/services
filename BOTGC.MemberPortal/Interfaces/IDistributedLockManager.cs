namespace BOTGC.MemberPortal.Interfaces;

public interface IDistributedLockManager
{
    Task<IDistributedLock> AcquireLockAsync(
        string resource,
        TimeSpan? expiry = null,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}
