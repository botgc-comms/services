using BOTGC.API.Interfaces;

namespace BOTGC.API.Services
{
    public class NoOpDistributedLockManager : IDistributedLockManager
    {
        public Task<IDistributedLock> AcquireLockAsync(
            string resource,
            TimeSpan? expiry = null,
            TimeSpan? waitTime = null,
            TimeSpan? retryTime = null,
            CancellationToken cancellationToken = default)
        {
            IDistributedLock lockInstance = new NoOpDistributedLock();
            return Task.FromResult(lockInstance);
        }
    }
}
