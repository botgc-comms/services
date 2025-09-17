using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetLadyMembersHandler(IOptions<AppSettings> settings,
                                      IMediator mediator,
                                      ILogger<GetLadyMembersHandler> logger,
                                      IDataProvider dataProvider,
                                      IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetLadyMembersQuery, List<MemberDto>>
    {
        private const string __CACHE_KEY = "Lady_Members";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetLadyMembersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDto>> Handle(GetLadyMembersQuery request, CancellationToken cancellationToken)
        {
            var playerIdsQuery = new GetPlayerIdsByMemberQuery();
            var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);

            var playerIdDictionary = playerIdLookup.ToDictionary(id => id.MemberId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LadyMembersReportUrl}";
            var members = await _dataProvider.GetData<MemberDto>(
                reportUrl,
                _reportParser,
                __CACHE_KEY,
                TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins),
                HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;

            var ladyMembers = members
                .Where(m => m.IsActive!.Value // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate!.Value >= now) // No past leave date
                    && !string.IsNullOrWhiteSpace(m.MembershipCategory)
                    && MembershipHelper.GetPrimaryCategory(m) == MembershipPrimaryCategories.PlayingMember
                    && m.Gender.ToLower() == "female")
               
                .ToList();

            foreach (var m in ladyMembers)
            {
                m.PlayerId = playerIdDictionary.TryGetValue(m.MemberNumber!.Value, out PlayerIdLookupDto? player)
                    ? player.PlayerId
                    : null;
            }

            _logger.LogInformation("Filtered {Count} lady members from {Total} total members.", ladyMembers.Count, members.Count);

            return ladyMembers;
        }
    }
}
