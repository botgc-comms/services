// DTOs
using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public enum TeeColor { White, Yellow, Red }

    public record TeeKey(Gender Gender, TeeColor Color);

    public class CompetitionSettingsDto : HateoasResource
    {
        public int? Id { get; set; }
        public string[] Tags { get; set; }
        public string Comments { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Format { get; set; }
        public string ResultsDisplay { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, int>? MultiPartCompetition { get; set;     }
        public CompetitionSignupSettingsDto SignupSettings { get; set; }

        public Dictionary<string, int> CoursePar { get; set; } = new Dictionary<string, int>
        {
            ["gents.white"] = 71,
            ["gents.yellow"] = 70,
            ["gents.red"] = 70,
            ["ladies.yellow"] = 75,
            ["ladies.red"] = 73,
        };

        // New fields from “Results Display” slate
        public int? HandicapAllowancePercent { get; set; }
        public int? Divisions { get; set; }
        public int? Division1Limit { get; set; }
        public int? Division2Limit { get; set; }
        public int? Division3Limit { get; set; }
        public int? Division4Limit { get; set; }
        public int? PrizesPerDivision { get; set; }
        public string PrizeStructure { get; set; }
        public bool? ResultsViewableInProgress { get; set; }
        public bool? TwosCompetitionIncluded { get; set; }
        public string TieResolution { get; set; }

        // Touchscreen/Mobile slate
        public bool? TouchscreenAllowPlayerInput { get; set; }
        public bool? TouchscreenRequireCheckIn { get; set; }
        public bool? TouchscreenShowLeaderboard { get; set; }
        public bool? MobileAllowPlayerInput { get; set; }
        public bool? MobileAllowPartnerScoring { get; set; }
        public bool? MobileShowLeaderboard { get; set; }

        // Extra options slate
        public bool? EnglandGolfMedal { get; set; }
        public bool? PartOfAlternateDayCompetition { get; set; }
    }

    public class CompetitionSummaryDto : HateoasResource
    {
        public int? Id { get; set; }
        public Dictionary<string, int>? MultiPartCompetition { get; set; }
    }

    public sealed class CompetitionSignupSettingsDto: HateoasResource
    {
        public bool? CompetitionPaymentsRequired { get; set; }
        public List<SignupChargeDto> Charges { get; set; } = new List<SignupChargeDto>();
        public bool? PaymentsFromMemberAccounts { get; set; }
        public bool? DirectPayments { get; set; }
        public string? PaymentDue { get; set; }
    }

    public sealed class SignupChargeDto
    {
        public string? Description { get; set; }
        public bool? Required { get; set; }
        public decimal? Amount { get; set; }
    }
}
