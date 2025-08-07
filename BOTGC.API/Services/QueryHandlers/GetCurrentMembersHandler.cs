using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCurrentMembersHandler(IOptions<AppSettings> settings,
                                          IMediator mediator,
                                          ILogger<GetCurrentMembersHandler> logger,
                                          IDataProvider dataProvider,
                                          IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetCurrentMembersQuery, List<MemberDto>>
    {
        private const string __CACHE_KEY = "Current_Members";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetCurrentMembersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDto>> Handle(GetCurrentMembersQuery request, CancellationToken cancellationToken)
        {
            var playerIdsQuery = new GetPlayerIdsByMemberQuery();
            var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);   

            var playerIdDictionary = playerIdLookup.ToDictionary(id => id.MemberId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.AllCurrentMembersReportUrl}";
            var members = await _dataProvider.GetData<MemberDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;

            var currentMembers = members
                .Where(m => m.IsActive!.Value // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate.Value >= now)) // No past leave date
                .ToList();

            foreach (var m in currentMembers)
            {
                m.PlayerId = playerIdDictionary.TryGetValue(m.MemberNumber!.Value, out PlayerIdLookupDto? player) ? player.PlayerId : null;
            }

            _logger.LogInformation("Filtered {Count} members from {Total} total members.", currentMembers.Count, members.Count);

            return currentMembers;
        }
    }
}
