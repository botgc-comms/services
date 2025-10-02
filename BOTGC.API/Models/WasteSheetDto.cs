namespace BOTGC.API.Models
{
    public sealed record WasteSheetDto(
        DateTime Date,
        string Status,
        List<WasteEntryDto> Entries
    );

}
