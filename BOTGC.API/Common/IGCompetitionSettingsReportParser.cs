using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace BOTGC.API.Common
{
    public class IGCompetitionSettingsReportParser : IReportParser<CompetitionSettingsDto>
    {
        private readonly ILogger<IGCompetitionSettingsReportParser> _logger;

        public IGCompetitionSettingsReportParser(ILogger<IGCompetitionSettingsReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<CompetitionSettingsDto> ParseReport(HtmlDocument document)
        {
            try
            {
                var dto = new CompetitionSettingsDto
                {
                    Name = ExtractInputValue(document, "compname"),
                    Contact = ExtractInputValue(document, "compadministrator"),
                    Comments = ExtractTextAreaHtml(document, "comments"),
                    Format = ExtractCompetitionFormat(document),
                    ResultsDisplay = ExtractResultsDisplay(document),
                    Tags = ExtractSelectedTags(document),
                    Datae = ExtractDate(document)
                };

                return new List<CompetitionSettingsDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing competition settings.");
                return new List<CompetitionSettingsDto>();
            }
        }

        private static string ExtractInputValue(HtmlDocument doc, string id)
        {
            return doc.DocumentNode
                      .SelectSingleNode($"//input[@id='{id}']")
                      ?.GetAttributeValue("value", null);
        }

        private static string ExtractTextAreaHtml(HtmlDocument doc, string id)
        {
            var preview = doc.DocumentNode.SelectSingleNode($"//*[@id='{id}Area']//div[contains(@class, 'js-preview-area-value')]");
            return preview?.InnerText?.Trim();
        }

        private static DateTime ExtractDate(HtmlDocument doc)
        {
            var dateStr = ExtractInputValue(doc, "compdate");
            if (DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            throw new FormatException($"Invalid date format found: {dateStr}");
        }

        private static string[] ExtractSelectedTags(HtmlDocument doc)
        {
            var tagList = doc.DocumentNode
                .SelectNodes("//select[@id='tags']/option[@selected]")?
                .Select(n => n.InnerText.Trim())
                .ToArray();

            return tagList ?? Array.Empty<string>();
        }

        private static string ExtractCompetitionFormat(HtmlDocument doc)
        {
            var selected = doc.DocumentNode
                .SelectSingleNode("//div[@id='comptype']//input[@checked]")?
                .GetAttributeValue("id", "");

            return selected switch
            {
                "medaltype" => "Medal",
                "stabtype" => "Stableford",
                "parbogeytype" => "Par/Bogey",
                "texastype" => "Texas Scramble",
                "greensomestype" => "Greensomes",
                "foursomestype" => "Foursomes",
                _ => "Unknown"
            };
        }

        private static string ExtractResultsDisplay(HtmlDocument doc)
        {
            var selected = doc.DocumentNode
                .SelectSingleNode("//div[@id='wintypearea']//input[@checked]")?
                .GetAttributeValue("value", null);

            return selected switch
            {
                "0" => "Nett",
                "1" => "Gross",
                "2" => "Gross & Nett",
                _ => "Unknown"
            };
        }
    }
}
