using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.IGScrapers
{
    public sealed class IGMembersWithoutHandicapsReportParser : IReportParser<MemberWithoutHandicapDto>
    {
        private static readonly Regex PlayerIdRegex = new Regex(@"(?:\?|&)playerid=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ILogger<IGMembersWithoutHandicapsReportParser> _logger;

        public IGMembersWithoutHandicapsReportParser(ILogger<IGMembersWithoutHandicapsReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<List<MemberWithoutHandicapDto>> ParseReport(HtmlDocument document)
        {
            if (document is null) throw new ArgumentNullException(nameof(document));

            var members = new List<MemberWithoutHandicapDto>();

            var table = document.DocumentNode.SelectSingleNode("//table[contains(@class,'table')]");
            if (table is null)
            {
                _logger.LogWarning("Members-without-handicaps report: table not found.");
                return Task.FromResult(members);
            }

            var rows = table.SelectNodes(".//tr");
            if (rows is null || rows.Count == 0)
            {
                _logger.LogWarning("Members-without-handicaps report: no data rows found.");
                return Task.FromResult(members);
            }

            foreach (var row in rows)
            {
                try
                {
                    var playerLink = row.SelectSingleNode("./td[1]//a[contains(@href,'playeradmin.php') and contains(@href,'playerid=')]");
                    if (playerLink is null) continue;

                    var name = HtmlEntity.DeEntitize(playerLink.InnerText ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var href = playerLink.GetAttributeValue("href", string.Empty);
                    if (string.IsNullOrWhiteSpace(href)) continue;

                    var match = PlayerIdRegex.Match(href);
                    if (!match.Success) continue;

                    if (!int.TryParse(match.Groups[1].Value, out var playerId)) continue;

                    var member = new MemberWithoutHandicapDto
                    {
                        PlayerId = playerId,
                        PlayerName = name
                    };

                    members.Add(member);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Members-without-handicaps report: error processing row.");
                }
            }

            _logger.LogInformation("Members-without-handicaps report: parsed {Count} members.", members.Count);
            return Task.FromResult(members);
        }
    }
}
