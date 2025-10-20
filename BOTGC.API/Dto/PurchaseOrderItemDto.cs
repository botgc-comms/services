namespace BOTGC.API.Dto
{
    public sealed class PurchaseOrderItemDto
    {
        public string? Id { get; init; }
        public int TillStockItemId { get; init; }
        public int StockItemId { get; init; }
        public int TillStockItemUnitId { get; init; }
        public decimal UnitCost { get; init; }
        public decimal Quantity { get; init; }
        public decimal Price { get; init; }
        public int SelectedStockRoomId { get; init; }
    }
}
