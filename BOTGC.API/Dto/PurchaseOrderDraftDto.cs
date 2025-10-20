namespace BOTGC.API.Dto
{
    public sealed class PurchaseOrderDraftDto
    {
        public string OrderReference { get; init; } = string.Empty;
        public int Supplier { get; init; } = 1;
        public IReadOnlyList<PurchaseOrderDraftItemDto> Items { get; init; } = Array.Empty<PurchaseOrderDraftItemDto>();
    }
}
