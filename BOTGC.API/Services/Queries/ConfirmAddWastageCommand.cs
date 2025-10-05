namespace BOTGC.API.Services.Queries
{
    public record ConfirmAddWastageCommand : QueryBase<bool>
    {
        public required DateTime WastageDateUtc { get; init; }
        public required int ProductId { get; init; }
        public required int StockRoomId { get; init; }
        public required int Quantity { get; init; }
        public required string Reason { get; init; }
    }
}
