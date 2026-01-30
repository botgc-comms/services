using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetMembersWithoutHandicapsQuaryHandler(IOptions<AppSettings> settings,
                                                    ILogger<GetMembersWithoutHandicapsQuaryHandler> logger,
                                                    IDataProvider dataProvider,
                                                    IReportParser<MemberWithoutHandicapDto> reportParser) : QueryHandlerBase<GetMembersWithoutHandicapsQuery, List<MemberWithoutHandicapDto>>
{
    private const string __CACHE_KEY = "Members_Without_Handicaps";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetMembersWithoutHandicapsQuaryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<MemberWithoutHandicapDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<MemberWithoutHandicapDto>> Handle(GetMembersWithoutHandicapsQuery request, CancellationToken cancellationToken)
    {
        var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembersWithoutHandicapsReportUrl}";
        var members = await _dataProvider.GetData<MemberWithoutHandicapDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins));

        _logger.LogInformation($"Successfully retrieved {members.Count} without handicaps.");

        return members;
    }
}
