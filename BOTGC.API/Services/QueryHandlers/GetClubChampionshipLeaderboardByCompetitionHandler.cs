using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetClubChampionshipLeaderboardByCompetition(IOptions<AppSettings> settings,
                                                             IMediator mediator, 
                                                             ILogger<GetClubChampionshipLeaderboardByCompetition> logger,
                                                             IDataProvider dataProvider,
                                                             IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto> reportParser) : QueryHandlerBase<GetClubChampionshipLeaderboardByCompetitionQuery, ClubChampionshipLeaderBoardDto?>
    {
        private const string __CACHE_KEY = "Leaderboard_Settings:{compid}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetClubChampionshipLeaderboardByCompetition> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<ClubChampionshipLeaderBoardDto?> Handle(GetClubChampionshipLeaderboardByCompetitionQuery request, CancellationToken cancellationToken)
        {
            var competitionId = request.CompetitionId ?? throw new ArgumentNullException(nameof(request.CompetitionId));
            if (string.IsNullOrWhiteSpace(competitionId))
            {
                _logger.LogWarning("Competition ID is null or empty.");
                return null;
            }

            var competitionSettingsQuery = new GetCompetitionSettingsByCompetitionIdQuery
            {
                CompetitionId = competitionId
            };

            var competitionSettings = await _mediator.Send(competitionSettingsQuery, cancellationToken);    

            if (competitionSettings.MultiPartCompetition != null && competitionSettings.MultiPartCompetition.Count == 2)
            {
                var grossOrNett = "2"; // Club champs is a gross competition

                var r1Id = competitionSettings.MultiPartCompetition.FirstOrDefault(
                    r => Regex.IsMatch(r.Key, "R(?:ound)?\\s*1", RegexOptions.IgnoreCase),
                    competitionSettings.MultiPartCompetition.ElementAt(0));

                var r1Url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
                    .Replace("{compid}", r1Id.Value.ToString()).Replace("{grossOrNett}", grossOrNett);

                var r1 = await _dataProvider.GetData<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>(
                    r1Url, _reportParser, competitionSettings,
                    __CACHE_KEY.Replace("{compid}", competitionId) + "_R1",
                    TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

                var r2Id = competitionSettings.MultiPartCompetition.FirstOrDefault(
                    r => Regex.IsMatch(r.Key, "R(?:ound)?\\s*2", RegexOptions.IgnoreCase),
                    competitionSettings.MultiPartCompetition.ElementAt(1));

                var r2Url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
                    .Replace("{compid}", r2Id.Value.ToString()).Replace("{grossOrNett}", grossOrNett);

                var r2 = await _dataProvider.GetData<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>(
                    r2Url, _reportParser, competitionSettings,
                    __CACHE_KEY.Replace("{compid}", competitionId) + "_R2",
                    TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

                var combined = new List<ChampionshipLeaderboardPlayerDto>();

                if (r1 != null && r1.Any())
                {
                    var allPlayers = r1.Select(p => p.PlayerName)
                        .Union(r2?.Select(p => p.PlayerName) ?? Enumerable.Empty<string>())
                        .Distinct();

                    foreach (var playerName in allPlayers)
                    {
                        var p1 = r1.FirstOrDefault(x => x.PlayerName == playerName);
                        var p2 = r2?.FirstOrDefault(x => x.PlayerName == playerName);

                        var toPar1 = ParseToPar(p1?.Par);
                        var toPar2 = ParseToPar(p2?.Par);
                        var totalPar = toPar1 + toPar2;
                        var totalParStr = totalPar == 0 ? "LEVEL" : (totalPar > 0 ? $"+{totalPar}" : totalPar.ToString());

                        var thru1 = ParseThru(p1?.Thru);
                        var thru2 = ParseThru(p2?.Thru);
                        var combinedThru = (thru1 + thru2).ToString();

                        var combinedPlayer = new ChampionshipLeaderboardPlayerDto
                        {
                            PlayerName = playerName,
                            PlayerId = p1?.PlayerId ?? p2?.PlayerId,
                            Par = totalParStr,
                            R1 = p1?.Score,
                            R2 = p2?.Score,
                            Countback = p2?.Countback ?? p1?.Countback,
                            Thru = combinedThru,
                            Score = ((TryParseInt(p1?.Score) + TryParseInt(p2?.Score)).ToString()),
                            Position = null
                        };

                        combined.Add(combinedPlayer);
                    }

                    bool IsDisqualifiedOrNR(ChampionshipLeaderboardPlayerDto player)
                    {
                        return string.Equals(player.R1, "NR", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(player.R2, "NR", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(player.R1, "DQ", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(player.R2, "DQ", StringComparison.OrdinalIgnoreCase);
                    }

                    combined = combined
                        .OrderBy(x => IsDisqualifiedOrNR(x) ? 1 : 0)
                        .ThenBy(x => ParseToPar(x.Par))
                        .ThenBy(x => TryParseThru(x.Thru))
                        .ThenBy(x => int.TryParse(x.R2, out var r2Score) ? r2Score : (int.TryParse(x.R1, out var r1Score) ? r1Score : int.MaxValue))
                        .ThenBy(x => ParseCountback(x.Countback).Back9)
                        .ThenBy(x => ParseCountback(x.Countback).Back6)
                        .ThenBy(x => ParseCountback(x.Countback).Back3)
                        .ThenBy(x => ParseCountback(x.Countback).Back1)
                        .ToList();

                    for (int i = 0; i < combined.Count; i++)
                    {
                        combined[i].Position = i + 1;
                    }

                    return new ClubChampionshipLeaderBoardDto
                    {
                        CompetitionDetails = competitionSettings,
                        Round1 = r1,
                        Round2 = r2,
                        Total = combined
                    };
                }

                int ParseToPar(string par)
                {
                    if (string.IsNullOrWhiteSpace(par)) return 0;
                    par = par.Trim().ToUpper();
                    if (par == "LEVEL") return 0;
                    if (par.StartsWith("+")) return int.TryParse(par.Substring(1), out var n) ? n : 0;
                    if (par.StartsWith("-")) return int.TryParse(par, out var n) ? n : 0;
                    return int.TryParse(par, out var x) ? x : 0;
                }

                int ParseThru(string thru)
                {
                    if (string.IsNullOrWhiteSpace(thru)) return 0;
                    if (int.TryParse(thru, out var n)) return n;
                    return 0;
                }

                int TryParseInt(string? val)
                {
                    if (int.TryParse(val, out var n)) return n;
                    return 0;
                }

                int TryParseThru(string? thru)
                {
                    if (int.TryParse(thru, out var holes)) return holes;
                    return 18;
                }

                (int Back9, int Back6, int Back3, int Back1) ParseCountback(string countback)
                {
                    var regex = new Regex(@"Back 9 - (?<b9>[\d.]+), Back 6 - (?<b6>[\d.]+), Back 3 - (?<b3>[\d.]+), Back 1 - (?<b1>[\d.]+)");
                    var match = regex.Match(countback ?? "");
                    if (!match.Success)
                        return (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
                    return (
                        (int)double.Parse(match.Groups["b9"].Value),
                        (int)double.Parse(match.Groups["b6"].Value),
                        (int)double.Parse(match.Groups["b3"].Value),
                        (int)double.Parse(match.Groups["b1"].Value)
                    );
                }
            }

            return new ClubChampionshipLeaderBoardDto
            {
                CompetitionDetails = competitionSettings
            };
        }
    }
}
