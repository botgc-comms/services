using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;

namespace BOTGC.API.Common
{
    public class IGHandicapIndexHistoryReportParser : IReportParser<HandicapIndexPointDto>
    {
        private readonly ILogger<IGHandicapIndexHistoryReportParser> _logger;

        public IGHandicapIndexHistoryReportParser(ILogger<IGHandicapIndexHistoryReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses the Highcharts handicap index history from an HTML document.
        /// Extracts points of the form [epochMs, index] from the 'Handicap Index' series.
        /// </summary>
        public async Task<List<HandicapIndexPointDto>> ParseReport(HtmlDocument document)
        {
            var points = new List<HandicapIndexPointDto>();

            var html = document?.DocumentNode?.OuterHtml ?? string.Empty;
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("Empty HTML received for handicap index parsing.");
                return points;
            }

            try
            {
                var arrayText = FindHandicapSeriesArray(html) ?? FindFirstDataArray(html);

                if (string.IsNullOrWhiteSpace(arrayText))
                {
                    _logger.LogWarning("No handicap index data array found in the document.");
                    return points;
                }

                var rxPair = new Regex(@"\[\s*(\d{10,})\s*,\s*([0-9]+(?:\.[0-9]+)?)\s*\]", RegexOptions.Compiled);
                var matches = rxPair.Matches(arrayText);

                foreach (Match m in matches)
                {
                    var ms = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    var idx = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    var date = DateTimeOffset.FromUnixTimeMilliseconds(ms);

                    points.Add(new HandicapIndexPointDto
                    {
                        Date = date.LocalDateTime,
                        Index = idx
                    });
                }

                _logger.LogInformation("Successfully parsed {Count} handicap index points.", points.Count);
                return points;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse handicap index history from HTML.");
                return points;
            }
        }

        private static string? FindHandicapSeriesArray(string html)
        {
            var nameIdx = Regex.Match(html, @"name\s*:\s*['""]Handicap\s+Index['""]", RegexOptions.IgnoreCase);
            if (!nameIdx.Success) return null;

            var afterName = html.IndexOf("data", nameIdx.Index, StringComparison.OrdinalIgnoreCase);
            if (afterName < 0) return null;

            return ExtractBracketedArray(html, afterName);
        }

        private static string? FindFirstDataArray(string html)
        {
            var i = html.IndexOf("data", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            return ExtractBracketedArray(html, i);
        }

        private static string? ExtractBracketedArray(string text, int startSearchIndex)
        {
            var start = text.IndexOf('[', startSearchIndex);
            if (start < 0) return null;

            var depth = 0;
            for (int p = start; p < text.Length; p++)
            {
                var ch = text[p];
                if (ch == '[') depth++;
                else if (ch == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, p - start + 1);
                    }
                }
            }

            return null;
        }
    }
}

