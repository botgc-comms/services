using Services.Dto;
using Services.Dtos;

namespace Services.Common
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
                    Href = $"/api/members/{member.MemberId}",
                    Method = "GET"
                },
                new HateoasLink
                {
                    Rel = "rounds",
                    Href = $"/api/members/{member.MemberId}/rounds",
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
