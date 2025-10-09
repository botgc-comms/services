namespace BOTGC.API.Dto
{
    public sealed record WasteSheetDto(
        DateTime Date,
        string Status,
        List<WasteEntryDto> Entries
    );

}
