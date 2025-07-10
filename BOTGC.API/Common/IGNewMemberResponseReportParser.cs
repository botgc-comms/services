using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public class IGNewMemberResponseReportParser : IReportParser<NewMemberResponseDto>
    {
        private readonly ILogger<IGNewMemberResponseReportParser> _logger;

        public IGNewMemberResponseReportParser(ILogger<IGNewMemberResponseReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<NewMemberResponseDto>> ParseReport(HtmlDocument document)
        {
            var result = new NewMemberResponseDto();

            var match = Regex.Match(document.DocumentNode?.InnerHtml ?? "", @"memberid=(\d+)");
            if (match.Success)
            {
                string memberId = match.Groups[1].Value;
                _logger.LogInformation($"Found member id {memberId} in response from IG.");

                result.MemberId = int.Parse(memberId);
                return new List<NewMemberResponseDto> { result };
            }
            else
            {
                _logger.LogWarning("MemberId was not found found in response from IG.");
            }

            return new List<NewMemberResponseDto> { result };
        }
    }
}
