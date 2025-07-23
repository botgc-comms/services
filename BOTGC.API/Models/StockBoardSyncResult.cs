namespace BOTGC.API.Models
{
    public class StockBoardSyncResult
    {
        public List<string> Created { get; set; } = new();
        public List<string> Updated { get; set; } = new();
        public List<string> ExistingMondayIds { get; set; } = new();
    }

}
