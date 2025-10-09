using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public sealed record AddToWasteSheetCommand(
        DateTime Date,
        Guid ClientEntryId,
        Guid OperatorId,
        Guid ProductId,
        long IGProductId, 
        string Unit, 
        string ProductName,
        string Reason,
        decimal Quantity
    ) : QueryBase<AddResultDto>;
}
