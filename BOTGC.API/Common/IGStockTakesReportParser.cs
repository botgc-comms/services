using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common
{
    public class IGStockTakeReportParser : IReportParser<StockTakeEntryDto>
    {
        private readonly ILogger<IGStockTakeReportParser> _logger;

        public IGStockTakeReportParser(ILogger<IGStockTakeReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<StockTakeEntryDto>> ParseReport(HtmlDocument document)
        {
            var results = new List<StockTakeEntryDto>();

            var rows = document.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No table rows found.");
                return results;
            }

            foreach (var tr in rows)
            {
                var tds = tr.SelectNodes("./td");
                if (tds == null || tds.Count < 6) continue;

                var action = (tds[1].InnerText ?? string.Empty).Trim();
                if (!action.Equals("stock take", StringComparison.OrdinalIgnoreCase)) continue;

                var timestamp = ParseTimestamp(tds[0]);
                var name = HtmlEntity.DeEntitize((tds[2].InnerText ?? string.Empty).Trim());
                var delta = ParseDecimal((tds[4].InnerText ?? string.Empty).Trim());
                var balance = ParseDecimal((tds[5].InnerText ?? string.Empty).Trim());

                int? productId = null;
                int? transactionId = null;

                var detailsCell = tds.Count > 6 ? tds[6] : null;
                var anchor = detailsCell?.SelectSingleNode(".//a[@href]");
                var href = anchor?.GetAttributeValue("href", null);

                if (!string.IsNullOrWhiteSpace(href))
                {
                    var idVal = ExtractFirstIntQueryParam(href, "id");
                    if (idVal.HasValue)
                    {
                        // Treat the `id` in the details link as the ProductId if no better source exists.
                        // If your HTML later adds a dedicated product id attribute, prefer that instead.
                        productId = idVal.Value;
                        transactionId = idVal.Value;
                    }
                }

                decimal? previous = null;
                if (balance.HasValue && delta.HasValue)
                {
                    previous = balance.Value - delta.Value;
                }

                var dto = new StockTakeEntryDto
                {
                    StockItemId = productId!.Value,
                    Name = name,
                    PreviousQuantity = previous,
                    NewQuantity = balance,
                    Difference = delta,
                    Timestamp = timestamp
                };

                results.Add(dto);
            }

            _logger.LogInformation("Parsed {Count} stock-take rows.", results.Count);
            return results;
        }

        private static DateTimeOffset? ParseTimestamp(HtmlNode dateCell)
        {
            if (dateCell == null) return null;

            var attr = dateCell.GetAttributeValue("data-csv-content", null);
            if (!string.IsNullOrWhiteSpace(attr))
            {
                // e.g. 2025/09/03 07:16
                if (DateTime.TryParseExact(attr, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var a))
                    return new DateTimeOffset(a);
            }

            var text = (dateCell.InnerText ?? string.Empty).Trim();
            // e.g. 03 Sep 2025 19:16
            var formats = new[]
            {
                "dd MMM yyyy HH:mm",
                "dd/MM/yyyy HH:mm",
                "yyyy/MM/dd HH:mm",
                "yyyy-MM-dd HH:mm",
                "dd/MM/yyyy",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                return new DateTimeOffset(dt);

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                return new DateTimeOffset(dt);

            return null;
        }

        private static decimal? ParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var cleaned = s.Replace(" ", string.Empty).Replace(",", string.Empty);
            if (cleaned.StartsWith("+", StringComparison.Ordinal)) cleaned = cleaned[1..];
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out d)) return d;
            return null;
        }

        private static int? ExtractFirstIntQueryParam(string href, string paramName)
        {
            var match = Regex.Match(href, @"[?&]" + Regex.Escape(paramName) + @"=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n)) return n;
            return null;
        }
    }
}
