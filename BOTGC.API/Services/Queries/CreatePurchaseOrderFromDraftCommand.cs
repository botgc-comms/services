using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record CreatePurchaseOrderFromDraftCommand(PurchaseOrderDraftDto Draft) : QueryBase<bool>;
