using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record SendPrizeNotificationEmailCommand(string PlayerId, int Position, int Division, string CompetitorName, DateTime CompetitionDate, string CompetitionName, decimal Amount) : QueryBase<bool>;
}
