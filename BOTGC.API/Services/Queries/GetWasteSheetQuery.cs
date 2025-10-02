using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetWasteSheetQuery(DateTime Date) : QueryBase<WasteSheetDto?>;
}
