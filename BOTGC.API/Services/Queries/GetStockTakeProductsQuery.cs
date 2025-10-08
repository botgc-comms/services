using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockTakeProductsQuery : QueryBase<List<DivisionStockTakeSuggestionDto>>
    {
        // Represents the maxiumm complexity budget per division.
        public int? ComplexityBudgetPerDivision { get; set; } = 10;
        public int? MinDaysSinceLastStockTake { get; set; } = 30;
    }
}
