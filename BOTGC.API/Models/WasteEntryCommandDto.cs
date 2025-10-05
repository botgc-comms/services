using System.ComponentModel.DataAnnotations;

namespace BOTGC.API.Models
{
    public sealed record WasteEntryCommandDto(
           [property: Required] DateTime WastageDateUtc,
           [property: Required] int ProductId,
           [property: Required] int StockRoomId,
           [property: Required] int Quantity,
           [property: Required] string Reason,
           Guid ClientEntryId,
           Guid OperatorId,
           string? DeviceId,
           string? ProductName
       );
}
