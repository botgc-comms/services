namespace BOTGC.POS.Models
{
    public sealed record Product(Guid Id, string Name, string Category, bool IsComposite, long igProductId, string unit);
}
