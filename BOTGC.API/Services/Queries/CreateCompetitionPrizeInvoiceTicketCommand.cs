using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record CreateCompetitionPrizeInvoiceTicketCommand(CompetitionWinningsSummaryDto Summary, byte[] PdfBytes, string InvoiceId) : QueryBase<string?>;
