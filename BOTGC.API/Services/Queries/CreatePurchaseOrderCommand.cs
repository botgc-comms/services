using BOTGC.API.Dto;
using BOTGC.API.Services.QueryHandlers;

namespace BOTGC.API.Services.Queries
{
    public sealed record CreatePurchaseOrderCommand(PurchaseOrderDto Order) : QueryBase<bool>;
}