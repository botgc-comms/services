using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class CreateStockItemHandler(
    IOptions<AppSettings> settings,
    ILogger<CreatePurchaseOrderHandler> logger,
    IDataProvider dataProvider
) : QueryHandlerBase<CreateStockItemCommand, int?>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<CreatePurchaseOrderHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    private const string __LOOKUP_CACHE_KEY = "Stock_Item_Lookups";

    public async override Task<int?> Handle(CreateStockItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CreateStockDialogUrl}";

            var lookupData = await _dataProvider.GetData(url, __LOOKUP_CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            var baseUnits = GetBaseUnits(lookupData);
            var divisions = GetDivisions(lookupData);   

            var baseUnit = baseUnits.TryGetValue(request.BaseUnit, out var buId) ? buId : 0;
            var division = divisions.TryGetValue(request.Division, out var divId) ? divId : 0;  

            if (baseUnit == 0)
            {
                _logger.LogError("Base unit '{BaseUnit}' not found in lookup data.", request.BaseUnit);
                return null;
            }

            if (division == 0)
            {
                _logger.LogError("Division '{Division}' not found in lookup data.", request.Division);
                return null;
            }   

            var data = new Dictionary<string, string>
            {
                { "name", request.Name },
                { "base_unit_id", baseUnit.ToString(CultureInfo.InvariantCulture) },
                { "division_id", division.ToString(CultureInfo.InvariantCulture) },
                { "min_alert", request.MinAlert.HasValue ? request.MinAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty },
                { "max_alert", request.MaxAlert.HasValue ? request.MaxAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty },
                { "active", "1" }
            };

            int? createdId = null;

            var saveUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CreateStockItemUrl}";
            var postResultJson = await _dataProvider.PostData(saveUrl, data);

            var redirectUrl = TryExtractRedirectUrl(postResultJson, _settings.IG.BaseUrl);
            if (redirectUrl == null)
            {
                _logger.LogError("Failed to extract redirect URL from response: {Response}", postResultJson);
                return null;
            }

            if (request.TradeUnits != null && request.TradeUnits.Count > 0)
            {
                var idMatch = Regex.Match(redirectUrl, @"(?:^|[?&])id=(\d+)(?:&|$)", RegexOptions.IgnoreCase);
                if (!idMatch.Success || !int.TryParse(idMatch.Groups[1].Value, out var parseId))
                {
                    _logger.LogError("Could not find item id in redirect URL: {RedirectUrl}", redirectUrl);
                    return null;
                }

                createdId = parseId;

                var updateData = new Dictionary<string, string>
                {
                    { "id", createdId.ToString() },
                    { "name", request.Name },
                    { "base_unit_id", baseUnit.ToString(CultureInfo.InvariantCulture) },
                    { "division_id", division.ToString(CultureInfo.InvariantCulture) },
                    { "min_alert", request.MinAlert.HasValue ? request.MinAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty },
                    { "max_alert", request.MaxAlert.HasValue ? request.MaxAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty },
                    { "active", "1" }
                };

                for (var i = 0; i < request.TradeUnits.Count; i++)
                {
                    var tu = request.TradeUnits[i];

                    updateData[$"ids[{i + 2}]"] = string.Empty;
                    updateData[$"names[{i + 2}]"] = tu.UnitName ?? string.Empty;
                    updateData[$"costs[{i + 2}]"] = tu.Cost.ToString("0.##", CultureInfo.InvariantCulture);
                    updateData[$"conversion_ratios[{i + 2}]"] = tu.ConversionRatio.HasValue ? tu.ConversionRatio.Value.ToString("0.########", CultureInfo.InvariantCulture) : "";
                    updateData[$"precision_of_units[{i + 2}]"] = tu.PrecisionOfUnits.HasValue ? tu.PrecisionOfUnits.Value.ToString(CultureInfo.InvariantCulture) : "";
                }

                var updateUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpdateStockItemUrl.Replace("{id}", createdId.ToString())}&requestType=ajax&ajaxaction=saveItem";
                var tradeUnitPostResult = await _dataProvider.PostData(updateUrl, updateData);

                _logger.LogInformation("Posted {Count} trade units for item {ItemId}. Response: {Response}", request.TradeUnits.Count, createdId, tradeUnitPostResult);
            }

            return createdId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create stock item '{ItemName}'.", request.Name); 
            return null;
        }
    }

    static string? TryExtractRedirectUrl(string json, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("actions", out var actions) || actions.ValueKind != JsonValueKind.Array) return null;

            foreach (var action in actions.EnumerateArray())
            {
                if (action.ValueKind != JsonValueKind.Object) continue;
                if (!action.TryGetProperty("type", out var typeEl)) continue;
                if (!"redirect".Equals(typeEl.GetString(), StringComparison.OrdinalIgnoreCase)) continue;

                if (!action.TryGetProperty("data", out var dataEl)) continue;
                var data = dataEl.GetString();
                if (string.IsNullOrWhiteSpace(data)) continue;

                var candidate = new Uri(data, UriKind.RelativeOrAbsolute);
                if (candidate.IsAbsoluteUri) return candidate.ToString();

                var baseUri = new Uri(baseUrl, UriKind.Absolute);
                var resolved = new Uri(baseUri, candidate);
                return resolved.ToString().TrimStart('/', '\\');
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static Dictionary<string, int> GetBaseUnits(string html)
    {
        return ExtractOptions(html, "base_unit_id");
    }

    static Dictionary<string, int> GetDivisions(string html)
    {
        return ExtractOptions(html, "division_id");
    }

    static Dictionary<string, int> ExtractOptions(string html, string selectId)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(html)) return result;

        var selectPattern = $@"<select[^>]*\bid\s*=\s*""{Regex.Escape(selectId)}""[^>]*>(.*?)</select>";
        var selectMatch = Regex.Match(html, selectPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success) return result;

        var inner = selectMatch.Groups[1].Value;
        var optionRegex = new Regex(@"<option\s+[^>]*value\s*=\s*""(?<val>\d+)""[^>]*>(?<text>.*?)</option>",
                                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match m in optionRegex.Matches(inner))
        {
            var text = WebUtility.HtmlDecode(Regex.Replace(m.Groups["text"].Value, @"\s+", " ").Trim());
            if (string.IsNullOrEmpty(text)) continue;
            if (text.Equals("Select a base unit", StringComparison.OrdinalIgnoreCase)) continue;
            if (text.Equals("Select a division", StringComparison.OrdinalIgnoreCase)) continue;

            var id = int.Parse(m.Groups["val"].Value, CultureInfo.InvariantCulture);
            result[text] = id;
        }

        return result;
    }
}





