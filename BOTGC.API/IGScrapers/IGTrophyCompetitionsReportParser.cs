using System.Globalization;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;

namespace BOTGC.API.IGScrapers;

public sealed class IGTrophyCompetitionsReportParser : IReportParser<ManualCompetitionResultDto>
{
    private readonly ILogger<IGTrophyCompetitionsReportParser> _logger;

    public IGTrophyCompetitionsReportParser(ILogger<IGTrophyCompetitionsReportParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ManualCompetitionResultDto>> ParseReport(HtmlDocument document)
    {
        await Task.Yield();

        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var results = new List<ManualCompetitionResultDto>();

        var table = FindTrophyCompetitionsTable(document);
        if (table == null)
        {
            _logger.LogWarning("Trophy competitions table not found.");
            return results;
        }

        var rows = table.SelectNodes("./tbody/tr");
        if (rows == null || rows.Count == 0)
        {
            _logger.LogWarning("Trophy competitions table contains no data rows.");
            return results;
        }

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null || cells.Count < 5)
            {
                continue;
            }

            var dateText = CleanCellText(cells[0]);
            var competitionName = CleanCellText(cells[1]);
            var entrantsText = CleanCellText(cells[2]);
            var prizePotText = CleanCellText(cells[3]);

            var linkNode = cells[4].SelectSingleNode(".//a[@href]");
            var url = linkNode?.GetAttributeValue("href", null);

            // Skip completely empty/footer rows
            if (string.IsNullOrWhiteSpace(dateText) &&
                string.IsNullOrWhiteSpace(competitionName) &&
                string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var entrants = TryParseNullableInt(entrantsText);
            var prizePot = TryParseNullableDecimal(prizePotText) ?? 0m;

            results.Add(new ManualCompetitionResultDto
            {
                Date = dateText,
                CompetitionName = competitionName,
                Entrants = entrants,
                PrizePot = prizePot,
                Url = url
            });
        }

        _logger.LogInformation(
            "Parsed {Count} manual competition results from trophy competitions table.",
            results.Count);

        return results;
    }

    private static HtmlNode? FindTrophyCompetitionsTable(HtmlDocument document)
    {
        var tables = document.DocumentNode.SelectNodes("//table");
        if (tables == null)
        {
            return null;
        }

        foreach (var table in tables)
        {
            var headers = table.SelectNodes(".//thead//th");
            if (headers == null || headers.Count == 0)
            {
                continue;
            }

            var headerTexts = headers
                .Select(h => HtmlEntity.DeEntitize(h.InnerText ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant())
                .ToList();

            if (headerTexts.Contains("date") &&
                headerTexts.Contains("competition name") &&
                headerTexts.Contains("entrants") &&
                headerTexts.Contains("prize pot") &&
                headerTexts.Contains("link"))
            {
                return table;
            }
        }

        return null;
    }

    private static string CleanCellText(HtmlNode cell)
    {
        if (cell == null)
        {
            return string.Empty;
        }

        var text = HtmlEntity.DeEntitize(cell.InnerText ?? string.Empty);
        text = text
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        // Normalise pure <br> cells to empty
        return text.Equals("<br>", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : text;
    }

    private static int? TryParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal? TryParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(
                value,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        if (decimal.TryParse(
                value,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.GetCultureInfo("en-GB"),
                out parsed))
        {
            return parsed;
        }

        return null;
    }
}
