﻿using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionTaskQueue
    {
        Task QueueTaskAsync(CompetitionTaskItem taskItem);
        Task<CompetitionTaskItem> DequeueAsync(CancellationToken cancellationToken);
    }

    public interface ITeeTimeUsageTaskQueue
    {
        Task QueueTaskAsync(TeeTimeUsageTaskItem taskItem);
        Task<TeeTimeUsageTaskItem> DequeueAsync(CancellationToken cancellationToken);
    }
}