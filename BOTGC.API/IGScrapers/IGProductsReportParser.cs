using System.Text.Json;
using System.Text.RegularExpressions;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;

namespace BOTGC.API.IGScrapers;

public sealed class IGProductsReportParser : IReportParser<TillProductLookupDto>
{
    private readonly ILogger<IGProductsReportParser> _logger;

    public IGProductsReportParser(ILogger<IGProductsReportParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TillProductLookupDto>> ParseReport(HtmlDocument document)
    {
        await Task.Yield();

        var results = new List<TillProductLookupDto>();

        var rowNodes = document.DocumentNode.SelectNodes("//table[@id='table']//tr[@data-uniqueid]");
        if (rowNodes != null && rowNodes.Count > 0)
        {
            foreach (var row in rowNodes)
            {
                var idStr = row.GetAttributeValue("data-uniqueid", null);
                if (!int.TryParse(idStr, out var id)) continue;

                var nameCell = row.SelectSingleNode("./td[1]");
                var name = HtmlEntity.DeEntitize(nameCell?.InnerText ?? string.Empty).Trim();

                results.Add(new TillProductLookupDto
                {
                    ProductId = id,
                    ProductName = string.IsNullOrWhiteSpace(name) ? null : name
                });
            }

            _logger.LogInformation("Parsed {Count} products from table rows.", results.Count);
            return results;
        }

        var html = document.DocumentNode.InnerHtml ?? string.Empty;
        var m = Regex.Match(html, @"var\s+data\s*=\s*(\[[\s\S]*?\]);", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var json = m.Groups[1].Value;

            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var idProp = el.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
                var nameProp = el.TryGetProperty("name", out var nameNode) ? nameNode.GetString() : null;

                if (!string.IsNullOrWhiteSpace(idProp) && int.TryParse(idProp, out var id))
                {
                    results.Add(new TillProductLookupDto
                    {
                        ProductId = id,
                        ProductName = string.IsNullOrWhiteSpace(nameProp) ? null : nameProp?.Trim()
                    });
                }
            }
            _logger.LogInformation("Parsed {Count} products from embedded JSON.", results.Count);
        }
        else
        {
            _logger.LogWarning("No table rows or embedded JSON found to parse.");
        }

        return results;
    }
}

