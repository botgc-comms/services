namespace BOTGC.API.Dto
{
    public sealed class PurchaseOrderDto
    {
        public string OrderReference { get; init; } = string.Empty;
        public int Supplier { get; init; } = 1;
        public IReadOnlyList<PurchaseOrderItemDto> Items { get; init; } = Array.Empty<PurchaseOrderItemDto>();
    }
}
