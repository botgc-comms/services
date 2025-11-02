namespace BOTGC.POS.Models
{
    public sealed record ProductDetails(
        Guid Id,
        string Name,
        string Category,
        long igProductId,
        string Unit,
        List<ProductComponent> Components
    );
}
