namespace BOTGC.API.Dto
{
    public sealed record StockTakeSheetDto(
       DateTime Date,
       string Division,
       string Status,
       List<StockTakeEntryDto> Entries
   );

}
