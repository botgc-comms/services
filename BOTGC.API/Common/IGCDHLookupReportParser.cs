using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common
{
    public class IGCDHLookupReportParser : IReportParser<MemberCDHLookupDto>
    {
        private readonly ILogger<IGCDHLookupReportParser> _logger;

        public IGCDHLookupReportParser(ILogger<IGCDHLookupReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MemberCDHLookupDto>> ParseReport(HtmlDocument document)
        {
            var result = new MemberCDHLookupDto();

            var hiddenInputs = document.DocumentNode.SelectNodes("//input[@type='hidden']");
            if (hiddenInputs == null)
            {
                _logger.LogWarning("No hidden input fields found in CDH lookup response.");
                return new List<MemberCDHLookupDto>();
            }

            foreach (var input in hiddenInputs)
            {
                var name = input.GetAttributeValue("name", "");
                var value = input.GetAttributeValue("value", "");

                switch (name)
                {
                    case "cdh_id":
                        result.CdhId = value;
                        break;
                    case "handicap_index":
                        result.HandicapIndex = value;
                        break;
                    case "whs_member_uid":
                        result.WhsMemberUid = value;
                        break;
                    case "clubName":
                        result.ClubName = value;
                        break;
                    case "clubUid":
                        result.ClubUid = value;
                        break;
                }
            }

            return new List<MemberCDHLookupDto> { result };
        }
    }
}
