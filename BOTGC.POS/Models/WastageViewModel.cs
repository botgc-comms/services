namespace BOTGC.POS.Models
{
    public sealed class WastageViewModel
    {
        public WasteSheet Sheet { get; set; } = new();
        public IReadOnlyList<Product> TopProducts { get; set; } = Array.Empty<Product>();
        public IReadOnlyList<Reason> Reasons { get; set; } = Array.Empty<Reason>();
        public IReadOnlyList<Operator> Operators { get; set; } = Array.Empty<Operator>();
    }
}
