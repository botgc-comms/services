using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;

namespace BOTGC.API.Common
{
    public class IGTillOperatorReportParser : IReportParser<TillOperatorDto>
    {
        private readonly ILogger<IGTillOperatorReportParser> _logger;

        public IGTillOperatorReportParser(ILogger<IGTillOperatorReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TillOperatorDto>> ParseReport(HtmlDocument document)
        {
            var operators = new List<TillOperatorDto>();
            var rows = document.DocumentNode.SelectNodes("//table")[0].SelectNodes("tr");

            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No operator rows found in the report.");
                return operators;
            }

            foreach (var row in rows)
            {
                try
                {
                    var nameCell = row.SelectSingleNode(".//td[1]");
                    var activeCell = row.SelectSingleNode(".//td[2]");
                    var editCell = row.SelectSingleNode(".//td[3]//a[contains(@class,'ajaxbutton') and @data-ajax-action='editoperator']");

                    var name = nameCell?.InnerText?.Trim() ?? string.Empty;
                    var isActive = activeCell?.SelectSingleNode(".//span[contains(@class,'fa-check')]") != null
                                   || activeCell?.InnerText?.IndexOf("check", StringComparison.OrdinalIgnoreCase) >= 0;

                    int id = 0;
                    var idAttr = editCell?.GetAttributeValue("data-ajax-data-inline-id", null);
                    if (!string.IsNullOrWhiteSpace(idAttr))
                    {
                        int.TryParse(idAttr, out id);
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        operators.Add(new TillOperatorDto
                        {
                            Id = id,
                            Name = name,
                            IsActive = isActive
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing operator row.");
                }
            }

            operators = operators
                .GroupBy(o => (o.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.Id).First())
                .ToList();

            _logger.LogInformation("Extracted {Count} till operators.", operators.Count);
            return await Task.FromResult(operators);
        }
    }
}
