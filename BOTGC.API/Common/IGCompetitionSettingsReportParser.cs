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
                    Date = ExtractDate(document),

                    HandicapAllowancePercent = ExtractPercentIntByLabel(document, "Handicap Allowances:"),
                    Divisions = ExtractWordNumberByLabel(document, "Divisions:"),
                    Division1Limit = ExtractIntByLabel(document, "Division 1 Limit:"),
                    Division2Limit = ExtractIntByLabel(document, "Division 2 Limit:"),
                    Division3Limit = ExtractIntByLabel(document, "Division 3 Limit:"),
                    Division4Limit = ExtractIntByLabel(document, "Division 4 Limit:"),
                    PrizesPerDivision = ExtractWordNumberByLabel(document, "Prizes per Division:"),
                    PrizeStructure = ExtractTextByLabel(document, "Prize Structure:"),
                    ResultsViewableInProgress = ExtractYesNoByLabel(document, "Results viewable while in-progress?"),
                    TwosCompetitionIncluded = ExtractYesNoByLabel(document, "Two's competition included?"),
                    TieResolution = ExtractTextByLabel(document, "Tie Resolution:"),

                    TouchscreenAllowPlayerInput = ExtractToggleByText(document, "Allow Player Input at Touchscreen?"),
                    TouchscreenRequireCheckIn = ExtractToggleByText(document, "Require players to Check In at Touchscreen?"),
                    TouchscreenShowLeaderboard = ExtractToggleByText(document, "Show Touchscreen Leaderboard"),
                    MobileAllowPlayerInput = ExtractToggleByText(document, "Allow Player Input on Mobile"),
                    MobileAllowPartnerScoring = ExtractToggleByText(document, "Allow partner scoring within the igMember App"),
                    MobileShowLeaderboard = ExtractToggleByText(document, "Show Mobile Leaderboard"),

                    EnglandGolfMedal = ExtractYesNoByLabelContains(document, "England Golf", "Medal?"),
                    PartOfAlternateDayCompetition = ExtractYesNoByLabel(document, "Part of an alternate day competition?")
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
            var anchor = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'compid=')]");
            var href = anchor?.GetAttributeValue("href", null);
            if (!string.IsNullOrEmpty(href))
            {
                var match = System.Text.RegularExpressions.Regex.Match(href, @"compid=(\d+)");
                if (match.Success) return int.Parse(match.Groups[1].Value);
            }
            return null;
        }

        private static string ExtractInputValue(HtmlDocument doc, string id)
        {
            var value = doc.DocumentNode.SelectSingleNode($"//input[@id='{id}']")?.GetAttributeValue("value", null);
            if (!string.IsNullOrEmpty(value)) return value;

            string labelText = id switch
            {
                "compname" => "Name:",
                "compadministrator" => "Contact:",
                "compdate" => "Date:",
                _ => null
            };

            if (labelText != null)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())={ToXPathLiteral(labelText)}]");
                var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                             ?? labelNode?.SelectSingleNode("following-sibling::div");
                var text = divNode?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(text)) return text;
            }

            return null;
        }

        private static string ExtractTextAreaHtml(HtmlDocument doc, string id)
        {
            var preview = doc.DocumentNode.SelectSingleNode($"//*[@id='{id}Area']//div[contains(@class, 'js-preview-area-value')]");
            if (preview != null) return preview.InnerText?.Trim();

            var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())={ToXPathLiteral("Comments:")}]");
            var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                ?? labelNode?.SelectSingleNode("following-sibling::div");
            if (divNode != null) return divNode.InnerHtml?.Trim();

            return null;
        }

        private static DateTime ExtractDate(HtmlDocument doc)
        {
            var dateStr = ExtractInputValue(doc, "compdate");
            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                if (DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) return result;
                throw new FormatException($"Invalid date format found: {dateStr}");
            }

            var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())={ToXPathLiteral("Date:")}]");
            var divNode = labelNode?.ParentNode?.SelectSingleNode("div[@class='col-sm-10']")
                ?? labelNode?.SelectSingleNode("following-sibling::div");
            if (divNode != null)
            {
                var dateText = string.Concat(divNode.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text).Select(n => n.InnerText)).Trim();
                if (DateTime.TryParseExact(dateText, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) return result;
                throw new FormatException($"Invalid date format found: {dateText}");
            }

            throw new FormatException("Date not found in the document.");
        }

        private static string[] ExtractSelectedTags(HtmlDocument doc)
        {
            var tagList = doc.DocumentNode.SelectNodes("//select[@id='tags']/option[@selected]")?
                .Select(n => n.InnerText.Trim())
                .ToArray();
            return tagList ?? Array.Empty<string>();
        }

        private static string ExtractCompetitionFormat(HtmlDocument doc)
        {
            var selected = doc.DocumentNode.SelectSingleNode("//div[@id='comptype']//input[@checked]")?.GetAttributeValue("id", "");
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

            var formatText = doc.DocumentNode.SelectSingleNode("//div[@id='comptype']")?.InnerText
                             ?? doc.DocumentNode.SelectSingleNode("//label[contains(text(),'Competition Format:')]/following-sibling::div")?.InnerText;

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
            var selected = doc.DocumentNode.SelectSingleNode("//div[@id='wintypearea']//input[@checked]")?.GetAttributeValue("value", null);
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

            var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())={ToXPathLiteral("Results Display:")}]");
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

        private static string? ExtractTextByLabel(HtmlDocument doc, string labelText)
        {
            var labelNode = doc.DocumentNode.SelectSingleNode($"//label[normalize-space(text())={ToXPathLiteral(labelText)}]");
            var valueNode = labelNode?.ParentNode?.SelectSingleNode("div[starts-with(@class,'col-sm-')]")
                           ?? labelNode?.SelectSingleNode("following-sibling::div");
            var text = valueNode?.InnerText?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : HtmlEntity.DeEntitize(text).Trim();
        }

        private static int? ExtractIntByLabel(HtmlDocument doc, string labelText)
        {
            var text = ExtractTextByLabel(doc, labelText);
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = new string(text.Where(char.IsDigit).ToArray());
            if (int.TryParse(text, out var val)) return val;
            return null;
        }

        private static int? ExtractPercentIntByLabel(HtmlDocument doc, string labelText)
        {
            var text = ExtractTextByLabel(doc, labelText);
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Replace("%", "").Trim();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)) return val;
            return null;
        }

        private static int? ExtractWordNumberByLabel(HtmlDocument doc, string labelText)
        {
            var text = ExtractTextByLabel(doc, labelText);
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.Trim().ToLowerInvariant();
            return t switch
            {
                "one" => 1,
                "two" => 2,
                "three" => 3,
                "four" => 4,
                _ => ExtractIntByLabel(doc, labelText)
            };
        }

        private static bool? ExtractYesNoByLabel(HtmlDocument doc, string labelText)
        {
            var text = ExtractTextByLabel(doc, labelText);
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.Trim().ToLowerInvariant();
            if (t.StartsWith("yes")) return true;
            if (t.StartsWith("no")) return false;
            return null;
        }

        private static bool? ExtractYesNoByLabelContains(HtmlDocument doc, params string[] tokens)
        {
            var labels = doc.DocumentNode.SelectNodes("//label");
            if (labels == null) return null;

            foreach (var lbl in labels)
            {
                var norm = HtmlEntity.DeEntitize(lbl.InnerText).Replace("\n", " ").Replace("\r", " ").Trim();
                var match = tokens.All(t => norm.Contains(t, StringComparison.OrdinalIgnoreCase));
                if (!match) continue;

                var valueNode = lbl.ParentNode?.SelectSingleNode("div[starts-with(@class,'col-sm-')]")
                                ?? lbl.SelectSingleNode("following-sibling::div");
                var text = valueNode?.InnerText?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (text.StartsWith("yes")) return true;
                if (text.StartsWith("no")) return false;
                return null;
            }

            return null;
        }

        private static bool? ExtractToggleByText(HtmlDocument doc, string containsText)
        {
            var node = doc.DocumentNode.SelectSingleNode(
                $"//div[contains(@class,'slateContent') and contains(@class,'form-horizontal')]//div[contains(@class,'form-group')]//div[starts-with(@class,'col-sm-')][contains(normalize-space(.), {ToXPathLiteral(containsText)})]"
            );
            if (node == null) return null;
            var onIcon = node.SelectSingleNode(".//i[contains(@class,'fa-toggle-on')]");
            var offIcon = node.SelectSingleNode(".//i[contains(@class,'fa-toggle-off')]");
            if (onIcon != null) return true;
            if (offIcon != null) return false;
            return null;
        }

        private static string ToXPathLiteral(string value)
        {
            if (value.IndexOf('\'') == -1) return $"'{value}'";
            if (value.IndexOf('\"') == -1) return $"\"{value}\"";
            var parts = value.Split('\'');
            return "concat(" + string.Join(", \"'\", ", parts.Select(p => $"'{p}'")) + ")";
        }
    }
}
