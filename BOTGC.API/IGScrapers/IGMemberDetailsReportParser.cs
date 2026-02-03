using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.IGScrapers
{
    public class IGMemberDetailsReportParser : IReportParser<MemberDetailsDto>
    {
        private readonly ILogger<IGMemberDetailsReportParser> _logger;
        private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

        public IGMemberDetailsReportParser(ILogger<IGMemberDetailsReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MemberDetailsDto>> ParseReport(HtmlDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var result = new List<MemberDetailsDto>();

            try
            {
                var statusText = Clean(GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Membership Status']]/td[2]"));
                var memberNumberText = GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Membership Number']]/td[2]");

                var dto = new MemberDetailsDto
                {
                    MemberNumber = int.TryParse(memberNumberText, out int memberNumber) ? memberNumber : 0,
                    DateOfBirth = ParseUkDate(GetCellText(document, "//div[@id='member_personal_area']//tr[td/strong[normalize-space()='Date of Birth']]/td[2]")),
                    MembershipStatus = MapStatusToCode(Clean(statusText)),
                    JoinDate = ParseUkDate(GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Joining Date']]/td[2]")),
                    LeaveDate = ParseUkDate(GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Date Left']]/td[2]")),
                    ApplicationDate = ParseUkDate(GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Application Date']]/td[2]")),
                    Email = GetCellText(document, "//div[@id='member_contact_area']//tr[td/strong[normalize-space()='Email']]/td[2]")
                };

                var categoryRaw = GetCellText(document, "//div[@id='member_memstatus_area']//tr[td/strong[normalize-space()='Category']]/td[2]");
                dto.MembershipCategory = categoryRaw;
                dto.PrimaryCategory = MembershipHelper.GetPrimaryCategory(dto.MembershipStatus, dto.MembershipCategory, dto.LeaveDate, dto.JoinDate);

                ParseAddressBlock(document, dto);

                dto.FurtherInformation = ParseFurtherInformation(document);

                result.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse member details page.");
            }

            return await Task.FromResult(result);
        }

        private static string MapStatusToCode(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return string.Empty;
            var s = status.Trim().ToLowerInvariant();
            if (s == "active") return "R";
            if (s == "waiting") return "W";
            if (s == "leaver") return "L";
            if (s == "deceased") return "D";
            return status;
        }

        private static string GetCellText(HtmlDocument doc, string xPath)
        {
            var node = doc.DocumentNode.SelectSingleNode(xPath);
            return node == null ? string.Empty : HtmlEntity.DeEntitize(node.InnerText).Trim();
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"(✓|✔|tick)", "", RegexOptions.IgnoreCase).Trim();
            return cleaned;
        }

        private static DateTime? ParseUkDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParse(input, Uk, DateTimeStyles.AssumeLocal, out var dt)) return dt;

            var formats = new[] { "d MMM yyyy", "dd MMM yyyy", "d/M/yyyy", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(input.Trim(), formats, Uk, DateTimeStyles.AssumeLocal, out dt)) return dt;

            return null;
        }

        private static Dictionary<string, List<string>> ParseFurtherInformation(HtmlDocument doc)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var table = doc.DocumentNode.SelectSingleNode("//div[@id='membership_custom_params']//table");
            if (table == null) return result;

            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null || rows.Count == 0) return result;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 2) continue;

                var nameCell = cells[0];
                var valueCell = cells[1];

                var name = Clean(HtmlEntity.DeEntitize(nameCell.InnerText));
                if (string.IsNullOrWhiteSpace(name)) continue;

                var values = new List<string>();

                var links = valueCell.SelectNodes(".//a");
                if (links != null && links.Count > 0)
                {
                    foreach (var a in links)
                    {
                        var v = Clean(HtmlEntity.DeEntitize(a.InnerText));
                        if (!string.IsNullOrWhiteSpace(v)) values.Add(v);
                    }
                }
                else
                {
                    var v = Clean(HtmlEntity.DeEntitize(valueCell.InnerText));
                    if (!string.IsNullOrWhiteSpace(v)) values.Add(v);
                }

                if (result.TryGetValue(name, out var existing))
                {
                    existing.AddRange(values);
                }
                else
                {
                    result[name] = values;
                }
            }

            return result;
        }

        private static void ParseAddressBlock(HtmlDocument doc, MemberDetailsDto dto)
        {
            var addressNode = doc.DocumentNode.SelectSingleNode("//div[@id='member_address_area']");
            if (addressNode == null) return;

            var html = addressNode.InnerHtml;
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            var text = HtmlEntity.DeEntitize(html);

            var lines = text
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (lines.Count == 0) return;

            var countryCandidates = new[] { "United Kingdom", "UK", "England", "Scotland", "Wales", "Northern Ireland" };
            if (lines.Count > 0 && countryCandidates.Any(c => c.Equals(lines[^1], StringComparison.OrdinalIgnoreCase)))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            var postcodeIndex = -1;
            var postcodeRegex = new Regex(@"\b[A-Z]{1,2}\d{1,2}[A-Z]?\s?\d[A-Z]{2}\b", RegexOptions.IgnoreCase);
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (postcodeRegex.IsMatch(lines[i]))
                {
                    postcodeIndex = i;
                    break;
                }
            }

            if (postcodeIndex >= 0)
            {
                dto.Postcode = lines[postcodeIndex];
                lines.RemoveAt(postcodeIndex);
            }

            if (lines.Count > 0)
            {
                dto.County = lines[^1];
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.Count > 0)
            {
                dto.Town = lines[^1];
                lines.RemoveAt(lines.Count - 1);
            }

            dto.Address1 = lines.Count > 0 ? lines[0] : null;
            dto.Address2 = lines.Count > 1 ? lines[1] : null;
            dto.Address3 = lines.Count > 2 ? lines[2] : null;
        }
    }
}
