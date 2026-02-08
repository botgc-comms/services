using BOTGC.API.Services.EventBus.Events;

namespace BOTGC.API.Services.Queries;

public sealed record GetLatestJuniorProgressEventQuery(int MemberId, int Take = 500)
    : QueryBase<JuniorProgressChangedEvent?>;
