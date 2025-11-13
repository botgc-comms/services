using System.Text.RegularExpressions;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetCompetitionsHandler(IOptions<AppSettings> settings,
                                    IMediator mediator,
                                    ILogger<GetCompetitionsHandler> logger,
                                    IDataProvider dataProvider,
                                    IReportParser<CompetitionDto> reportParser) : QueryHandlerBase<GetCompetitionsQuery, List<CompetitionDto>>
{
    private const string __CACHE_ACTIVECOMPETITIONS = "Active_Competitions";
    private const string __CACHE_FUTURECOMPETITIONS = "Future_Competitions";
    private const string __CACHE_FINALISEDCOMPETITIONS = "Finalised_Competitions";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetCompetitionsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<CompetitionDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<CompetitionDto>> Handle(GetCompetitionsQuery request, CancellationToken cancellationToken)
    {
        var allCompetitions = new List<CompetitionDto>();

        if (request.Active)
        {
            var activeCompetitionsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.ActiveCompetitionsUrl}";
            var activeCompetitions = await _dataProvider.GetData<CompetitionDto>(activeCompetitionsUrl, _reportParser, __CACHE_ACTIVECOMPETITIONS, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));
            if (activeCompetitions != null) allCompetitions.AddRange(activeCompetitions);
        }

        if (request.Future)
        {
            var upcomingCompetitionsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpcomingCompetitionsUrl}";
            var upcomingCompetitions = await _dataProvider.GetData<CompetitionDto>(upcomingCompetitionsUrl, _reportParser, __CACHE_FUTURECOMPETITIONS, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));
            if (upcomingCompetitions != null) allCompetitions.AddRange(upcomingCompetitions);
        }

        if (request.Finalised)
        {
            var finalisedCompetitionsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.FinalisedCompetitionsUrl}";
            var finalisedCompetitions = await _dataProvider.GetData<CompetitionDto>(finalisedCompetitionsUrl, _reportParser, __CACHE_FINALISEDCOMPETITIONS, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));
            if (finalisedCompetitions != null) allCompetitions.AddRange(finalisedCompetitions);
        }

        allCompetitions = allCompetitions
            .Where(c => c.Id.HasValue)
            .GroupBy(c => c.Id!.Value)
            .Select(g => g.First())
            .ToList();

        foreach (var comp in allCompetitions.Where(c => !c.Date.HasValue))
        {
            if (comp.Id.HasValue && comp.Id.Value > 0)
            {
                var compSettingsQuery = new GetCompetitionSettingsByCompetitionIdQuery { CompetitionId = comp.Id.Value.ToString() };
                _logger.LogInformation("Fetching settings for competition {CompetitionId} to fix missing date.", comp.Id.Value);
                var settings = await _mediator.Send(compSettingsQuery, cancellationToken);
                if (settings != null)
                {
                    comp.Date = settings.Date;
                    comp.MultiPartCompetition = settings.MultiPartCompetition;
                }
                else
                {
                    _logger.LogWarning("No settings found for competition {CompetitionId}.", comp.Id);
                }
            }
        }

        IEnumerable<CompetitionDto> resultQuery = allCompetitions.Where(c => c.Date.HasValue);

        if (!request.Finalised)
        {
            resultQuery = resultQuery.Where(c => c.Date!.Value >= DateTime.Today.Date);
        }

        var result = resultQuery
            .OrderBy(c => c.Date)
            .ToList();

        InferMultiDayBySeries(result);

        _logger.LogInformation("Retrieved {Count} competitions. Flags => Active: {Active}, Future: {Future}, Finalised: {Finalised}.", result.Count, request.Active, request.Future, request.Finalised);

        return result;
    }

    private sealed record SeriesKey(string Code, int Number);

    private static SeriesKey? TryExtractSeriesKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var m = Regex.Match(name, @"(?i).*?(?<code>Medal|Stab)[a-z]*\D+(?<num>\d+)");
        if (!m.Success) return null;

        var raw = m.Groups["code"].Value;
        var code = raw.StartsWith("stab", StringComparison.OrdinalIgnoreCase) ? "STAB" : "MEDAL";
        var number = int.Parse(m.Groups["num"].Value);

        return new SeriesKey(code, number);
    }

    private void InferMultiDayBySeries(List<CompetitionDto> comps)
    {
        var parents = comps.Where(c => c.IsMultidayParent && c.Id.HasValue).ToList();
        var children = comps.Where(c => c.IsAlternateDay && c.Id.HasValue && c.Date.HasValue && !c.IsMultidayParent).ToList();

        foreach (var p in parents)
        {
            var pk = TryExtractSeriesKey(p.Name);
            if (pk == null) continue;

            var parentCode = pk.Code;
            var parentNum = pk.Number;

            var start = p.Date.HasValue ? p.Date.Value.AddDays(-3) : DateTime.MinValue;
            var end = p.Date.HasValue ? p.Date.Value.AddDays(3) : DateTime.MaxValue;

            var matches = children
                .Where(c =>
                {
                    var ck = TryExtractSeriesKey(c.Name);
                    if (ck == null) return false;
                    if (!string.Equals(ck.Code, parentCode, StringComparison.OrdinalIgnoreCase)) return false;
                    if (ck.Number != parentNum) return false;
                    return c.Date >= start && c.Date <= end;
                })
                .OrderBy(c => c.Date)
                .ToList();

            if (matches.Count < 2) continue;

            var map = matches
                .Select((c, i) => new { c.Id, i })
                .Where(x => x.Id.HasValue)
                .ToDictionary(x => x.Id!.Value.ToString(), x => x.i + 1);

            if (map.Count > 0)
            {
                p.MultiPartCompetition = map;
                p.MultiPartCompetition.Remove(p.Id!.Value.ToString());
                _logger.LogInformation("Inferred multiday parent {ParentId} ({Code}{Number}) with {Count} children.", p.Id!.Value, parentCode, parentNum, map.Count);
            }
        }
    }

}
