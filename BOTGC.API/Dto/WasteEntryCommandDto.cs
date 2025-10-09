using System.ComponentModel.DataAnnotations;

namespace BOTGC.API.Dto
{
    public sealed record WasteEntryCommandDto(
           [property: Required] DateTime WastageDateUtc,
           [property: Required] long ProductId,
           [property: Required] int Quantity,
           [property: Required] string Reason,
           Guid ClientEntryId,
           Guid OperatorId,
           int? StockRoomId,
           string? ProductName
       );
}
