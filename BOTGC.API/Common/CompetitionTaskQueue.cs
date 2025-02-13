﻿using Services.Interfaces;
using Services.Models;
using System.Threading.Channels;

namespace Services.Common
{
    public class CompetitionTaskQueue : ICompetitionTaskQueue
    {
        private readonly Channel<CompetitionTaskItem> _channel;

        public CompetitionTaskQueue()
        {
            _channel = Channel.CreateUnbounded<CompetitionTaskItem>();
        }

        public async Task QueueTaskAsync(CompetitionTaskItem taskItem)
        {
            await _channel.Writer.WriteAsync(taskItem);
        }

        public async Task<CompetitionTaskItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
