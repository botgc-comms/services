using Microsoft.Extensions.Options;
using Services.Controllers;
using Services.Dto;
using Services.Interfaces;
using Services.Models;
using System.Diagnostics;
using System.Linq;

namespace Services.Services.CompetitionProcessors
{
    public class JuniorEclecticCompetitionProcessor : ICompetitionProcessor
    {
        private readonly AppSettings _settings;
        private readonly IReportService _reportService;
        private readonly ILogger<JuniorEclecticCompetitionProcessor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JuniorEclecticCompetitionProcessor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public JuniorEclecticCompetitionProcessor(IOptions<AppSettings> settings,
                                                  ILogger<JuniorEclecticCompetitionProcessor> logger,
                                                  IReportService reportService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        public async Task ProcessCompetitionAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            const int TOPSCORES = 5;
            const int TOPGPSCORES = 3;

            var eclecticScorecards = new List<EclecticScorecardDto>();

            #region STEP 1: Get all Junior Members
            
            var juniorMembers = await _reportService.GetJuniorMembersAsync();

            var roundExlusions = new List<RoundExclusionReason>();
            var holeExclusions = new List<HoleExclusionReason>();

            #endregion

            // step 2: for each junior member get that juniors rounds
            foreach (var juniorMember in juniorMembers)
            {
                #region STEP 2: Get all rounds for each Junior Member

                var rounds = await _reportService.GetRoundsByMemberIdAsync(juniorMember.MemberId.ToString());

                #endregion

                #region STEP 3: Identify the eligable rounds

                // step 3a: Get rounds played in the date range
                var roundsInDateRange = rounds.Where(r => r.DatePlayed >= fromDate && r.DatePlayed <= toDate);

                // step 3b: separate competition rounds from general play rounds
                var competitionRounds = roundsInDateRange.Where(r => r.IsHandicappingRound && !r.IsGeneralPlay);
                var generalPlayRounds = roundsInDateRange.Where(r => r.IsHandicappingRound && r.IsGeneralPlay);

                // step 3c: remove any general play round not played within 6 weeks of a competition
                var validGeneralPlayRounds = generalPlayRounds.Where(gpr => competitionRounds.Any(cr => Math.Abs((gpr.DatePlayed - cr.DatePlayed).TotalDays) <= 42));

                // step 3d: update the collection of exclusion reasons
                roundExlusions.AddRange(
                    generalPlayRounds
                        .Except(validGeneralPlayRounds)
                        .Select(gpr => new RoundExclusionReason()
                        {
                            MemberId = juniorMember.MemberId.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            DatePlayed = gpr.DatePlayed, 
                            ExclusionReason = $"General play round on {gpr.DatePlayed} was not played within 6 weeks of any competition round."
                        })
                        .ToList()
                    );

                #endregion

                #region STEP 4: Get separate front and back nine scorecards

                // step 4: get the score cards for each of the rounds
                var competitionScorecards = new List<ScorecardDto>();

                foreach (var competitionRound in competitionRounds)
                {
                    var scorecard = await _reportService.GetScorecardForRoundAsync(competitionRound.RoundId.ToString());
                    if (scorecard != null)
                    {
                        competitionScorecards.Add(scorecard);
                    }
                }

                var generalPlayScorecards = new List<ScorecardDto>();

                foreach (var generalPlayRound in validGeneralPlayRounds)
                {
                    var scorecard = await _reportService.GetScorecardForRoundAsync(generalPlayRound.RoundId.ToString());
                    if (scorecard != null)
                    {
                        generalPlayScorecards.Add(scorecard);
                    }
                }

                #endregion

                #region STEP 5: Filter the best competition and general play cards for the front and back nine

                var frontNineCompetitionScorecards = new List<ScorecardDto>();
                var backNineCompetitionScorecards = new List<ScorecardDto>();
                var frontNineGeneralPlayScorecards = new List<ScorecardDto>();
                var backNineGeneralPlayScorecards = new List<ScorecardDto>();

                foreach (var scorecard in competitionScorecards)
                {
                    // Split the holes into front nine and back nine
                    var frontNineHoles = scorecard.Holes.Where(h => h.HoleNumber <= 9).ToList();
                    var backNineHoles = scorecard.Holes.Where(h => h.HoleNumber > 9).ToList();

                    // Create new ScorecardDto for front nine if there are any front nine holes
                    if (frontNineHoles.Any())
                    {
                        var frontNineScorecard = new ScorecardDto
                        {
                            PlayerName = scorecard.PlayerName,
                            ShotsReceived = scorecard.ShotsReceived,
                            HandicapAllowance = scorecard.HandicapAllowance,
                            CompetitionName = scorecard.CompetitionName,
                            TeeColour = scorecard.TeeColour,
                            DatePlayed = scorecard.DatePlayed,
                            TotalStrokes = frontNineHoles.Sum(h => int.TryParse(h.Gross, out var gross) ? gross : 0),
                            TotalStablefordScore = frontNineHoles.Sum(h => h.StablefordScore),
                            Holes = frontNineHoles
                        };
                        frontNineCompetitionScorecards.Add(frontNineScorecard);
                    }

                    // Create new ScorecardDto for back nine if there are any back nine holes
                    if (backNineHoles.Any())
                    {
                        var backNineScorecard = new ScorecardDto
                        {
                            PlayerName = scorecard.PlayerName,
                            ShotsReceived = scorecard.ShotsReceived,
                            HandicapAllowance = scorecard.HandicapAllowance,
                            CompetitionName = scorecard.CompetitionName,
                            TeeColour = scorecard.TeeColour,
                            DatePlayed = scorecard.DatePlayed,
                            TotalStrokes = backNineHoles.Sum(h => int.TryParse(h.Gross, out var gross) ? gross : 0),
                            TotalStablefordScore = backNineHoles.Sum(h => h.StablefordScore),
                            Holes = backNineHoles
                        };
                        backNineCompetitionScorecards.Add(backNineScorecard);
                    }
                }

                foreach (var scorecard in generalPlayScorecards)
                {
                    // Split the holes into front nine and back nine
                    var frontNineHoles = scorecard.Holes.Where(h => h.HoleNumber <= 9).ToList();
                    var backNineHoles = scorecard.Holes.Where(h => h.HoleNumber > 9).ToList();

                    // Create new ScorecardDto for front nine if there are any front nine holes
                    if (frontNineHoles.Any())
                    {
                        var frontNineScorecard = new ScorecardDto
                        {
                            PlayerName = scorecard.PlayerName,
                            ShotsReceived = scorecard.ShotsReceived,
                            HandicapAllowance = scorecard.HandicapAllowance,
                            CompetitionName = scorecard.CompetitionName,
                            TeeColour = scorecard.TeeColour,
                            DatePlayed = scorecard.DatePlayed,
                            TotalStrokes = frontNineHoles.Sum(h => int.TryParse(h.Gross, out var gross) ? gross : 0),
                            TotalStablefordScore = frontNineHoles.Sum(h => h.StablefordScore),
                            Holes = frontNineHoles
                        };
                        frontNineGeneralPlayScorecards.Add(frontNineScorecard);
                    }

                    // Create new ScorecardDto for back nine if there are any back nine holes
                    if (backNineHoles.Any())
                    {
                        var backNineScorecard = new ScorecardDto
                        {
                            PlayerName = scorecard.PlayerName,
                            ShotsReceived = scorecard.ShotsReceived,
                            HandicapAllowance = scorecard.HandicapAllowance,
                            CompetitionName = scorecard.CompetitionName,
                            TeeColour = scorecard.TeeColour,
                            DatePlayed = scorecard.DatePlayed,
                            TotalStrokes = backNineHoles.Sum(h => int.TryParse(h.Gross, out var gross) ? gross : 0),
                            TotalStablefordScore = backNineHoles.Sum(h => h.StablefordScore),
                            Holes = backNineHoles
                        };
                        backNineGeneralPlayScorecards.Add(backNineScorecard);
                    }
                }

                // Filter the best scoring front nine cards
                var bestFNineComp = frontNineCompetitionScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList());
                var bestFNineGeneral = frontNineGeneralPlayScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPGPSCORES).SelectMany(g => g.ToList());

