namespace BOTGC.API.Interfaces
{
    public interface IDistributedLockManager
    {
        Task<IDistributedLock> AcquireLockAsync(
            string resource,
            TimeSpan? expiry = null,
            TimeSpan? waitTime = null,
            TimeSpan? retryTime = null,
            CancellationToken cancellationToken = default);
    }

}
