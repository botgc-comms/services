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

        public async Task<List<CompetitionSettingsDto>> ParseReport(HtmlDocument document)
        {
            try
            {
                var dto = new CompetitionSettingsDto
                {
                    Id = ExtractCompetitionId(document),
                    Name = ExtractInputValue(document, "compname"),
                    Contact = ExtractInputValue(document, "compadministrator"),
                    Comments = ExtractTextAreaHtml(document, "comments"),
                    Format = ExtractCompetitionFormat(document),
                    ResultsDisplay = ExtractResultsDisplay(document),
                    Tags = ExtractSelectedTags(document),
                    Date = ExtractDate(document)
                };

                dto.Links = HateOASLinks.GetCompetitionLinks(dto);

                return new List<CompetitionSettingsDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing competition settings.");
                return new List<CompetitionSettingsDto>();
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


        private static string ExtractInputValue(HtmlDocument doc, string id)
        {
            // Try to get value from input by id
            var value = doc.DocumentNode
                .SelectSingleNode($"//input[@id='{id}']")
                ?.GetAttributeValue("value", null);

            if (!string.IsNullOrEmpty(value))
                return value;

            string labelText = id switch
            {
                "compname" => "Name:",
                "compadministrator" => "Contact:",
                "compdate" => "Date:",
                _ => null
            };

            if (labelText != null)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())='{labelText}']");
                var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                             ?? labelNode?.SelectSingleNode("following-sibling::div");
                var text = divNode?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            return null;
        }


        private static string ExtractTextAreaHtml(HtmlDocument doc, string id)
        {
            // Try the current preview area selector
            var preview = doc.DocumentNode
                .SelectSingleNode($"//*[@id='{id}Area']//div[contains(@class, 'js-preview-area-value')]");
            if (preview != null)
                return preview.InnerText?.Trim();

            // Fallback: look for a label with "Comments:" and get the next div
            var labelNode = doc.DocumentNode
                .SelectSingleNode("//label[normalize-space(text())='Comments:']");
            var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                ?? labelNode?.SelectSingleNode("following-sibling::div");
            if (divNode != null)
                return divNode.InnerHtml?.Trim();

            return null;
        }


        private static DateTime ExtractDate(HtmlDocument doc)
        {
            // Try to get date from input first
            var dateStr = ExtractInputValue(doc, "compdate");
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                if (DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return result;
                }
                throw new FormatException($"Invalid date format found: {dateStr}");
            }

            // Fallback: look for a label with "Date:" and get the next div
            var labelNode = doc.DocumentNode.SelectSingleNode("//label[normalize-space(text())='Date:']");
            var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                ?? labelNode?.SelectSingleNode("following-sibling::div");
            if (divNode != null)
            {
                // Remove any child nodes (like <i> icons), then trim and parse the remaining text
                var dateText = string.Concat(divNode.ChildNodes
                    .Where(n => n.NodeType == HtmlNodeType.Text)
                    .Select(n => n.InnerText)).Trim();

                if (DateTime.TryParseExact(dateText, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return result;
                }
                throw new FormatException($"Invalid date format found: {dateText}");
            }

            throw new FormatException("Date not found in the document.");
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
            // Try to get from checked input first
            var selected = doc.DocumentNode
                .SelectSingleNode("//div[@id='comptype']//input[@checked]")
                ?.GetAttributeValue("id", "");

            if (!string.IsNullOrEmpty(selected))
            {
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

            // Fallback: try to get the text from the format display
            var formatText = doc.DocumentNode
                .SelectSingleNode("//div[@id='comptype']")?.InnerText
                ?? doc.DocumentNode
                    .SelectSingleNode("//label[contains(text(),'Competition Format:')]/following-sibling::div")
                    ?.InnerText;

            if (!string.IsNullOrWhiteSpace(formatText))
            {
                var normalized = formatText.Trim().ToLowerInvariant();
                return normalized switch
                {
                    var s when s.Contains("medal") => "Medal",
                    var s when s.Contains("stableford") => "Stableford",
                    var s when s.Contains("par/bogey") => "Par/Bogey",
                    var s when s.Contains("texas scramble") => "Texas Scramble",
                    var s when s.Contains("greensomes") => "Greensomes",
                    var s when s.Contains("foursomes") => "Foursomes",
                    _ => "Unknown"
                };
            }

            return "Unknown";
        }

        private static string ExtractResultsDisplay(HtmlDocument doc)
        {
            // Try to get from checked input first
            var selected = doc.DocumentNode
                .SelectSingleNode("//div[@id='wintypearea']//input[@checked]")
                ?.GetAttributeValue("value", null);

            if (!string.IsNullOrEmpty(selected))
            {
                return selected switch
                {
                    "0" => "Nett",
                    "1" => "Gross",
                    "2" => "Gross & Nett",
                    _ => "Unknown"
                };
            }

            // Fallback: look for a label with "Results Display:" and get the next div
            var labelNode = doc.DocumentNode
                .SelectSingleNode("//label[normalize-space(text())='Results Display:']");
            var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-8']")
                ?? labelNode?.SelectSingleNode("following-sibling::div");
            var text = divNode?.InnerText?.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                var normalized = text.ToLowerInvariant();
                return normalized switch
                {
                    var s when s.Contains("nett") => "Nett",
                    var s when s.Contains("gross & nett") => "Gross & Nett",
                    var s when s.Contains("gross") => "Gross",
                    _ => "Unknown"
                };
            }

            return "Unknown";
        }

    }
}
