using System;

namespace BOTGC.API.Dto
{
    /// <summary>
    /// Represents a summary of a golf round, including competition details, score, and metadata.
    /// </summary>
    public class RoundDto : HateoasResource
    {
        /// <summary>
        /// The competition name.
        /// </summary>
        public string CompetitionName { get; set; }

        /// <summary>
        /// The unique competition ID (if available).
        /// </summary>
        public int? CompetitionId { get; set; }

        /// <summary>
        /// Indicates whether the round was a General Play Score.
        /// </summary>
        public bool IsGeneralPlay { get; set; }

        /// <summary>
        /// Indicates whether the round was a qualifying handicapping round.
        /// </summary>
        public bool IsHandicappingRound { get; set; }

        /// <summary>
        /// The date the round was played.
        /// </summary>
        public DateTime DatePlayed { get; set; }

        /// <summary>
        /// The course name.
        /// </summary>
        public string Course { get; set; }

        /// <summary>
        /// The tee colour used for the round.
        /// </summary>
        public string TeeColour { get; set; }

        /// <summary>
        /// The unique round ID (extracted from the Gross column).
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The gross score recorded for the round.
        /// </summary>
        public string GrossScore { get; set; }

        /// <summary>
        /// The net score recorded for the round.
        /// </summary>
        public int? NetScore { get; set; }

        /// <summary>
        /// The stableford points earned in the round.
        /// </summary>
        public int? StablefordPoints { get; set; }
    }
}
