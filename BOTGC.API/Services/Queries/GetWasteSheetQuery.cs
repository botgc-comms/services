using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetWasteSheetQuery(DateTime Date) : QueryBase<WasteSheetDto?>;
}
