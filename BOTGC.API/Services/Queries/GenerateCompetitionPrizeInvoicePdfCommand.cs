using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record GenerateCompetitionPrizeInvoicePdfCommand(CompetitionWinningsSummaryDto Summary, string InvoiceId) : QueryBase<byte[]>;
