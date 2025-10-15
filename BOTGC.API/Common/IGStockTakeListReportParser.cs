using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;
using System.Web;

namespace BOTGC.API.Common;

public sealed class IGStockTakeListReportParser(ILogger<IGStockTakeListReportParser> logger) : IReportParser<StockTakeDto>
{
    private readonly ILogger<IGStockTakeListReportParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");
    private static readonly string[] DateFormats =
    {
        "dd MMM yyyy HH:mm",
        "d MMM yyyy HH:mm"
    };

    public Task<List<StockTakeDto>> ParseReport(HtmlDocument document)
    {
        var results = new List<StockTakeDto>();
        try
        {
            var rows = document.DocumentNode.SelectNodes("//div[@id='stockTakeTable']//tbody/tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No stock take rows found.");
                return Task.FromResult(results);
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 4) continue;

                var createdBy = Clean(cells[0].InnerText);
                var createdAtText = Clean(cells[1].InnerText);
                var stockTakeDateText = Clean(cells[2].InnerText);
                var stockRoom = Clean(cells[3].InnerText);

                DateTime? createdAt = ParseGbDateTime(createdAtText);
                DateTime? stockTakeDate = ParseGbDateTime(stockTakeDateText);

                var actionsCell = cells.Count > 4 ? cells[4] : null;
                int? id = ExtractId(actionsCell);

                if (id == null)
                {
                    _logger.LogWarning("Row for '{CreatedBy}' on '{CreatedAt}' had no detectable ID.", createdBy, createdAtText);
                    continue;
                }

                results.Add(new StockTakeDto
                {
                    Id = id.Value,
                    CreatedBy = createdBy,
                    CreatedAt = createdAt,
                    StockTakeDate = stockTakeDate,
                    StockRoom = string.IsNullOrWhiteSpace(stockRoom) ? null : stockRoom
                });
            }

            _logger.LogInformation("Parsed {Count} stock take summaries.", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse stock take list HTML.");
        }

        return Task.FromResult(results);
    }

    private static string Clean(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var decoded = HttpUtility.HtmlDecode(s);
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded.Trim();
    }

    private static DateTime? ParseGbDateTime(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParseExact(s, DateFormats, Gb, DateTimeStyles.AssumeLocal, out var dt)) return dt;
        if (DateTime.TryParse(s, Gb, DateTimeStyles.AssumeLocal, out dt)) return dt;
        return null;
    }

    private static int? ExtractId(HtmlNode actionsCell)
    {
        if (actionsCell == null) return null;

        // Prefer the Edit link: href like "?tab=take&section=edit&id=98268"
        var editLink = actionsCell.SelectSingleNode(".//a[contains(@href,'section=edit') and contains(@href,'id=')]");
        if (editLink != null)
        {
            var href = editLink.GetAttributeValue("href", null);
            var id = TryParseIdFromHref(href);
            if (id != null) return id;
        }

        // Fallback: any control with data-ajax-data-inline-id attribute (covers CSV/PDF/Finalise rows)
        var anyWithDataId = actionsCell.SelectSingleNode(".//*[@data-ajax-data-inline-id]");
        if (anyWithDataId != null)
        {
            var val = anyWithDataId.GetAttributeValue("data-ajax-data-inline-id", null);
            if (int.TryParse(val, out var n)) return n;
        }

        return null;
    }

    private static int? TryParseIdFromHref(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        // Ensure we can parse query parameters whether relative or absolute
        try
        {
            var q = href.Contains("?") ? href.Substring(href.IndexOf("?", StringComparison.Ordinal) + 1) : href;
            var parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    var raw = HttpUtility.UrlDecode(kv[1]);
                    if (int.TryParse(raw, out var n)) return n;
                }
            }
        }
        catch
        {
            return null;
        }
        return null;
    }
}

