using BOTGC.API.Interfaces;

namespace BOTGC.API.Services
{
    public class NoOpDistributedLock : IDistributedLock
    {
        public bool IsAcquired => true;

        public ValueTask DisposeAsync()
        {
            // Nothing to dispose
            return ValueTask.CompletedTask;
        }
    }
}
