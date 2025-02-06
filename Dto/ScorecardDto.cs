using System;
using System.Collections.Generic;

namespace Services.Dto
{
    /// <summary>
    /// Represents the details of a golf scorecard.
    /// </summary>
    public class ScorecardDto: HateoasResource
    {
        /// <summary>
        /// The player's name.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// The player's playing handicap.
        /// </summary>
        public int ShotsReceived { get; set; }

        /// <summary>
        /// The handicap allowance (percentage).
        /// </summary>
        public string HandicapAllowance { get; set; } = string.Empty;

        /// <summary>
        /// The name of the competition.
        /// </summary>
        public string CompetitionName { get; set; } = string.Empty;

        /// <summary>
        /// The tee colour used.
        /// </summary>
        public string TeeColour { get; set; } = string.Empty;

        /// <summary>
        /// The date of the competition.
        /// </summary>
        public DateTime CompetitionDate { get; set; }

        /// <summary>
        /// The total strokes taken in the round.
        /// </summary>
        public int TotalStrokes { get; set; }

        /// <summary>
        /// The total stableford score.
        /// </summary>
        public int TotalStablefordScore { get; set; }

        /// <summary>
        /// The list of individual hole scores.
        /// </summary>
        public List<ScorecardHoleDto> Holes { get; set; } = new();
    }

    /// <summary>
    /// Represents the score details for an individual hole.
    /// </summary>
    public class ScorecardHoleDto
    {
        /// <summary>
        /// The hole number (1-18).
        /// </summary>
        public int HoleNumber { get; set; }

        /// <summary>
        /// The yardage for the hole.
        /// </summary>
        public int Yardage { get; set; }

        /// <summary>
        /// The stroke index (SI) of the hole.
        /// </summary>
        public int StrokeIndex { get; set; }

        /// <summary>
        /// The number of shots the player received on the hole
        /// </summary>
        public int ShotsReceived { get; set; }

        /// <summary>
        /// The par for the hole.
        /// </summary>
        public int Par { get; set; }

        /// <summary>
        /// The player's score for the hole.
        /// </summary>
        public string Gross { get; set; } = string.Empty;

        /// <summary>
        /// The player's score for the hole.
        /// </summary>
        public string Net { get; set; } = string.Empty;

        /// <summary>
        /// The stableford points earned for the hole.
        /// </summary>
        public int StablefordScore { get; set; }
    }
}
