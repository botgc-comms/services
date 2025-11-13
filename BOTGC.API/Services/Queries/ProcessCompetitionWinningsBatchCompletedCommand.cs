namespace BOTGC.API.Services.Queries;

public sealed record ProcessCompetitionWinningsBatchCompletedCommand(
        IReadOnlyList<int> CompetitionIds,
        DateTime CalculatedOn
    ) : QueryBase<bool>;
