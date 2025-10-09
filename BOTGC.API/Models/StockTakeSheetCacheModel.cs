using BOTGC.API.Dto;

namespace BOTGC.API.Models
{
    public sealed class StockTakeSheetCacheModel
    {
        public string Status { get; set; } = "Open";
        public Dictionary<int, StockTakeEntryDto> Entries { get; set; } = new Dictionary<int, StockTakeEntryDto>();
        public string Division { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
