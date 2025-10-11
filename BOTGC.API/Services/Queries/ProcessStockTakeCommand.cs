using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries
{
    public sealed record ProcessStockTakeCommand(DateTime Date, string Division)
    : QueryBase<ProcessStockTakeResult>;
}
