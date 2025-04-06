using System;

namespace Services.Dto
{
    public class TeeSheetDto: TeeTimesDto
    {
        public DateTime Date { get; set; }
    }

    public class TeeTimesDto : HateoasResource
    {
        public List<TeeTimePlayersDto> TeeTimes { get; set; }

        public List<PlayerTeeTimesDto> Players { get; set; }

    }

    public class TeeTimePlayersDto : TeeTimeDto
    {
        public List<PlayerDto> Players { get; set; }
    }

    public class TeeTimeDto
    {
        public DateTime Time { get; set; }
    }

    public class TeeTimeBookingDto: TeeTimeDto
    {
        public bool IsCompetitionBooking { get; set; }
    }

    public class PlayerTeeTimesDto: PlayerDto
    {
        public List<TeeTimeBookingDto> TeeTimes { get; set; }
    }

    public class PlayerDto
    {
        public string FullName { get; set; }
    }

    public class MemberTeeStatsDto
    {
        public MemberDto Membership { get; set; }
        public int TotalRounds { get; set; }
        public int Year { get; set; }
        public int CompetitionRounds { get; set; }
        public int QuietPeriodRounds { get; set; }
        public double QuietPeriodPercentage => TotalRounds > 0 ? (QuietPeriodRounds * 100.0) / TotalRounds : 0;
        public double CompetitionPercentage => TotalRounds > 0 ? (CompetitionRounds * 100.0) / TotalRounds : 0;
        public string Group { get; set; } // Goldilocks, Amber, Other
    }
}
