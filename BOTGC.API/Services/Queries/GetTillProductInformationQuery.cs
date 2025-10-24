using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetTillProductInformationQuery(int ProductId) : QueryBase<TillProductInformationDto?>;
}
