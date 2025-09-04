using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetCompetitionResultsForJuniorsQuery : QueryBase<List<PlayerCompetitionResultsDto>>
    {
        public DateTime? FromDate { get; set; } = new DateTime(DateTime.UtcNow.Year, 1, 1);
        public DateTime? ToDate { get; set; } = DateTime.UtcNow;
        public int? MaxFinishingPosition { get; set; } = 10;
    }
}
