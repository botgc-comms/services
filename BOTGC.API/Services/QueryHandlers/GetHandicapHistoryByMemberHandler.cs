using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BOTGC.API.Services.QueryHandlers;

public class GetHandicapHistoryByMemberHandler(IOptions<AppSettings> settings,
                                               IMediator mediator,
                                               ILogger<GetHandicapHistoryByMemberHandler> logger,
                                               IDataProvider dataProvider,
                                               IReportParser<HandicapIndexPointDto> reportParser) : QueryHandlerBase<GetHandicapHistoryByMemberQuery, PlayerHandicapSummaryDto>
{
    private const string __CACHE_KEY = "Handicap_Hisotry_By_Member:{memberId}";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetHandicapHistoryByMemberHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<HandicapIndexPointDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<PlayerHandicapSummaryDto?> Handle(GetHandicapHistoryByMemberQuery request, CancellationToken cancellationToken)
    {
        PlayerHandicapSummaryDto result = null;

        try
        {
            var cacheKey = __CACHE_KEY.Replace("{memberId}", request.MemberId.ToString());

            var playerIdsQuery = new GetPlayerIdsByMemberQuery();
            var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);

            var playerLookupId = playerIdLookup.Where(id => id.MemberId == request.MemberId).SingleOrDefault();

            if (playerLookupId == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {request.MemberId}");
                throw new KeyNotFoundException($"No player found for member ID {request.MemberId}");
            }

            var playerDetailsQuery = new GetMemberQuery
            {
                PlayerId = playerLookupId.PlayerId
            };

            var playerDetails = await _mediator.Send(playerDetailsQuery, cancellationToken);

            if (playerDetails == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {request.MemberId}");
                throw new KeyNotFoundException($"No player found for member ID {request.MemberId}");
            }

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.HandicapIndexHistoryReportUrl.Replace("{playerId}", playerLookupId.PlayerId.ToString())}";

            var fromDate = request.FromDate ?? DateTime.Now.AddYears(-1);
            var toDate = request.ToDate ?? DateTime.Now;

            var data = new Dictionary<string, string>
            {
                { "action", "graph" },
                { "graph_type", "handicap_index_history" },
                { "playerid",playerLookupId.PlayerId.ToString(CultureInfo.InvariantCulture) },
                { "start_date", "" },
                { "end_date", "" },
                { "period", "all" }
            };

            var points = await _dataProvider.PostData<HandicapIndexPointDto>(
                reportUrl,
                data,
                _reportParser,
                cacheKey,
                TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins)
            );

            if (points != null && points.Any())
            {
                _logger.LogInformation($"Retrieved {points.Count()} hanidcap indenx history entries for member {request.MemberId}");


                var handicapHistory = new List<HandicapIndexPointDto>(points);
                if (request.FromDate.HasValue)
                {
                    handicapHistory = handicapHistory.Where(p => p.Date >= request.FromDate.Value).ToList();
                }
                if (request.ToDate.HasValue)
                {
                    handicapHistory = handicapHistory.Where(p => p.Date <= request.ToDate.Value).ToList();
                }

                result = new PlayerHandicapSummaryDto
                {
                    MemberDetails = playerDetails, 

                    FirstHandicap = points.OrderBy(p => p.Date).First().Index,
                    CurrentHandicap = points.OrderByDescending(p => p.Date).First().Index,
                    FirstHandicapDate = points.OrderBy(p => p.Date).First().Date,

                    Change1Year = points.OrderByDescending(p => p.Date).First().Index
                        - (points.Where(p => p.Date <= DateTime.Now.AddYears(-1))
                                .OrderByDescending(p => p.Date)
                                .FirstOrDefault()?.Index ?? points.OrderBy(p => p.Date).First().Index),

                    ChangeYTD = points.OrderByDescending(p => p.Date).First().Index
                        - (points.Where(p => p.Date <= new DateTime(DateTime.Now.Year, 1, 1))
                                .OrderByDescending(p => p.Date)
                                .FirstOrDefault()?.Index ?? points.OrderBy(p => p.Date).First().Index),

                    HandicapIndexPoints = handicapHistory.OrderByDescending(p => p.Date).ToList(),
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return result;
    }
}
