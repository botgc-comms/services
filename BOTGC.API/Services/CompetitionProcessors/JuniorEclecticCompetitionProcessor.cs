using BOTGC.API.Dto;
using BOTGC.API.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BOTGC.API.Controllers;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using System.Diagnostics;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services.CompetitionProcessors
{
    public class JuniorEclecticCompetitionProcessor : ICompetitionProcessor
    {
        private readonly AppSettings _settings;
        private readonly IDataService _reportService;
        private readonly ILogger<JuniorEclecticCompetitionProcessor> _logger;
        private IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="JuniorEclecticCompetitionProcessor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public JuniorEclecticCompetitionProcessor(IOptions<AppSettings> settings,
                                                  ILogger<JuniorEclecticCompetitionProcessor> logger,
                                                  IDataService reportService,
                                                  IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task ProcessCompetitionAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var results = await GetCompetitionResultAsync(fromDate, toDate, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await cacheService.SetAsync($"Junior_Eclectic_Results_{fromDate.ToString("yyyyMMdd")}_{toDate.ToString("yyyyMMdd")}", results, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));
            }
        }

        private async Task<EclecticScoretDto> GetEclecticScoreByMember(MemberDto juniorMember, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            const int TOPSCORES = 5;
            const int TOPGPSCORES = 3;

            try
            {
                var roundExclusions = new List<EclecticRoundExclusionReasonDto>();

                #region STEP 2: Get all rounds for each Junior Member

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

                var rounds = await _reportService.GetRoundsByMemberIdAsync(juniorMember.MemberNumber.ToString());

                #endregion

                #region STEP 3: Identify the eligable rounds

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

                // step 3a: Get rounds played in the date range
                var roundsInDateRange = rounds.Where(r => r.DatePlayed >= fromDate && r.DatePlayed <= toDate);

                // step 3b: separate competition rounds from general play rounds
                var competitionRounds = roundsInDateRange.Where(r => r.IsHandicappingRound && !r.IsGeneralPlay);
                var generalPlayRounds = roundsInDateRange.Where(r => r.IsHandicappingRound && r.IsGeneralPlay);

                // step 3c: remove any general play round not played within 6 weeks of a competition
                var validGeneralPlayRounds = generalPlayRounds.Where(gpr => competitionRounds.Any(cr => Math.Abs((gpr.DatePlayed - cr.DatePlayed).TotalDays) <= 42));

                // step 3d: update the collection of exclusion reasons
                roundExclusions.AddRange(
                    generalPlayRounds
                        .Except(validGeneralPlayRounds ?? [])
                        .Select(gpr => new EclecticRoundExclusionReasonDto()
                        {
                            Type = "WholeRound",
                            MemberId = juniorMember.MemberNumber.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            DatePlayed = gpr.DatePlayed,
                            ExclusionReason = $"General play round on {gpr.DatePlayed.ToOrdinalDateString()} was not played within 6 weeks of any competition round."
                        })
                        .ToList()
                    );

                #endregion

                #region STEP 4: Get separate front and back nine scorecards

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

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

                if (validGeneralPlayRounds != null)
                {
                    foreach (var generalPlayRound in validGeneralPlayRounds)
                    {
                        var scorecard = await _reportService.GetScorecardForRoundAsync(generalPlayRound.RoundId.ToString());
                        if (scorecard != null)
                        {
                            generalPlayScorecards.Add(scorecard);
                        }
                    }
                }

                #endregion

                #region STEP 5: Filter the best competition and general play cards for the front and back nine

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

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
                            RoundId = scorecard.RoundId,
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
                            RoundId = scorecard.RoundId,
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
                            RoundId = scorecard.RoundId,
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
                            RoundId = scorecard.RoundId,
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

                var bestFrontNineCards = bestFNineComp.Union(bestFNineGeneral ?? []).GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList()).ToList();

                roundExclusions.AddRange(
                   frontNineGeneralPlayScorecards
                       .Except(bestFNineGeneral)
                       .Select(gpr => new EclecticRoundExclusionReasonDto()
                       {
                           MemberId = juniorMember.MemberNumber.ToString(),
                           RoundId = gpr.RoundId.ToString(),
                           Type = "FrontNine",
                           DatePlayed = gpr.DatePlayed,
                           ExclusionReason = $"This front nine score was not one of your {TOPGPSCORES} best general play rounds."
                       })
                       .ToList()
                   );

                roundExclusions.AddRange(
                    frontNineCompetitionScorecards.Union(frontNineGeneralPlayScorecards ?? [])
                        .Except(bestFrontNineCards)
                        .Select(gpr => new EclecticRoundExclusionReasonDto()
                        {
                            MemberId = juniorMember.MemberNumber.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            Type = "FrontNine",
                            DatePlayed = gpr.DatePlayed,
                            ExclusionReason = $"This front nine score was not one of your {TOPSCORES} best overall."
                        })
                        .ToList()
                    );


                // Filter the bast scoring back nine cards
                var bestBNineComp = backNineCompetitionScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList());
                var bestBNineGeneral = backNineGeneralPlayScorecards.GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPGPSCORES).SelectMany(g => g.ToList());

                var bestBackNineCards = bestBNineComp.Union(bestBNineGeneral ?? []).GroupBy(s => s.TotalStablefordScore).OrderBy(g => g.Key).Take(TOPSCORES).SelectMany(g => g.ToList()).ToList();

                roundExclusions.AddRange(
                  backNineGeneralPlayScorecards
                      .Except(bestBNineGeneral)
                      .Select(gpr => new EclecticRoundExclusionReasonDto()
                      {
                          MemberId = juniorMember.MemberNumber.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          Type = "BackNine",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This back nine score was not one of your {TOPGPSCORES} best general play rounds."
                      })
                      .ToList()
                  );

                roundExclusions.AddRange(
                    backNineCompetitionScorecards.Union(backNineGeneralPlayScorecards ?? [])
                        .Except(bestBackNineCards)
                        .Select(gpr => new EclecticRoundExclusionReasonDto()
                        {
                            MemberId = juniorMember.MemberNumber.ToString(),
                            RoundId = gpr.RoundId.ToString(),
                            Type = "BackNine",
                            DatePlayed = gpr.DatePlayed,
                            ExclusionReason = $"This back nine score was not one of your {TOPSCORES} best overall."
                        })
                        .ToList()
                    );

                #endregion

                #region STEP 6: Using every combination of cards determine the best combination that achieves the highest eclectic score

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

                // Generate all combinations (up to 5 cards that include only 2 general play cards)
                var allFrontCombinations = GetCombinations(bestFrontNineCards, TOPSCORES).Where(c => c.Where(s => s.IsGeneralPlay).Count() <= TOPGPSCORES);
                var allBackCombinations = GetCombinations(bestBackNineCards, TOPSCORES).Where(c => c.Where(s => s.IsGeneralPlay).Count() <= TOPGPSCORES);

                // Get best combination
                var frontEclecticScores = allFrontCombinations
                    .Select(fc => new
                    {
                        scorecards = fc,
                        eclecticScore = CalculateEclecticScore(fc),
                        numScorecards = fc.Count,
                        latestDate = fc.Max(sc => sc.DatePlayed),
                        earliestDate = fc.Min(sc => sc.DatePlayed)
                    })
                    .OrderByDescending(e => e.eclecticScore)
                    .ThenByDescending(e => e.numScorecards)
                    .ThenByDescending(e => e.latestDate)
                    .ToList();

                var backEclecticScores = allBackCombinations
                    .Select(fc => new
                    {
                        scorecards = fc,
                        eclecticScore = CalculateEclecticScore(fc),
                        numScorecards = fc.Count,
                        latestDate = fc.Max(sc => sc.DatePlayed),
                        earliestDate = fc.Min(sc => sc.DatePlayed)
                    })
                    .OrderByDescending(e => e.eclecticScore)
                    .ThenByDescending(e => e.numScorecards)
                    .ThenByDescending(e => e.latestDate)
                    .ThenByDescending(e => e.earliestDate)
                    .ToList();

                List<ScorecardDto>? finalBestFrontNineCombination = frontEclecticScores.Select(f => f.scorecards).FirstOrDefault();
                List<ScorecardDto>? finalBestBackNineCombination = backEclecticScores.Select(f => f.scorecards).FirstOrDefault();

                var bestFrontEclecticScore = frontEclecticScores.Select(f => f.eclecticScore).FirstOrDefault();
                var bestbackEclecticScore = backEclecticScores.Select(f => f.eclecticScore).FirstOrDefault();

                roundExclusions.AddRange(
                  bestFrontNineCards
                      .Except(finalBestFrontNineCombination ?? [])
                      .Select(gpr => new EclecticRoundExclusionReasonDto()
                      {
                          MemberId = juniorMember.MemberNumber.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          Type = "FrontNine",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This front nine scorecard did not result in the best eclectic score."
                      })
                      .ToList()
                  );

                roundExclusions.AddRange(
                  bestBackNineCards
                      .Except(finalBestBackNineCombination ?? [])
                      .Select(gpr => new EclecticRoundExclusionReasonDto()
                      {
                          MemberId = juniorMember.MemberNumber.ToString(),
                          RoundId = gpr.RoundId.ToString(),
                          Type = "BackNine",
                          DatePlayed = gpr.DatePlayed,
                          ExclusionReason = $"This back nine scorecard did not result in the best eclectic score."
                      })
                      .ToList()
                  );

                #endregion

                #region STEP 7: Create the final eclectic scorecard

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, stopping...");
                    return null;
                }

                if (finalBestFrontNineCombination != null && finalBestFrontNineCombination!.Any() &&
                    finalBestBackNineCombination != null && finalBestBackNineCombination!.Any())
                {

                    var finalEclecticScorecard = new EclecticScorecardDto
                    {
                        // Assuming you have a unique round ID, use it here for the full 18-hole scorecard
                        PlayerName = finalBestFrontNineCombination!.First().PlayerName, // Assuming the player name is the same for all cards
                        TotalStablefordScore = bestFrontEclecticScore + bestbackEclecticScore, // Combine the total scores
                        Holes = new List<ExclecticScorecardHoleDto>()
                    };

                    // Extract all front nine holes with their score and DatePlayed from the scorecard
                    var allFrontHoles = finalBestFrontNineCombination?
                        .SelectMany(card => card.Holes.Select(h => new
                        {
                            HoleNumber = h.HoleNumber,  
                            StablefordScore = h.StablefordScore,
                            DatePlayed = card.DatePlayed, 
                            RoundId = card.RoundId       
                        })).ToList();

                    // Extract all back nine holes with their score and DatePlayed from the scorecard
                    var allBackHoles = finalBestBackNineCombination?
                        .SelectMany(card => card.Holes.Select(h => new
                        {
                            HoleNumber = h.HoleNumber,  
                            StablefordScore = h.StablefordScore,
                            DatePlayed = card.DatePlayed, 
                            RoundId = card.RoundId       
                        })).ToList();

                    // Combine both front and back nine holes
                    var allHoles = (allFrontHoles ?? []).Concat(allBackHoles ?? []).ToList();

                    var allHolesResult = allHoles
                        .GroupBy(h => h.HoleNumber)
                        .Select(group =>
                        {
                            var bestScore = group.Max(h => h.StablefordScore);

                            var bestScorecard = group
                                .Where(h => h.StablefordScore == bestScore)
                                .OrderByDescending(h => h.DatePlayed)
                                .First();

                            return new ExclecticScorecardHoleDto
                            {
                                HoleNumber = group.Key,
                                StablefordScore = bestScore,
                                RoundDate = bestScorecard.DatePlayed,
                                RoundId = bestScorecard.RoundId,
                                UncountedScores = group
                                    .Where(h => h.RoundId != bestScorecard.RoundId)
                                    .Select(uncounted => new EclecticScorecardHoleUncountedScoreDto
                                    {
                                        RoundDate = uncounted.DatePlayed,
                                        RoundId = uncounted.RoundId,
                                        StablefordScore = uncounted.StablefordScore
                                    })
                                    .ToList()
                            };
                        }).ToList();


                    // Add the selected holes to the EclecticScorecardDto
                    finalEclecticScorecard.Holes.AddRange(allHolesResult);

                    #endregion

                    return new EclecticScoretDto
                    {
                        Scorecard = finalEclecticScorecard,
                        ExcludedRounds = roundExclusions
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to calculate competition results for member {juniorMember.MemberNumber}.");
            }

            return null;
        }

        public async Task<EclecticCompetitionResultsDto> GetCompetitionResultAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            var eclecticResults = new List<EclecticScoretDto>();

            #region STEP 1: Get all Junior Members
            var juniorMembers = await _reportService.GetJuniorMembersAsync();
            #endregion

            var tasks = juniorMembers
                .Select(async juniorMember =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return null;

                    return await GetEclecticScoreByMember(juniorMember, fromDate, toDate, cancellationToken);
                });

            var scores = await Task.WhenAll(tasks); // Run tasks in parallel

            eclecticResults.AddRange(scores.Where(score => score != null)); // Filter out null results

            return new EclecticCompetitionResultsDto
            {
                Scores = eclecticResults.OrderByDescending(s => s.Scorecard.TotalStablefordScore).ToList()
            };
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

                if (combination.Count >= 1 && combination.Count <= combinationSize)
                {
                    combinations.Add(combination);
                }
            }

            return combinations.OrderByDescending(c => c.Count).ToList();
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
