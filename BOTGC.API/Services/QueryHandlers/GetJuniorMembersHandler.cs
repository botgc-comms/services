using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetJuniorMembersHandler(IOptions<AppSettings> settings,
                                     IMediator mediator,
                                     ILogger<GetJuniorMembersHandler> logger,
                                     IDataProvider dataProvider,
                                     IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetJuniorMembersQuery, List<MemberDto>>
{
    private const string __CACHE_KEY = "Junior_Members";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetJuniorMembersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<MemberDto>> Handle(GetJuniorMembersQuery request, CancellationToken cancellationToken)
    {
        var playerIdsQuery = new GetPlayerIdsByMemberQuery();
        var playerIdLookup = await _mediator.Send(playerIdsQuery, cancellationToken);   

        var playerIdDictionary = playerIdLookup.ToDictionary(id => id.MemberId);

        var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.JuniorMembershipReportUrl}";
        var members = await _dataProvider.GetData<MemberDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

        var now = DateTime.UtcNow;
        var cutoffDate = new DateTime(now.Year, 1, 1).AddYears(-18); // 1st January of the current year - 18 years

        var juniorMembers = members
            .Where(m => m.IsActive!.Value // Active members only
                && (!m.LeaveDate.HasValue || m.LeaveDate!.Value >= now) // No past leave date
                && m.DateOfBirth.HasValue && m.DateOfBirth.Value >= cutoffDate) // Was 18 or younger on 1st Jan
            .ToList();

        foreach (var m in juniorMembers)
        {
            m.PlayerId = playerIdDictionary.TryGetValue(m.MemberNumber!.Value, out PlayerIdLookupDto? player) ? player.PlayerId : null;
        }

        _logger.LogInformation("Filtered {Count} junior members from {Total} total members.", juniorMembers.Count, members.Count);

        return juniorMembers;
    }
}
