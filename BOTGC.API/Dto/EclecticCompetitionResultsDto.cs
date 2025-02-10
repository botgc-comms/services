using BOTGC.API.Dto;
using System;
using System.Collections.Generic;

namespace Services.Dto
{
    public class EclecticCompetitionResultsDto
    {
        public List<EclecticScoretDto> Scores { get; set; }
    }

    public class EclecticScoretDto
    {
        public EclecticScorecardDto Scorecard { get; set; }
        public List<EclecticRoundExclusionReasonDto> ExcludedRounds { get; set; }

    }
}
