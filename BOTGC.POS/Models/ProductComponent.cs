namespace BOTGC.POS.Models
{
    public sealed record ProductComponent(
        int Id,
        string Name,
        string Unit,
        decimal? Quantity,
        string Division
    );
}
