using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetWaitingMembersHandler(IOptions<AppSettings> settings,
                                          IMediator mediator,
                                          ILogger<GetWaitingMembersHandler> logger,
                                          IDataProvider dataProvider,
                                          IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetWaitingMembersQuery, List<MemberDto>>
    {
        private const string __CACHE_KEY = "Waiting_Members";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetWaitingMembersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDto>> Handle(GetWaitingMembersQuery request, CancellationToken cancellationToken)
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.AllWaitingMembersReportUrl}";
            var members = await _dataProvider.GetData<MemberDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;

            var waitingMembers = members
                .Where(m => !m.LeaveDate.HasValue || m.LeaveDate.Value >= now) 
                .ToList();

            _logger.LogInformation("Returning {Count} waiting members.", waitingMembers.Count);

            return waitingMembers;
        }
    }
}
