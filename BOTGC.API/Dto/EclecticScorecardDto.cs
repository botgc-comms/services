using System;
using System.Collections.Generic;

namespace BOTGC.API.Dto
{
    /// <summary>
    /// Represents the details of a golf scorecard.
    /// </summary>
    public class EclecticScorecardDto: HateoasResource
    {
        /// <summary>
        /// The player's name.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// The total stableford score.
        /// </summary>
        public int TotalStablefordScore { get; set; }

        /// <summary>
        /// The list of individual hole scores.
        /// </summary>
        public List<ExclecticScorecardHoleDto> Holes { get; set; } = new();
    }

    /// <summary>
    /// Represents the score details for an individual hole.
    /// </summary>
    public class ExclecticScorecardHoleDto
    {
        /// <summary>
        /// The hole number (1-18).
        /// </summary>
        public int HoleNumber { get; set; }

        /// <summary>
        /// The Date of the round that contributed to this score
        /// </summary>
        public DateTime RoundDate { get; set; }

        /// <summary>
        /// The id of the round that contributed to this score
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The stableford points earned for the hole.
        /// </summary>
        public int StablefordScore { get; set; }

        /// <summary>
        ///  The other scores that are not counted for this hole
        /// </summary>
        public List<EclecticScorecardHoleUncountedScoreDto> UncountedScores { get; set; }
    }

    public class EclecticScorecardHoleUncountedScoreDto
    {
        /// <summary>
        /// The Date of the round that contributed to this score
        /// </summary>
        public DateTime RoundDate { get; set; }

        /// <summary>
        /// The id of the round that contributed to this score
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The stableford points earned for the hole.
        /// </summary>
        public int StablefordScore { get; set; }
    }
}