                var bestFrontNineCards = bestFNineComp.Union(bestFNineGeneral).GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList()).ToList();

                holeExclusions.AddRange(
                   frontNineGeneralPlayScorecards
                       .Except(bestFNineGeneral)
                       .Select(gpr => new HoleExclusionReason()
                       {
                           MemberId = juniorMember.MemberId.ToString(),
                           RoundId = gpr.RoundId.ToString(),
                           FrontBack = "Front",
                           DatePlayed = gpr.DatePlayed,
                           ExclusionReason = $"This front nine score was not one of your {TOPGPSCORES} best general play rounds."
                       })
                       .ToList()
                   );

                holeExclusions.AddRange(
                    frontNineCompetitionScorecards.Union(frontNineGeneralPlayScorecards)
                        .Except(bestFrontNineCards)
                        .Select(gpr => new HoleExclusionReason()
                        {
                            MemberId = juniorMember.MemberId.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            FrontBack = "Front",
                            DatePlayed = gpr.DatePlayed,
                            ExclusionReason = $"This front nine score was not one of your {TOPSCORES} best overall."
                        })
                        .ToList()
                    );


                // Filter the bast scoring back nine cards
                var bestBNineComp = backNineCompetitionScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList());
                var bestBNineGeneral = backNineGeneralPlayScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPGPSCORES).SelectMany(g => g.ToList());

                var bestBackNineCards = bestBNineComp.Union(bestBNineGeneral).GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList()).ToList();

                holeExclusions.AddRange(
                  backNineGeneralPlayScorecards
                      .Except(bestBNineGeneral)
                      .Select(gpr => new HoleExclusionReason()
                      {
                          MemberId = juniorMember.MemberId.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          FrontBack = "Back",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This back nine score was not one of your {TOPGPSCORES} best general play rounds."
                      })
                      .ToList()
                  );

                holeExclusions.AddRange(
                    backNineCompetitionScorecards.Union(backNineGeneralPlayScorecards)
                        .Except(bestBackNineCards)
                        .Select(gpr => new HoleExclusionReason()
                        {
                            MemberId = juniorMember.MemberId.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            FrontBack = "Back",
                            DatePlayed = gpr.DatePlayed,
                            ExclusionReason = $"This back nine score was not one of your {TOPSCORES} best overall."
                        })
                        .ToList()
                    );

                #endregion

                #region STEP 6: Using every combination of cards determine the best combination that achieves the highest eclectic score

                // Generate all combinations (up to 5 cards that include only 2 general play cards)
                var allFrontCombinations = GetCombinations(bestFrontNineCards, TOPSCORES).Where(c => c.Where(s => s.IsGeneralPlay).Count() <= TOPGPSCORES);
                var allBackCombinations = GetCombinations(bestBackNineCards, TOPSCORES).Where(c => c.Where(s => s.IsGeneralPlay).Count() <= TOPGPSCORES); 

                // Get best combination
                var bestFrontEclecticScore = int.MinValue;
                var bestbackEclecticScore = int.MinValue;

                List<ScorecardDto> finalBestFrontNineCombination = null;
                List<ScorecardDto> finalBestBackNineCombination = null;

                foreach (var combination in allFrontCombinations)
                {
                    var score = CalculateEclecticScore(combination);
                    if (score > bestFrontEclecticScore)
                    {
                        bestFrontEclecticScore = score;
                        finalBestBackNineCombination = combination;
                    }
                }

                foreach (var combination in allBackCombinations)
                {
                    var score = CalculateEclecticScore(combination);
                    if (score > bestbackEclecticScore)
                    {
                        bestbackEclecticScore = score;
                        finalBestBackNineCombination = combination;
                    }
                }

                holeExclusions.AddRange(
                  bestFrontNineCards
                      .Except(finalBestFrontNineCombination!)
                      .Select(gpr => new HoleExclusionReason()
                      {
                          MemberId = juniorMember.MemberId.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          FrontBack = "Front",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This front nine scorecard did not result in the best eclectic score."
                      })
                      .ToList()
                  );

                holeExclusions.AddRange(
                  bestBackNineCards
                      .Except(finalBestBackNineCombination!)
                      .Select(gpr => new HoleExclusionReason()
                      {
                          MemberId = juniorMember.MemberId.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          FrontBack = "Front",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This back nine scorecard did not result in the best eclectic score."
                      })
                      .ToList()
                  );

                #endregion

                #region STEP 7: Create the final eclectic scorecard

                var finalEclecticScorecard = new EclecticScorecardDto
                {
                    // Assuming you have a unique round ID, use it here for the full 18-hole scorecard
                    PlayerName = finalBestFrontNineCombination.First().PlayerName, // Assuming the player name is the same for all cards
                    TotalStablefordScore = bestFrontEclecticScore + bestbackEclecticScore, // Combine the total scores
                    Holes = new List<ExclecticScorecardHoleDto>()
                };

                // Extract all front nine holes with their score and DatePlayed from the scorecard
                var allFrontHoles = finalBestFrontNineCombination
                    .SelectMany(card => card.Holes.Select(h => new {
                        HoleNumber = h.HoleNumber,  // Include HoleNumber for clarity
                        StablefordScore = h.StablefordScore,
                        DatePlayed = card.DatePlayed, // Use DatePlayed from the scorecard
                        RoundId = card.RoundId       // Use RoundId from the scorecard
                    })).ToList();

                // Extract all back nine holes with their score and DatePlayed from the scorecard
                var allBackHoles = finalBestBackNineCombination
                    .SelectMany(card => card.Holes.Select(h => new {
                        HoleNumber = h.HoleNumber,  // Include HoleNumber for clarity
                        StablefordScore = h.StablefordScore,
                        DatePlayed = card.DatePlayed, // Use DatePlayed from the scorecard
                        RoundId = card.RoundId       // Use RoundId from the scorecard
                    })).ToList();

                // Combine both front and back nine holes
                var allHoles = allFrontHoles.Concat(allBackHoles).ToList();

                var allHolesResult = allHoles
                    .GroupBy(h => h.HoleNumber)
                    .Select(group => new ExclecticScorecardHoleDto
                    {
                        HoleNumber = group.Key,
                        StablefordScore = group.Max(h => h.StablefordScore), // Take the best stableford score for each hole

                        // Select the most recent scorecard for the best score
                        RoundDate = group.Where(h => h.StablefordScore == group.Max(g => g.StablefordScore)) 
                                         .OrderByDescending(h => h.DatePlayed) 
                                         .First().DatePlayed, 

                        RoundId = group.Where(h => h.StablefordScore == group.Max(g => g.StablefordScore)) 
                                       .OrderByDescending(h => h.DatePlayed) 
                                       .First().RoundId, 

                        UncountedScores = group.Where(h => h.StablefordScore != group.Max(g => g.StablefordScore)) // Collect uncounted scores
                                               .Select(uncounted => new EclecticScorecardHoleUncountedScoreDto
                                               {
                                                   RoundDate = uncounted.DatePlayed,
                                                   RoundId = uncounted.RoundId,
                                                   StablefordScore = uncounted.StablefordScore
                                               }).ToList()
                    }).ToList();


                // Add the selected holes to the EclecticScorecardDto
                finalEclecticScorecard.Holes.AddRange(allHolesResult);

                #endregion

                eclecticScorecards.Add(finalEclecticScorecard);
            }
        }

        // Get all combinations of a given size
        private List<List<ScorecardDto>> GetCombinations(List<ScorecardDto> cards, int combinationSize)
        {
            var combinations = new List<List<ScorecardDto>>();
            var totalCards = cards.Count;

            for (int i = 0; i < (1 << totalCards); i++)
            {
                var combination = new List<ScorecardDto>();
                for (int j = 0; j < totalCards; j++)
                {
                    if ((i & (1 << j)) != 0) combination.Add(cards[j]);
                }

                if (combination.Count <= combinationSize)
                {
                    combinations.Add(combination);
                }
            }

            return combinations;
        }

        private int CalculateEclecticScore(List<ScorecardDto> selectedCards)
        {
            var bestScores = new Dictionary<int, int>();

            foreach (var card in selectedCards)
            {
                foreach (var hole in card.Holes)
                {
                    if (!bestScores.ContainsKey(hole.HoleNumber) || hole.StablefordScore > bestScores[hole.HoleNumber])
                    {
                        bestScores[hole.HoleNumber] = hole.StablefordScore;
                    }
                }
            }

            return bestScores.Values.Sum();
        }
    }
}
