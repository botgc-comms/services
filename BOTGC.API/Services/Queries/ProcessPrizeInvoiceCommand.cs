namespace BOTGC.API.Services.Queries;

public sealed record ProcessPrizeInvoiceCommand(int CompetitionId) : QueryBase<bool>;
