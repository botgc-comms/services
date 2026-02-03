using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries;

public sealed record GetMandatoryLearningPacksForChildQuery(int ChildMemberId)
    : QueryBase<MandatoryLearningPacksForChildDto>;

