using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.IGScrapers
{
    public class IGStockItemReportParser : IReportParser<StockItemDto>
    {
        private readonly ILogger<IGStockItemReportParser> _logger;

        public IGStockItemReportParser(ILogger<IGStockItemReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<StockItemDto>> ParseReport(HtmlDocument document)
        {
            var stockItems = new List<StockItemDto>();

            // Find the script block containing the JSON
            var scriptNode = document.DocumentNode.SelectSingleNode("//script[contains(.,'\"id\"') and contains(.,'\"name\"') and contains(.,'division')]");
            if (scriptNode == null)
            {
                _logger.LogWarning("No stock item JSON data found in the document.");
                return stockItems;
            }

            // Extract JSON string from JavaScript
            var json = ExtractJsonFromScript(scriptNode.InnerText);
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("Failed to extract stock item JSON from script.");
                return stockItems;
            }

            try
            {
                using var docJson = JsonDocument.Parse(json);
                foreach (var element in docJson.RootElement.EnumerateArray())
                {
                    var dto = new StockItemDto
                    {
                        Id = GetInt(element, "id"),
                        Name = GetName(element, "name"),
                        ExternalId = GetExternalId(element, "name"),
                        MinAlert = GetNullableInt(element, "min_alert"),
                        MaxAlert = GetNullableInt(element, "max_alert"),
                        IsActive = GetNullableBool(element, "is_active"),
                        TillStockDivisionId = GetNullableInt(element, "till_stock_division_id"),
                        Unit = GetString(element, "unit"),
                        Quantity = GetNullableDecimal(element, "quantity"),
                        TillStockRoomId = GetNullableInt(element, "till_stock_room_id"),
                        Division = GetString(element, "division"),
                        Value = GetNullableDecimal(element, "value"),
                        TotalQuantity = GetNullableDecimal(element, "total_quantity"),
                        TotalValue = GetNullableDecimal(element, "total_value"),
                        OneQuantity = GetNullableDecimal(element, "1_quantity"),
                        OneValue = GetNullableDecimal(element, "1_value"),
                        TwoQuantity = GetNullableDecimal(element, "2_quantity"),
                        TwoValue = GetNullableDecimal(element, "2_value")
                    };
                    stockItems.Add(dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse stock item JSON.");
            }

            _logger.LogInformation("Successfully parsed {Count} stock items.", stockItems.Count);
            return stockItems;
        }

        private static string GetName(JsonElement e, string name)
        {
            var str = GetString(e, name);
            var strMatch = Regex.Match(str, "(^.*?)\\s*\\[[^\\]]*\\]\\s*$", RegexOptions.Singleline);
            if (strMatch.Success)
            {
                return strMatch.Groups[1].Value;
            }
            return str;
        }

        private static string GetExternalId(JsonElement e, string name)
        {
            var str = GetString(e, name);
            var strMatch = Regex.Match(str, "^.*?\\s*\\[\\s*([^\\]]*?)\\s*\\]\\s*$", RegexOptions.Singleline);
            if (strMatch.Success)
            {
                return strMatch.Groups[1].Value;
            }
            return null;
        }

        private static string ExtractJsonFromScript(string script)
        {
            // Attempt to find the actual JSON array in a script
            var match = Regex.Match(script, @"(\[\s*{.*?}\s*\])", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string GetString(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.GetString();
            return null;
        }

        private static int GetInt(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n))
                    return n;
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out n))
                    return n;
            }
            throw new InvalidOperationException($"Property {name} not found or not convertible to int.");
        }

        private static int? GetNullableInt(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null)
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n))
                    return n;
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out n))
                    return n;
            }
            return null;
        }

        private static decimal? GetNullableDecimal(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null)
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
                    return d;
                if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out d))
                    return d;
            }
            return null;
        }

        private static bool? GetNullableBool(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null)
            {
                if (v.ValueKind == JsonValueKind.True) return true;
                if (v.ValueKind == JsonValueKind.False) return false;
                if (v.ValueKind == JsonValueKind.String)
                {
                    var s = v.GetString();
                    if (s == "1" || s?.ToLower() == "true") return true;
                    if (s == "0" || s?.ToLower() == "false") return false;
                }
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n))
                    return n != 0;
            }
            return null;
        }
    }
}
