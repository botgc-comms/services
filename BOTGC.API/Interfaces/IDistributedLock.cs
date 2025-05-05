namespace BOTGC.API.Interfaces
{
    public interface IDistributedLock : IAsyncDisposable
    {
        bool IsAcquired { get; }
    }

}
