using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetTillProductsQuery(): QueryBase<List<TillProductInformationDto>?>;
}
