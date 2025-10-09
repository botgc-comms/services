using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IStockTakeService
{
    Task<IReadOnlyList<StockTakeDivisionPlan>> GetPlannedAsync();
    Task<bool> CommitAsync(Guid operatorId, DateTimeOffset timestamp, IEnumerable<StockTakeObservationDto> observations);
}
