using BOTGC.API.Dto;
using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface IObservationCompletenessPolicy
    {
        bool IsComplete(StockTakeEntryDto entry, StockItemConfig cfg);
    }
}