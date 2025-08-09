namespace BOTGC.API.Models
{
    public class StockAnalysisTaskItem
    {
        public DateTime? RequestedAt { get; set; } = DateTime.UtcNow;
        public string? RequestedBy { get; set; }
        public bool? Force { get; set; } = true;
    }

}
