using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using HtmlAgilityPack;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetNewMembersHandler(IOptions<AppSettings> settings,
                                      IMediator mediator,
                                      ILogger<GetNewMembersHandler> logger,
                                      IDataProvider dataProvider,
                                      IReportParser<NewMemberLookupDto> reportParser) : QueryHandlerBase<GetNewMembersQuery, List<MemberDetailsDto>>
    {
        private const string __CACHE_KEY = "New_Members_Lookup";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetNewMembersHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<NewMemberLookupDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDetailsDto>> Handle(GetNewMembersQuery request, CancellationToken cancellationToken)
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.NewMemberLookupReportUrl}";
            var members = await _dataProvider.GetData<NewMemberLookupDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetNewMemberLookupLinks);

            var now = DateTime.UtcNow;

            var cutoff = DateTime.Now.Date.AddDays(-14);
            var today = DateTime.Now.Date;
            var newMembersLookup = members
                .Where(m => m.JoinDate.HasValue && m.JoinDate.Value.Date >= cutoff && m.JoinDate.Value.Date <= today)
                .Where(m => m.PlayerId != 0)
                .ToList();

            if (newMembersLookup.Count == 0)
            {
                _logger.LogInformation("No new members found.");
                return new List<MemberDetailsDto>();
            }   

            var newMemberQueries = newMembersLookup
                  .Select(r => new GetMemberQuery
                  {
                      PlayerId = r.PlayerId
                  })
                  .ToList();

            var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle);

            var newMemberTasks = newMemberQueries.Select(async query =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await _mediator.Send(query, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "New member query failed: {QueryType} {@Query}", query.GetType().Name, query);
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            List<MemberDetailsDto> newMembers;
            try
            {
                newMembers = (await Task.WhenAll(newMemberTasks)).Where(x => x != null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "One or more new member queries failed during Task.WhenAll.");
                throw;
            }

            newMembers = newMembers.Select(nm =>
            {
                var member = members?.FirstOrDefault(m => m.PlayerId == nm.ID);
                if (member != null)
                {
                    nm.Forename = member.Forename;
                    nm.Surname = member.Surname;
                }

                return nm;
            }).ToList();

            _logger.LogInformation("{Count} members were found.", newMembers.Count);


            return newMembers;
        }
    }
}
