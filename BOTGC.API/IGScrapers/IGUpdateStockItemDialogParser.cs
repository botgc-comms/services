using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;

namespace BOTGC.API.IGScrapers;

/// <summary>
/// Parses the IG “Items &gt; Edit Product” page and extracts the product fields and all Trade Units.
/// </summary>
public class IGUpdateStockItemDialogParser(ILogger<IGUpdateStockItemDialogParser> logger) : IReportParser<StockItemEditDialogDto>
{
    private readonly ILogger<IGUpdateStockItemDialogParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<List<StockItemEditDialogDto>> ParseReport(HtmlDocument document)
    {
        var result = new List<StockItemEditDialogDto>();

        if (document?.DocumentNode == null)
        {
            _logger.LogError("HTML document is null.");
            return result;
        }

        try
        {
            var root = document.DocumentNode;

            var idVal = GetInputValue(root, "//input[@name='id']");
            var nameVal = GetInputValue(root, "//input[@name='name']");
            var baseUnitIdVal = GetSelectedOptionValue(root, "//select[@name='base_unit_id']");
            var divisionIdVal = GetSelectedOptionValue(root, "//select[@name='division_id']");
            var minAlertVal = GetInputValue(root, "//input[@name='min_alert']");
            var maxAlertVal = GetInputValue(root, "//input[@name='max_alert']");
            var activeNode = root.SelectSingleNode("//input[@name='active' and @type='checkbox']");
            var isActive = activeNode != null && activeNode.GetAttributeValue("checked", null) != null;

            var dto = new StockItemEditDialogDto
            {
                Id = ParseInt(idVal),
                Name = nameVal,
                BaseUnitId = ParseNullableInt(baseUnitIdVal),
                DivisionId = ParseNullableInt(divisionIdVal),
                MinAlert = ParseNullableDecimal(minAlertVal),
                MaxAlert = ParseNullableDecimal(maxAlertVal),
                Active = isActive,
                TradeUnits = ParseTradeUnits(root)
            };

            result.Add(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse IG Item Edit page.");
        }

        return await Task.FromResult(result);
    }

    private static string GetInputValue(HtmlNode root, string xPath)
    {
        var node = root.SelectSingleNode(xPath);
        return node?.GetAttributeValue("value", string.Empty)?.Trim() ?? string.Empty;
    }

    private static string? GetSelectedOptionValue(HtmlNode root, string selectXPath)
    {
        var selected = root.SelectSingleNode($"{selectXPath}/option[@selected]") ??
                       root.SelectSingleNode($"{selectXPath}/option[@selected='selected']");
        return selected?.GetAttributeValue("value", null)?.Trim();
    }

    private static int ParseInt(string? s)
    {
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static int? ParseNullableInt(string? s)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }

    private static decimal? ParseNullableDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var cleaned = s.Replace("£", "").Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }

    private static List<StockItemEditDialogTradeUnitDto> ParseTradeUnits(HtmlNode root)
    {
        var list = new List<StockItemEditDialogTradeUnitDto>();

        var rows = root.SelectNodes("//section[h3[normalize-space()='Trade Units']]//table//tbody/tr[not(@id='addRow')]");
        if (rows == null || rows.Count == 0) return list;

        foreach (var row in rows)
        {
            try
            {
                var id = row.SelectSingleNode(".//input[@name='ids[]']")?.GetAttributeValue("value", null);
                var name = row.SelectSingleNode(".//input[@name='names[]']")?.GetAttributeValue("value", null);
                var cost = row.SelectSingleNode(".//input[@name='costs[]']")?.GetAttributeValue("value", null);
                var ratio = row.SelectSingleNode(".//input[@name='conversion_ratios[]']")?.GetAttributeValue("value", null);
                var precision = row.SelectSingleNode(".//input[@name='precision_of_units[]']")?.GetAttributeValue("value", null)
                              ?? row.SelectSingleNode(".//input[@id='precisionOfUnits']")?.GetAttributeValue("value", null);

                var dto = new StockItemEditDialogTradeUnitDto
                {
                    Id = ParseInt(id),
                    Name = name ?? string.Empty,
                    Cost = ParseNullableDecimal(cost) ?? 0m,
                    ConversionRatio = ParseNullableDecimal(ratio) ?? 0m,
                    PrecisionOfUnit = ParseInt(precision)
                };

                list.Add(dto);
            }
            catch
            {
                // Skip malformed row
            }
        }

        return list;
    }
}


