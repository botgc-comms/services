namespace BOTGC.API.Services.Queries
{
    public sealed record SaveStockTakeCommand(DateTime TakenAtLocal, IReadOnlyDictionary<int, decimal> Quantities, IReadOnlyDictionary<int, string>? Reasons) : QueryBase<int?>;
}
