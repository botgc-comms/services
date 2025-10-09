using BOTGC.API.Dto;

namespace BOTGC.API.Models
{
    internal sealed class WasteSheetCacheModel
    {
        public string Status { get; set; } = "Open";
        public Dictionary<Guid, WasteEntryDto> Entries { get; set; } = new();
    }


}
