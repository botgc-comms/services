using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using System.Globalization;
using System.Net;
using System.Text;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class UpdateCompetitionResultsPageHandler : QueryHandlerBase<UpdateCompetitionResultsPageCommand, string>
{
    private readonly ILogger<UpdateCompetitionResultsPageHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ICmsEncodingHelper _cmsEncodingHelper;
    private readonly ICompetitionWinnersUrlProvider _winnersUrlProvider;

    public UpdateCompetitionResultsPageHandler(ILogger<UpdateCompetitionResultsPageHandler> logger,
                                               IMediator mediator,
                                               ICmsEncodingHelper cmsEncodingHelper,
                                               ICompetitionWinnersUrlProvider winnersUrlProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _cmsEncodingHelper = cmsEncodingHelper ?? throw new ArgumentNullException(nameof(cmsEncodingHelper));
        _winnersUrlProvider = winnersUrlProvider ?? throw new ArgumentNullException(nameof(winnersUrlProvider));
    }

    public override async Task<string> Handle(UpdateCompetitionResultsPageCommand request, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var years = new[] { currentYear, currentYear - 1, currentYear - 2 };

        var yearlyTasks = years.Select(y => _mediator.Send(new GetYearlyWinningsSummaryQuery(y), cancellationToken)).ToArray();
        var manualTasks = years.Select(y => _mediator.Send(new GetManualCompetitionResultsQuery(y), cancellationToken)).ToArray();

        await Task.WhenAll(yearlyTasks.Concat<Task>(manualTasks)).ConfigureAwait(false);

        var sb = new StringBuilder();

        foreach (var year in years)
        {
            var yearly = yearlyTasks[Array.IndexOf(years, year)].Result;
            var manual = manualTasks[Array.IndexOf(years, year)].Result;

            var rows = Combine(yearly, manual);
            if (rows.Count == 0) continue;

            sb.AppendLine($@"<h2>Competition results for {year}</h2>");
            sb.AppendLine(@"<table>");
            sb.AppendLine(@"  <thead>");
            sb.AppendLine(@"    <tr>");
            sb.AppendLine(@"      <th>Date</th>");
            sb.AppendLine(@"      <th>Competition</th>");
            sb.AppendLine(@"      <th>Entrants &amp; Prize Pot</th>");
            sb.AppendLine(@"      <th>Winners</th>");
            sb.AppendLine(@"    </tr>");
            sb.AppendLine(@"  </thead>");
            sb.AppendLine(@"  <tbody>");

            foreach (var r in rows.OrderBy(r => r.Date).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                var dateText = r.Date.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(r.Date, DateTimeKind.Utc).ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"))
                    : r.Date.ToUniversalTime().ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

                var name = WebUtility.HtmlEncode(r.Name);
                var entrantsText = r.Entrants.HasValue ? r.Entrants.Value.ToString(CultureInfo.InvariantCulture) : "—";
                var prizeText = $"&pound;{FormatMoney(r.PrizePot)}";
                var winnersCell = !string.IsNullOrWhiteSpace(r.Url)
                    ? $@"<a href=""{WebUtility.HtmlEncode(r.Url)}"">View winners</a>"
                    : "—";

                sb.AppendLine(@"    <tr>");
                sb.AppendLine($@"      <td>{dateText}</td>");
                sb.AppendLine($@"      <td>{name}</td>");
                sb.AppendLine($@"      <td>Entrants: {entrantsText} &nbsp;|&nbsp; Prize Pot: {prizeText}</td>");
                sb.AppendLine($@"      <td>{winnersCell}</td>");
                sb.AppendLine(@"    </tr>");
            }

            sb.AppendLine(@"  </tbody>");
            sb.AppendLine(@"</table>");
            sb.AppendLine();
        }

        var html = sb.ToString().Trim();
        var encoded = _cmsEncodingHelper.EncodePageContent(html);
        return encoded;
    }

    private static string FormatMoney(decimal amount)
    {
        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        return rounded % 1m == 0m
            ? rounded.ToString("0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private sealed class CombinedRow
    {
        public DateTime Date { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? Entrants { get; init; }
        public decimal PrizePot { get; init; }
        public string? Url { get; init; }
    }

    private List<CombinedRow> Combine(YearlyWinningsSummaryDto yearly, IReadOnlyCollection<ManualCompetitionResultDto> manual)
    {
        var map = new Dictionary<string, CombinedRow>(StringComparer.OrdinalIgnoreCase);

        if (yearly != null)
        {
            foreach (var c in yearly.Competitions)
            {
                var url = _winnersUrlProvider.TryGetUrl(c);
                var key = url ?? $"{c.CompetitionDate:yyyy-MM-dd}|{c.CompetitionName}";
                if (!map.ContainsKey(key))
                {
                    map[key] = new CombinedRow
                    {
                        Date = c.CompetitionDate,
                        Name = c.CompetitionName,
                        Entrants = null,
                        PrizePot = c.PrizePot,
                        Url = url
                    };
                }
                else
                {
                    var existing = map[key];
                    map[key] = new CombinedRow
                    {
                        Date = ChooseDate(existing.Date, c.CompetitionDate),
                        Name = string.IsNullOrWhiteSpace(existing.Name) ? c.CompetitionName : existing.Name,
                        Entrants = existing.Entrants,
                        PrizePot = existing.PrizePot == 0m ? c.PrizePot : existing.PrizePot,
                        Url = existing.Url ?? url
                    };
                }
            }
        }

        if (manual != null)
        {
            foreach (var m in manual)
            {
                var key = !string.IsNullOrWhiteSpace(m.Url) ? m.Url! : $"{m.Date:yyyy-MM-dd}|{m.CompetitionName}";
                if (!map.ContainsKey(key))
                {
                    map[key] = new CombinedRow
                    {
                        Date = m.Date,
                        Name = m.CompetitionName,
                        Entrants = m.Entrants,
                        PrizePot = m.PrizePot,
                        Url = m.Url
                    };
                }
                else
                {
                    var existing = map[key];
                    map[key] = new CombinedRow
                    {
                        Date = ChooseDate(existing.Date, m.Date),
                        Name = string.IsNullOrWhiteSpace(existing.Name) ? m.CompetitionName : existing.Name,
                        Entrants = existing.Entrants ?? m.Entrants,
                        PrizePot = existing.PrizePot == 0m ? m.PrizePot : existing.PrizePot,
                        Url = existing.Url ?? m.Url
                    };
                }
            }
        }

        return map.Values.ToList();
    }

    private static DateTime ChooseDate(DateTime a, DateTime b)
    {
        if (a == default) return b;
        if (b == default) return a;
        return a <= b ? a : b;
    }
}