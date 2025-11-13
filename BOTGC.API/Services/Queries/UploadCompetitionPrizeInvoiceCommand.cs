using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record UploadCompetitionPrizeInvoiceCommand(
        CompetitionWinningsSummaryDto Summary,
        string InvoiceId,
        byte[] PdfBytes
    ) : QueryBase<string>;
}
