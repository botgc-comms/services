namespace BOTGC.API.Dto
{
    public sealed class PurchaseOrderDraftItemDto
    {
        public int StockItemId { get; init; }
        public decimal Quantity { get; init; }
        public decimal Price { get; init; }
    }
}
