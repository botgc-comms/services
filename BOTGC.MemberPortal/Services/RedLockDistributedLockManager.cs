using BOTGC.MemberPortal.Interfaces;
using RedLockNet.SERedis;

namespace BOTGC.MemberPortal.Services;

public class RedLockDistributedLockManager : IDistributedLockManager
{
    private readonly RedLockFactory _redLockFactory;
    private readonly ILogger<RedLockDistributedLockManager> _logger;

    public RedLockDistributedLockManager(
        RedLockFactory redLockFactory,
        ILogger<RedLockDistributedLockManager> logger)
    {
        _redLockFactory = redLockFactory ?? throw new ArgumentNullException(nameof(redLockFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IDistributedLock> AcquireLockAsync(
        string resource,
        TimeSpan? expiry = null,
        TimeSpan? waitTime = null,
        TimeSpan? retryTime = null,
        CancellationToken cancellationToken = default)
    {
        if (expiry != null)
        {
            _logger.LogWarning(
                "A custom expiry timespan was provided when acquiring a lock for resource {Resource}. " +
                "Ensure that your queue message visibility timeout is equal to or longer than the lock expiry to avoid race conditions. " +
                "Provided expiry: {Expiry}.",
                resource, expiry);
        }

        var actualExpiry = expiry ?? TimeSpan.FromSeconds((3 * 60) - 30);

        var actualWaitTime = waitTime ?? TimeSpan.FromSeconds(10);
        var actualRetryTime = retryTime ?? TimeSpan.FromMilliseconds(500);

        var redLock = await _redLockFactory.CreateLockAsync(
            resource,
            expiryTime: actualExpiry,
            waitTime: actualWaitTime,
            retryTime: actualRetryTime,
            cancellationToken: cancellationToken);

        return new RedLockDistributedLock(redLock);
    }
}
