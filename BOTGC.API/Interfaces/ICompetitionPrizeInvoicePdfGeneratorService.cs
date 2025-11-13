using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces;

public interface ICompetitionPrizeInvoicePdfGeneratorService
{
    byte[] GenerateInvoice(CompetitionWinningsSummaryDto summary, string invoiceId);
}