using BOTGC.API.Dto;
using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries
{
    public sealed record ProcessStockTakeCommand(DateTime Date, string Division, StockTakeSheetDto Sheet, string CorrelationId, DateTime EnqueuedAtUtc)
    : QueryBase<bool>;
}
