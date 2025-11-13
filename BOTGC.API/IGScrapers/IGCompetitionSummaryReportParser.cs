using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace BOTGC.API.IGScrapers
{
    public class IGCompetitionSummaryReportParser : IReportParser<CompetitionSummaryDto>
    {
        private readonly ILogger<IGCompetitionSummaryReportParser> _logger;

        public IGCompetitionSummaryReportParser(ILogger<IGCompetitionSummaryReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CompetitionSummaryDto>> ParseReport(HtmlDocument document)
        {
            try
            {
                var dto = new CompetitionSummaryDto
                {
                    Id = ExtractCompetitionId(document),
                    MultiPartCompetition = ExtractMultiPartCompetitionIds(document),
                };

                dto.Links = HateOASLinks.GetCompetitionLinks(dto);

                return new List<CompetitionSummaryDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing competition summary.");
                return new List<CompetitionSummaryDto>();
            }
        }

        private static int? ExtractCompetitionId(HtmlDocument doc)
        {
            // Find the first <a> with href containing 'compid='
            var anchor = doc.DocumentNode
                .SelectSingleNode("//a[contains(@href, 'compid=')]");

            var href = anchor?.GetAttributeValue("href", null);
            if (!string.IsNullOrEmpty(href))
            {
                // Use regex to extract compid value
                var match = System.Text.RegularExpressions.Regex.Match(href, @"compid=(\d+)");
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }

            return null;
        }

        private static Dictionary<string, int>? ExtractMultiPartCompetitionIds(HtmlDocument doc)
        {
            var result = new Dictionary<string, int>();

            // Find the form-group for "Component Competitions:"
            var formGroups = doc.DocumentNode.SelectNodes("//div[contains(@class,'form-group')]");
            if (formGroups == null)
                return null;

            foreach (var group in formGroups)
            {
                var label = group.SelectSingleNode(".//label");
                if (label == null)
                    continue;

                if (label.InnerText.Trim().Equals("Component Competitions:", StringComparison.OrdinalIgnoreCase))
                {
                    var links = group.SelectNodes(".//a[contains(@href, 'compid=')]");
                    if (links == null)
                        continue;

                    foreach (var link in links)
                    {
                        var title = link.InnerText.Trim();
                        var href = link.GetAttributeValue("href", "");
                        var match = System.Text.RegularExpressions.Regex.Match(href, @"compid=(\d+)");
                        if (match.Success)
                        {
                            var compId = int.Parse(match.Groups[1].Value);
                            result[title] = compId;
                        }
                    }
                    break;
                }
            }

            return result.Count > 0 ? result : null;
        }

    }
}
