namespace BOTGC.POS.Models
{
    public sealed class WasteSheet
    {
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public List<WasteEntry> Entries { get; set; } = new();
        public bool Submitted { get; set; }
    }
}
