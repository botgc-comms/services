using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetWastageProductsQuery : QueryBase<List<WastageProductDto>>;
}
