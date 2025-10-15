namespace BOTGC.API.Dto
{
    public sealed class StockTakeDto: HateoasResource
    {
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StockTakeDate { get; set; }
        public string StockRoom { get; set; }
    }

}
