using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public sealed record CreateStockTakeMondayTicketsCommand(StockTakeCompletedCommand Ticket) : QueryBase<bool>;
}
