using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public sealed class UpdateCompetitionResultsPageHandler(
            IOptions<AppSettings> settings,
            ILogger<UpdateCompetitionResultsPageHandler> logger,
            IMediator mediator,
            IBlobStorageService blobStorage)
        : QueryHandlerBase<UpdateCompetitionResultsPageCommand, bool>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<UpdateCompetitionResultsPageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IBlobStorageService _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));

        public override async Task<bool> Handle(UpdateCompetitionResultsPageCommand request, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.UtcNow.Year;
            var years = new[] { currentYear, currentYear - 1, currentYear - 2 };

            var yearlyTasks = years
                .Select(y => _mediator.Send(new GetYearlyWinningsSummaryQuery(y), cancellationToken))
                .ToArray();

            var manualTasks = years
                .Select(y => _mediator.Send(new GetManualCompetitionResultsQuery(y), cancellationToken))
                .ToArray();

            await Task.WhenAll(yearlyTasks.Concat<Task>(manualTasks)).ConfigureAwait(false);

            var sb = new StringBuilder();

            for (var i = 0; i < years.Length; i++)
            {
                var year = years[i];
                var yearly = yearlyTasks[i].Result;
                var manual = manualTasks[i].Result;

                var rows = await CombineAsync(yearly, manual, cancellationToken);

                if (rows.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($@"<h2 style=""margin:30px 0 10px 0;font-size:28px;text-align:center;"">Competition results for {year}</h2>");
                sb.AppendLine(@"<table class=""comp-results"" style=""width:90%;margin:0 auto;border-collapse:collapse;font-size:14px;"">");
                sb.AppendLine(@"  <thead>");
                sb.AppendLine(@"    <tr>");
                sb.AppendLine(@"      <th style=""text-align:left;padding:8px 10px;vertical-align:top;border-bottom:1px solid #cccccc;font-weight:bold;"">Date</th>");
                sb.AppendLine(@"      <th style=""text-align:left;padding:8px 10px;vertical-align:top;border-bottom:1px solid #cccccc;font-weight:bold;width:33%;"">Competition</th>");
                sb.AppendLine(@"      <th style=""text-align:left;padding:8px 10px;vertical-align:top;border-bottom:1px solid #cccccc;font-weight:bold;"">Entrants &amp; Prize Pot</th>");
                sb.AppendLine(@"      <th style=""text-align:left;padding:8px 10px;vertical-align:top;border-bottom:1px solid #cccccc;font-weight:bold;"">Winners</th>");
                sb.AppendLine(@"    </tr>");
                sb.AppendLine(@"  </thead>");
                sb.AppendLine(@"  <tbody>");

                foreach (var r in rows
                             .OrderByDescending(r => r.IsGenericDate)
                             .ThenByDescending(r => r.ParsedDate ?? DateTime.MinValue)
                             .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var dateText = GetDateText(r);
                    var name = WebUtility.HtmlEncode(r.Name);

                    var entrantsText = r.Entrants.HasValue
                        ? r.Entrants.Value.ToString(CultureInfo.InvariantCulture)
                        : "";

                    var prizeText = r.PrizePot > 0
                        ? $"&pound;{FormatMoney(r.PrizePot)}"
                        : "";

                    var entrantsPrizeText =  (entrantsText + prizeText != "") ?  $@"{entrantsText} Entrants, Prize Pot {prizeText}" : "";

                    var winnersCell = !string.IsNullOrWhiteSpace(r.Url)
                        ? $@"<a href=""{WebUtility.HtmlEncode(r.Url)}""
                               style=""cursor:pointer;color:#007fa3;text-decoration:none;""
                               target=""_Blank"" 
                               onmouseover=""this.style.textDecoration='underline';""
                               onmouseout=""this.style.textDecoration='none';"">View winners</a>"
                        : "—";

                    sb.AppendLine(@"    <tr>");
                    sb.AppendLine($@"      <td style=""text-align:left;padding:6px 10px;vertical-align:top;border-bottom:1px solid #e5e5e5;"">{dateText}</td>");
                    sb.AppendLine($@"      <td style=""text-align:left;padding:6px 10px;vertical-align:top;border-bottom:1px solid #e5e5e5;width:33%;word-wrap:break-word;white-space:normal;"">{name}</td>");
                    sb.AppendLine($@"      <td style=""text-align:left;padding:6px 10px;vertical-align:top;border-bottom:1px solid #e5e5e5;"">{entrantsPrizeText}</td>");
                    sb.AppendLine($@"      <td style=""text-align:left;padding:6px 10px;vertical-align:top;border-bottom:1px solid #e5e5e5;"">{winnersCell}</td>");
                    sb.AppendLine(@"    </tr>");
                }

                sb.AppendLine(@"  </tbody>");
                sb.AppendLine(@"</table>");
                sb.AppendLine();
            }

            var table = sb.ToString().Trim();

            var page = new StringBuilder();

            page.AppendLine(@"<style>
              .comp-results { border-bottom: 2px solid var(--headingcolor) !important; }

              /* Mobile tweaks */
              @media (max-width: 640px){
                .comp-results{ table-layout: fixed; width:100%; }
                .comp-results th, .comp-results td{ white-space: normal; word-break: break-word; }

                /* Hide Entrants & Prize Pot column on mobile */
                .comp-results th:nth-child(3),
                .comp-results td:nth-child(3){ display:none; }

                /* Fix narrow widths for Date (col 1) and Winners (col 4) so they wrap */
                .comp-results th:nth-child(1), .comp-results td:nth-child(1){ width:64px; padding:0px 3px !important; }
                .comp-results th:nth-child(4), .comp-results td:nth-child(4){ width:64px; padding:0px 3px !important; }

                /* Let the Competition name (col 2) take the remaining space */
                .comp-results th:nth-child(2), .comp-results td:nth-child(2){ width:auto !important; padding:0px 3px !important; }

                /* Ensure the winners link can wrap to two lines if needed */
                .comp-results td:nth-child(4) a{ display:inline-block; padding:0px 3px !important;}
              }
            </style>");

            page.AppendLine(@"<div class=""inner-page-wrapper"">");
            //page.AppendLine(@"  <div class=""inner-slideshow"">");
            //page.AppendLine(@"    <div class=""wysiwyg-editable"">{NEWSLIDESHOW=New Inner Slideshow}</div>");
            //page.AppendLine(@"  </div>");
            page.AppendLine(@"  <div class=""inner-main"">");
            page.AppendLine(@"    <div class=""page-space"">");
            page.AppendLine(@"      <div class=""inner-nav"">");
            page.AppendLine(@"        <div class=""wysiwyg-editable"">{MENU,section=Hospitality and Hire}</div>");
            page.AppendLine(@"      </div>");
            page.AppendLine(@"      <div class=""inner-full"">");
            page.AppendLine(@"        <div class=""wysiwyg-editable"">");
            page.AppendLine(@"          <h1>Competition Results</h1>");
            page.AppendLine(@"          <p>View recent competition outcomes and prize winners.</p>");
            page.AppendLine(table);
            page.AppendLine(@"        </div>");
            page.AppendLine(@"      </div>");
            //page.AppendLine(@"      <div class=""inner-carousel-block"">");
            //page.AppendLine(@"        <div class=""wysiwyg-editable"">{INCLUDE=Inner Carousel}</div>");
            //page.AppendLine(@"      </div>");
            page.AppendLine(@"    </div>");
            page.AppendLine(@"  </div>");
            page.AppendLine(@"</div>");
            //page.AppendLine(@"<div class=""page-footer"">");
            //page.AppendLine(@"  <div class=""wysiwyg-editable"">{INCLUDE=Public Footer}</div>");
            //page.AppendLine(@"</div>");

            var html = page.ToString();

            var updateRequest = new UpdateCmsPageCommand(_settings.IG.CompetitionResultsPageId, html);
            var result = await _mediator.Send(updateRequest, cancellationToken);

            return result;
        }

        private static string FormatMoney(decimal amount)
        {
            var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            return rounded % 1m == 0m
                ? rounded.ToString("0", CultureInfo.InvariantCulture)
                : rounded.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string GetDateText(CombinedRow row)
        {
            if (row.IsGenericDate && !string.IsNullOrWhiteSpace(row.RawDate))
            {
                return WebUtility.HtmlEncode(row.RawDate);
            }

            if (row.ParsedDate.HasValue)
            {
                return row.ParsedDate.Value.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
            }

            return "—";
        }

        private static DateTime? TryParseFullDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            var formats = new[]
            {
                "dd/MM/yyyy",
                "d/M/yyyy",
                "dd-MM-yyyy",
                "d-M-yyyy",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(
                    trimmed,
                    formats,
                    CultureInfo.GetCultureInfo("en-GB"),
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static DateTime? ChooseBestParsedDate(DateTime? a, DateTime? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return a <= b ? a : b;
        }

        private sealed class CombinedRow
        {
            public string RawDate { get; set; } = string.Empty;
            public DateTime? ParsedDate { get; set; }
            public bool IsGenericDate { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? Entrants { get; set; }
            public decimal PrizePot { get; set; }
            public string? Url { get; set; }
            public int? CompetitionId { get; set; }
            public bool IsAuto { get; set; }
        }

        private async Task<List<CombinedRow>> CombineAsync(
            YearlyWinningsSummaryDto yearly,
            IReadOnlyCollection<ManualCompetitionResultDto> manual,
            CancellationToken cancellationToken)
        {
            var eligibilityExpression = new Regex(_settings.PrizePayout.EligibilityExpression ?? ".*", RegexOptions.IgnoreCase);
            var excludeExpression = new Regex(_settings.PrizePayout.ExclusionExpression ?? "^$", RegexOptions.IgnoreCase);

            var map = new Dictionary<string, CombinedRow>(StringComparer.OrdinalIgnoreCase);

            if (yearly != null)
            {
                var yearlyCompetitions = yearly.Competitions;

                var excluded = yearlyCompetitions.Where(tp => !eligibilityExpression.IsMatch(tp.CompetitionName)).ToList();
                excluded = excluded.Where(tp => excludeExpression.IsMatch(tp.CompetitionName)).ToList();

                if (excluded.Count > 0)
                {
                    _logger.LogWarning(
                        "The following competitions have been excluded for the winnings page based on eligbility and exclusion expressions: {Competitions}",
                        string.Join(", ", excluded.Select(ne => $"{ne.CompetitionName} (ID: {ne.CompetitionId})")));
                }

                yearlyCompetitions = yearlyCompetitions.Where(tp => eligibilityExpression.IsMatch(tp.CompetitionName)).ToList();
                yearlyCompetitions = yearlyCompetitions.Where(tp => !excludeExpression.IsMatch(tp.CompetitionName)).ToList();

                foreach (var c in yearlyCompetitions)
                {
                    var key = $"{c.CompetitionId}|{c.CompetitionDate:yyyy-MM-dd}|{c.CompetitionName}";

                    if (!map.TryGetValue(key, out var existing))
                    {
                        map[key] = new CombinedRow
                        {
                            RawDate = c.CompetitionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            ParsedDate = c.CompetitionDate,
                            IsGenericDate = false,
                            Name = c.CompetitionName,
                            Entrants = c.Entrants,
                            PrizePot = c.PrizePot,
                            CompetitionId = c.CompetitionId,
                            IsAuto = true
                        };
                    }
                    else
                    {
                        var chosenDate = ChooseBestParsedDate(existing.ParsedDate, c.CompetitionDate);
                        var rawDate = chosenDate.HasValue
                            ? chosenDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : existing.RawDate;

                        existing.RawDate = rawDate;
                        existing.ParsedDate = chosenDate;
                        existing.Name = string.IsNullOrWhiteSpace(existing.Name) ? c.CompetitionName : existing.Name;
                        existing.Entrants ??= c.Entrants;
                        if (existing.PrizePot == 0m) existing.PrizePot = c.PrizePot;
                        existing.CompetitionId ??= c.CompetitionId;
                        existing.IsAuto = true;
                    }
                }
            }

            if (manual != null)
            {
                foreach (var m in manual)
                {
                    var rawDate = (m.Date ?? string.Empty).Trim();
                    var parsedDate = TryParseFullDate(rawDate);
                    var isGenericDate = !parsedDate.HasValue && !string.IsNullOrWhiteSpace(rawDate);

                    var key = !string.IsNullOrWhiteSpace(m.Url)
                        ? m.Url!
                        : $"{rawDate}|{m.CompetitionName}";

                    if (!map.TryGetValue(key, out var existing))
                    {
                        map[key] = new CombinedRow
                        {
                            RawDate = rawDate,
                            ParsedDate = parsedDate,
                            IsGenericDate = isGenericDate,
                            Name = m.CompetitionName,
                            Entrants = m.Entrants,
                            PrizePot = m.PrizePot,
                            Url = m.Url,
                            IsAuto = false
                        };
                    }
                    else
                    {
                        var chosenParsedDate = ChooseBestParsedDate(parsedDate, existing.ParsedDate);
                        var combinedIsGeneric = isGenericDate || existing.IsGenericDate;
                        var chosenRawDate = combinedIsGeneric
                            ? (!string.IsNullOrWhiteSpace(rawDate) ? rawDate : existing.RawDate)
                            : existing.RawDate;

                        existing.RawDate = chosenRawDate;
                        existing.ParsedDate = chosenParsedDate;
                        existing.IsGenericDate = combinedIsGeneric;
                        existing.Name = string.IsNullOrWhiteSpace(existing.Name) ? m.CompetitionName : existing.Name;
                        existing.Entrants ??= m.Entrants;
                        if (existing.PrizePot == 0m && m.PrizePot != 0m) existing.PrizePot = m.PrizePot;
                        if (string.IsNullOrWhiteSpace(existing.Url) && !string.IsNullOrWhiteSpace(m.Url))
                        {
                            existing.Url = m.Url;
                        }
                    }
                }
            }

            foreach (var row in map.Values.Where(r => r.IsAuto && rowNeedsUrl(r)))
            {
                var compId = row.CompetitionId!.Value;
                var date = row.ParsedDate ?? DateTime.SpecifyKind(DateTime.Parse(row.RawDate), DateTimeKind.Utc);
                var blobName = CompetitionWinnersBlobNaming.GetBlobName(compId, date, row.Name);

                var exists = await _blobStorage.ExistsAsync(
                    CompetitionWinnersBlobNaming.ContainerName,
                    blobName,
                    cancellationToken);

                if (exists)
                {
                    var uri = _blobStorage.GetBlobUri(
                        CompetitionWinnersBlobNaming.ContainerName,
                        blobName);

                    row.Url = uri.ToString();
                }
            }

            return map.Values.ToList();

            static bool rowNeedsUrl(CombinedRow r)
                => string.IsNullOrWhiteSpace(r.Url) && r.CompetitionId.HasValue;
        }
    }
}
