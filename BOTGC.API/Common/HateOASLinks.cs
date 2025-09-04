using BOTGC.API.Dto;

namespace BOTGC.API.Common
{
    public static class HateOASLinks
    {
        public static List<HateoasLink> GetMemberLinks(MemberDto member)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "self",
                    Href = $"/api/members/{member.MemberNumber}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "rounds",
                    Href = $"/api/members/{member.MemberNumber}/rounds",
                    Method = "GET"
                }
            };
        }

        public static List<HateoasLink> GetNewMemberLookupLinks(NewMemberLookupDto member)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "self",
                    Href = $"/api/members/{member.MemberNumber}",
                    Method = "GET"
                }
            };
        }

        public static List<HateoasLink> GetCompetitionLinks(CompetitionDto competition)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "settings",
                    Href = $"/api/competitions/{competition.Id}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "leaderboard",
                    Href = $"/api/competitions/{competition.Id}/leaderboard",
                    Method = "GET"
                }
            };
        }

        public static List<HateoasLink> GetCompetitionLinks(CompetitionSummaryDto competition)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "settings",
                    Href = $"/api/competitions/{competition.Id}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "leaderboard",
                    Href = $"/api/competitions/{competition.Id}/leaderboard",
                    Method = "GET"
                }
            };
        }

        public static List<HateoasLink> GetCompetitionLinks(CompetitionSettingsDto competition)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "settings",
                    Href = $"/api/competitions/{competition.Id}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "leaderboard",
                    Href = $"/api/competitions/{competition.Id}/leaderboard",
                    Method = "GET"
                }
            };
        }

        public static List<HateoasLink> GetRoundLinks(RoundDto round)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "self",
                    Href = $"/api/rounds/{round.RoundId}",
                    Method = "GET"
                }
            };
        }
        public static List<HateoasLink> GetTrophyLinks(TrophyDto trophy, string nextTrophyId, string previousTrophyId)
        {
            return new List<HateoasLink>
            {
                new HateoasLink
                {
                    Rel = "self",
                    Href = $"/api/trophies/{trophy.Slug}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "previous",
                    Href = $"/api/trophies/{previousTrophyId}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "next",
                    Href = $"/api/trophies/{nextTrophyId}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "winnerImage",
                    Href = $"/api/images/winners/{trophy.Slug}",
                    Method = "GET"
                }
            };
        }

    }
}
