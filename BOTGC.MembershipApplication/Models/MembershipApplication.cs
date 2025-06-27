using PhoneNumbers;
using System.ComponentModel.DataAnnotations;

namespace BOTGC.MembershipApplication.Models
{
    public class AssertTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is bool b && b;
        }
    }

    public class UkPhoneAttribute : ValidationAttribute
    {
        private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

        public override bool IsValid(object? value)
        {
            var input = value as string;
            if (string.IsNullOrWhiteSpace(input))
                return true;

            try
            {
                var number = _phoneUtil.Parse(input, "GB");
                return _phoneUtil.IsValidNumberForRegion(number, "GB");
            }
            catch (NumberParseException)
            {
                return false;
            }
        }
    }

    public class MembershipApplication : IValidatableObject
    {
        public string ApplicationId { get; set; } = Guid.NewGuid().ToString();

        public string? ReferrerId { get; set; } = string.Empty;
        public string? ReferrerName { get; set; } = string.Empty;
        public string? Fingerprint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your gender that you were born with.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Please select a title.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your first name.")]
        public string Forename { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your surname.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your date of birth.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please provide your telephone number.")]
        [UkPhone(ErrorMessage = "Please enter a valid UK telephone number.")]
        public string Telephone { get; set; } = string.Empty;

        [UkPhone(ErrorMessage = "Please enter a valid UK alternative telephone number.")]
        public string? AlternativeTelephone { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your address line 1.")]
        public string AddressLine1 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your second address line.")]
        public string AddressLine2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your town or city.")]
        public string Town { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your county.")]
        public string County { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your postcode.")]
        public string Postcode { get; set; } = string.Empty;

        public bool HasCdhId { get; set; }

        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "Please enter a valid CDH ID.")]
        public string? CdhId { get; set; }

        [Required(ErrorMessage = "Please select a membership category.")]
        public string? MembershipCategory { get; set; } = string.Empty;

        public Dictionary<string, bool> ContactPreferences { get; set; } = new();

        [Required(ErrorMessage = "You must agree to the club rules.")]
        [AssertTrue(ErrorMessage = "You must agree to the club rules.")]
        [Display(Name = "Agree to the club rules")]
        public bool AgreeToClubRules { get; set; } 

        public bool ArrangeFinance { get; set; }

        public string Channel { get; set; } = "Direct";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var financeEligible = new[] { "7Day", "6Day", "5Day", "Intermediate" };

            if (!financeEligible.Contains(MembershipCategory) && ArrangeFinance)
            {
                yield return new ValidationResult(
                    "Finance can only be arranged for full, 6-day, 5-day or intermediate membership.",
                    new[] { nameof(ArrangeFinance) });
            }

            if (HasCdhId && string.IsNullOrWhiteSpace(CdhId))
            {
                yield return new ValidationResult(
                    "CDH ID is required if you have one.",
                    new[] { nameof(CdhId) });
            }

            var age = CalculateAge(DateOfBirth!.Value);

            switch (MembershipCategory)
            {
                case "7Day":
                case "6Day":
                case "5Day":
                    if (age < 18)
                    {
                        yield return new ValidationResult(
                            "This membership category is only available to applicants aged 18 or over.",
                            new[] { nameof(MembershipCategory) });
                    }
                    break;

                case "Student":
                    if (age < 18 || age > 21)
                    {
                        yield return new ValidationResult(
                            "Student membership is only available to applicants aged between 18 and 21.",
                            new[] { nameof(MembershipCategory) });
                    }
                    break;

                case "Intermediate":
                    if (age < 21 || age > 29)
                    {
                        yield return new ValidationResult(
                            "Affordable membership is only available to applicants aged between 21 and 29.",
                            new[] { nameof(MembershipCategory) });
                    }
                    break;

                case "Junior":
                    if (age > 18)
                    {
                        yield return new ValidationResult(
                            "Junior membership is only available to applicants aged 18 or under.",
                            new[] { nameof(MembershipCategory)});
                    }
                    break;
            }
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
