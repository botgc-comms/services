using BOTGC.API.Dto;
using System.Collections.ObjectModel;

namespace BOTGC.API.Services.Queries;

public sealed record GetManualCompetitionResultsQuery(int Year) : QueryBase<ReadOnlyCollection<ManualCompetitionResultDto>>;
