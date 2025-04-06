using Services.Interfaces;
using Services.Models;
using System.Threading.Channels;

namespace Services.Common
{
    public class TeeTimeUsageTaskQueue : ITeeTimeUsageTaskQueue
    {
        private readonly Channel<TeeTimeUsageTaskItem> _channel;

        public TeeTimeUsageTaskQueue()
        {
            _channel = Channel.CreateUnbounded<TeeTimeUsageTaskItem>();
        }

        public async Task QueueTaskAsync(TeeTimeUsageTaskItem taskItem)
        {
            await _channel.Writer.WriteAsync(taskItem);
        }

        public async Task<TeeTimeUsageTaskItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
