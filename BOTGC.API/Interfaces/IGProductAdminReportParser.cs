using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOTGC.API.Dto;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.Interfaces;

public sealed class IGProductAdminReportParser : IReportParser<TillProductInformationDto>
{
    private readonly ILogger<IGProductAdminReportParser> _logger;

    public IGProductAdminReportParser(ILogger<IGProductAdminReportParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TillProductInformationDto>> ParseReport(HtmlDocument document)
    {
        var result = new TillProductInformationDto
        {
            Components = new List<TillProductStockComponentDto>()
        };

        result.ProductId = ExtractProductId(document);
        result.ProductName = ExtractProductName(document);

        var clubhouseNode = document.DocumentNode.SelectSingleNode("//input[@id='discount_group_3']");
        var staffNode = document.DocumentNode.SelectSingleNode("//input[@id='discount_group_2']");
        var standardNode = document.DocumentNode.SelectSingleNode("//input[@id='discount_group_1']");

        result.DiscountClubhouseSocial = ReadPercent(clubhouseNode);
        result.DiscountStaff = ReadPercent(staffNode);
        result.DiscountStandard = ReadPercent(standardNode);

        var priceNode = document.DocumentNode.SelectSingleNode("//div[@id='addproduct_dialog']//input[@id='price' and @name='price']");
        var vatRateNode = document.DocumentNode.SelectSingleNode("//div[@id='addproduct_dialog']//select[@id='vat_rate']");
        var vatInclNode = document.DocumentNode.SelectSingleNode("//div[@id='addproduct_dialog']//span[@id='vattypecontainer']//input[@name='vat_type' and @value='incl' and @checked]");

        var basePrice = ReadDecimal(priceNode?.GetAttributeValue("value", null));
        var vatRate = ReadVatRate(vatRateNode);
        var priceIsIncVat = vatInclNode != null;

        if (basePrice.HasValue)
        {
            result.SellingPriceIncVat = priceIsIncVat
                ? basePrice.Value
                : vatRate.HasValue ? basePrice.Value * (1m + vatRate.Value) : basePrice.Value;
        }

        var componentRows = document.DocumentNode.SelectNodes("//div[@id='component-list']//div[contains(@class,'component-item')]");
        if (componentRows != null)
        {
            foreach (var row in componentRows)
            {
                var hiddenId = row.SelectSingleNode(".//input[@type='hidden' and @name='stock_ids[]']");
                if (hiddenId == null) continue;

                var select = row.SelectSingleNode(".//select[contains(@class,'stockItems')]");
                var unitNode = row.SelectSingleNode(".//p[contains(@class,'stock-unit')]");
                var qtyNode = row.SelectSingleNode(".//input[@name='stock_amounts[]']");
                var priceEachNode = row.SelectSingleNode(".//input[@name='selling_price[]']");

                var selectedOption = select?.SelectSingleNode(".//option[@selected]")
                                     ?? select?.SelectSingleNode(".//option[@value and string-length(@value)>0]");

                var stockId = ReadInt(hiddenId?.GetAttributeValue("value", null))
                              ?? ReadInt(selectedOption?.GetAttributeValue("value", null));

                var name = selectedOption?.InnerText?.Trim();
                if (string.IsNullOrWhiteSpace(name) && select != null)
                {
                    name = select.InnerText?
                        .Split('\n')
                        .Select(s => s.Trim())
                        .FirstOrDefault(s => !string.IsNullOrEmpty(s));
                }

                var extId = default(string);
                var extIdMatch = Regex.Match(name, "^(.*?)\\s+\\[(\\d+)\\]\\s*$");
                if (extIdMatch.Success)
                {
                    name = extIdMatch.Groups[1].Value.Trim();
                    extId = extIdMatch.Groups[2].Value;
                }

                var unit = selectedOption?.GetAttributeValue("data-unit", null) ?? unitNode?.InnerText?.Trim();
                var qty = ReadDecimal(qtyNode?.GetAttributeValue("value", null));
                var sellingPrice = ReadDecimal(priceEachNode?.GetAttributeValue("value", null));

                var empty = stockId == null && string.IsNullOrWhiteSpace(name) && unit == null && qty == null && sellingPrice == null;
                if (empty) continue;

                result.Components.Add(new TillProductStockComponentDto
                {
                    StockId = stockId,
                    Name = name,
                    ExternalId = extId,
                    Unit = unit,
                    Quantity = qty,
                    SellingPrice = sellingPrice
                });
            }
        }

        _logger.LogInformation("Parsed product {ProductId} \"{ProductName}\", discounts, and {Count} stock components.", result.ProductId, result.ProductName, result.Components.Count);
        return new List<TillProductInformationDto> { result };
    }

    private static int? ExtractProductId(HtmlDocument document)
    {
        var node = document.DocumentNode.SelectSingleNode("//input[@name='id' or @id='id' or @name='product_id' or @id='product_id' or @name='productid' or @id='productid']");
        var fromHidden = ReadInt(node?.GetAttributeValue("value", null));
        if (fromHidden.HasValue) return fromHidden;

        var form = document.DocumentNode.SelectSingleNode("//form[@id='editform' or @name='editform' or contains(@action,'tilladmin.php')]");
        var action = form?.GetAttributeValue("action", null);
        if (!string.IsNullOrWhiteSpace(action))
        {
            var m = Regex.Match(action, @"[?&]id=(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idFromAction)) return idFromAction;
        }

        var html = document.DocumentNode.InnerHtml ?? string.Empty;
        var m2 = Regex.Match(html, @"[?&]id=(\d+)", RegexOptions.IgnoreCase);
        if (m2.Success && int.TryParse(m2.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idFromHtml)) return idFromHtml;

        return null;
    }

    private static string? ExtractProductName(HtmlDocument document)
    {
        var node = document.DocumentNode.SelectSingleNode("//div[@id='addproduct_dialog']//input[@id='name' or @name='name' or @id='productname' or @name='productname']");
        var val = node?.GetAttributeValue("value", null);
        if (!string.IsNullOrWhiteSpace(val)) return val.Trim();

        var header = document.DocumentNode.SelectSingleNode("//div[@id='addproduct_dialog']//h1|//div[@id='addproduct_dialog']//h2");
        var headerText = header?.InnerText?.Trim();
        if (!string.IsNullOrWhiteSpace(headerText)) return headerText;

        return null;
    }

    private static decimal? ReadPercent(HtmlNode? node)
    {
        var s = node?.GetAttributeValue("value", null);
        var d = ReadDecimal(s);
        return d.HasValue ? d.Value / 100m : null;
    }

    private static decimal? ReadVatRate(HtmlNode? vatRateSelect)
    {
        if (vatRateSelect == null) return null;
        var selected = vatRateSelect.SelectSingleNode(".//option[@selected]") ?? vatRateSelect.SelectSingleNode(".//option[1]");
        var val = selected?.GetAttributeValue("value", null);
        var rate = ReadDecimal(val);
        if (rate.HasValue) return rate.Value;
        var txt = selected?.InnerText?.Trim().TrimEnd('%');
        var pct = ReadDecimal(txt);
        return pct.HasValue ? pct.Value / 100m : null;
    }

    private static decimal? ReadDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace("£", "").Trim();
        if (decimal.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d)) return d;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("en-GB"), out d)) return d;
        return null;
    }

    private static int? ReadInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        return null;
    }
}