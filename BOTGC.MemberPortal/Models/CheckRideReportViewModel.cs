using System.ComponentModel.DataAnnotations;

namespace BOTGC.MemberPortal.Models;

public sealed class CheckRideReportViewModel : IValidatableObject
{
    [Required]
    public DateOnly? PlayedOn { get; set; }

    [Required]
    public CheckRideAssessmentAnswer? EtiquetteAndSafetyOk { get; set; }

    [Required]
    public CheckRideAssessmentAnswer? CareForCourseOk { get; set; }

    [Required]
    public CheckRideAssessmentAnswer? PaceAndImpactOk { get; set; }

    [Required]
    public CheckRideAssessmentAnswer? ReadyToPlayIndependently { get; set; }

    [Required]
    public string? RecommendedTeeBox { get; set; }

    public bool IsAlreadySignedOff { get; set; }

    public CheckRideSignedOffSummaryViewModel? SignedOffSummary { get; set; }

    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasAnyNo =
            EtiquetteAndSafetyOk == CheckRideAssessmentAnswer.No ||
            CareForCourseOk == CheckRideAssessmentAnswer.No ||
            PaceAndImpactOk == CheckRideAssessmentAnswer.No ||
            ReadyToPlayIndependently == CheckRideAssessmentAnswer.No;

        if (hasAnyNo && string.IsNullOrWhiteSpace(Notes))
        {
            yield return new ValidationResult(
                "Notes are required when any answer is No.",
                new[] { nameof(Notes) });
        }
    }
}
