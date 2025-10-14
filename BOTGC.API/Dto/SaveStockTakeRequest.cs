using System.ComponentModel.DataAnnotations;

namespace BOTGC.API.Dto
{
    public sealed class SaveStockTakeRequest : IValidatableObject
    {
        [Required]
        public DateTime TakenAtLocal { get; set; }

        [Required]
        [MinLength(1)]
        public List<SaveStockTakeItem> Items { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Items.Count != Items.Select(i => i.StockItemId).Distinct().Count())
                yield return new ValidationResult("Duplicate StockItemId values are not allowed.", new[] { nameof(Items) });
        }
    }

}
