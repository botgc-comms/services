using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record SendCompetitionPrizeInvoiceEmailCommand(CompetitionWinningsSummaryDto Summary, byte[] PdfBytes, string? TicketId, string InvoiceId, string InvoiceUrl) : QueryBase<bool>;
