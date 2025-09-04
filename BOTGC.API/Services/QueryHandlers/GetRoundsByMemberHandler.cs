using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetRoundsByMemberHandler(IOptions<AppSettings> settings,
                                          IMediator mediator,
                                          ILogger<GetRoundsByMemberHandler> logger,
                                          IDataProvider dataProvider,
                                          IReportParser<RoundDto> reportParser) : QueryHandlerBase<GetRoundsByMemberIdQuery, List<RoundDto>>
    {
        private const string __CACHE_KEY = "Rounds_By_Member_{memberId}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetRoundsByMemberHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<RoundDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<RoundDto>> Handle(GetRoundsByMemberIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY.Replace("{memberId}", request.MemberId);

            var playerIdsQuery = new GetPlayerIdsByMemberQuery();
            var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);

            var playerLookupId = playerIdLookup.Where(id => id.MemberId.ToString() == request.MemberId).SingleOrDefault();

            if (playerLookupId == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {request.MemberId}");
                throw new KeyNotFoundException($"No player found for member ID {request.MemberId}");
            }

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MemberRoundsReportUrl.Replace("{playerId}", playerLookupId.PlayerId.ToString())}";
            var memberRounds = await _dataProvider.GetData<RoundDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetRoundLinks);

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date; // Start of day
                memberRounds = memberRounds.Where(r => r.DatePlayed >= from).ToList();
            }

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
                memberRounds = memberRounds.Where(r => r.DatePlayed <= to).ToList();
            }

            _logger.LogInformation($"Retrieved {memberRounds.Count()} rounds for member {request.MemberId}");

            return memberRounds;
        }
    }
}
