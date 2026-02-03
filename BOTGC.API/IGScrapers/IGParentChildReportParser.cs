using System.Globalization;
using System.Text.RegularExpressions;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;

namespace BOTGC.API.IGScrapers;

public sealed class IGParentChildReportParser : IReportParser<ParentChildDto>
{
    private readonly ILogger<IGParentChildReportParser> _logger;

    public IGParentChildReportParser(ILogger<IGParentChildReportParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ParentChildDto>> ParseReport(HtmlDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));

        var results = new List<ParentChildDto>();

        var table = document.DocumentNode.SelectSingleNode("//div[@id='reporttable']//table[.//thead//th]");
        if (table == null)
        {
            _logger.LogWarning("Parent-child report table not found.");
            return results;
        }

        var headerTexts = table.SelectNodes(".//thead//th")
            ?.Select(th => Clean(th.InnerText))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (headerTexts == null || headerTexts.Length == 0)
        {
            _logger.LogWarning("No headers found in parent-child report.");
            return results;
        }

        var columnMapping = new Dictionary<string, string>
        {
            { "^Member\\s*ID$", "ParentMemberId" },
            { "^Forename$", "Forename" },
            { "^Surname$", "Surname" },
            { "^Parent-Child\\s*Relationship$", "ChildMemberId" },
            { "^Member\\s*Status$", "MemberStatus" }
        };

        var headerIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headerTexts.Length; i++)
        {
            var header = headerTexts[i];

            foreach (var pattern in columnMapping.Keys)
            {
                if (Regex.IsMatch(header, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    headerIndexMap[columnMapping[pattern]] = i;
                    break;
                }
            }
        }

        if (!headerIndexMap.TryGetValue("ParentMemberId", out var parentIndex) ||
            !headerIndexMap.TryGetValue("ChildMemberId", out var childIndex))
        {
            _logger.LogError(
                "Required columns missing. Found headers: {Headers}",
                string.Join(", ", headerTexts)
            );
            return results;
        }

        var rows = table.SelectNodes(".//tbody//tr");
        if (rows == null || rows.Count == 0)
        {
            _logger.LogInformation("No data rows found in parent-child report.");
            return results;
        }

        var parentToChildren = new Dictionary<int, HashSet<int>>();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null || cells.Count == 0) continue;

            var values = cells.Select(td => Clean(td.InnerText)).ToArray();

            if (parentIndex >= values.Length || childIndex >= values.Length) continue;

            var parentText = values[parentIndex];
            var childText = values[childIndex];

            if (!TryParseInt(parentText, out var parentId) || parentId <= 0) continue;
            if (!TryParseInt(childText, out var childId) || childId <= 0) continue;

            if (!parentToChildren.TryGetValue(parentId, out var children))
            {
                children = new HashSet<int>();
                parentToChildren[parentId] = children;
            }

            children.Add(childId);
        }

        foreach (var kvp in parentToChildren.OrderBy(k => k.Key))
        {
            results.Add(new ParentChildDto
            {
                ParentMemberId = kvp.Key,
                Children = kvp.Value.OrderBy(x => x).ToArray()
            });
        }

        _logger.LogInformation(
            "Successfully parsed {ParentCount} parents and {RelationshipCount} parent-child relationships.",
            results.Count,
            parentToChildren.Sum(k => k.Value.Count)
        );

        return results;
    }

    private static string Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = HtmlEntity.DeEntitize(s);
        s = Regex.Replace(s, "\\s+", " ", RegexOptions.CultureInvariant);
        return s.Trim();
    }

    private static bool TryParseInt(string? s, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim();

        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        var digits = Regex.Match(s, "\\d+", RegexOptions.CultureInvariant);
        if (digits.Success && int.TryParse(digits.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        return false;
    }
}

