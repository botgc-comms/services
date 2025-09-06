﻿using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMemberHandler(IOptions<AppSettings> settings,
                                  IMediator mediator,
                                  ILogger<GetMemberEventsHandler> logger,
                                  IDataProvider dataProvider,
                                  IReportParser<MemberDetailsDto> reportParser) : QueryHandlerBase<GetMemberQuery, MemberDetailsDto?>
    {
        private const string __CACHE_KEY = "Membership_Details_{memberid}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMemberEventsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDetailsDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        public async override Task<MemberDetailsDto?> Handle(GetMemberQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY.Replace("{memberid}", request.MemberNumber.ToString());

            var playerIdsQuery = new GetPlayerIdsByMemberQuery();
            var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);

            var playerLookupId = playerIdLookup.Where(id => id.MemberId == request.MemberNumber || id.PlayerId == request.MemberNumber).SingleOrDefault();

            if (playerLookupId == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {request.MemberNumber}");
                throw new KeyNotFoundException($"No player found for member ID {request.MemberNumber}");
            }

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MemberDetailsUrl}".Replace("{memberid}", request.MemberNumber.ToString());
            var response = await _dataProvider.GetData<MemberDetailsDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

            if (response.Any())
            {
                var member = response.First();
                member.ID = request.MemberNumber;

                member.Forename = playerLookupId.Forename;
                member.Surname = playerLookupId.Surname;

                _logger.LogInformation($"Successfully retrieved the member details for member ID {request.MemberNumber}.");
                return response.First();
            }
            else
            {
                _logger.LogWarning($"No member details found for member ID {request.MemberNumber}.");
                return null;
            }   
        }
    }
}
