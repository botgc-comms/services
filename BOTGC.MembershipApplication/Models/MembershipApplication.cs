using System.ComponentModel.DataAnnotations;

namespace BOTGC.MembershipApplication.Models
{
    public class MembershipApplication : IValidatableObject
    {
        [Required(ErrorMessage = "Please select your gender.")]
        public int Gender { get; set; }

        [Required(ErrorMessage = "Please select a title.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your first name.")]
        public string Forename { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your surname.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your date of birth.")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please provide your telephone number.")]
        [Phone(ErrorMessage = "Please enter a valid telephone number.")]
        public string Telephone { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid telephone number.")]
        public string? AlternativeTelephone { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your address line 1.")]
        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your town or city.")]
        public string Town { get; set; } = string.Empty;

        public string County { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your postcode.")]
        public string Postcode { get; set; } = string.Empty;

        public bool HasCdhId { get; set; }

        public string? CdhId { get; set; }

        [Required(ErrorMessage = "Please select your membership category.")]
        public string MembershipCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a payment type.")]
        public string PaymentType { get; set; } = string.Empty;

        public Dictionary<string, bool> ContactPreferences { get; set; } = new();

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the club rules.")]
        public bool AgreeToClubRules { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HasCdhId && string.IsNullOrWhiteSpace(CdhId))
            {
                yield return new ValidationResult(
                    "CDH ID is required if you have one.",
                    new[] { nameof(CdhId) });
            }
        }
    }
}
