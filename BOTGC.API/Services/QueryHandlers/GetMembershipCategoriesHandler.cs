using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMembershipCategoriesHandler(IOptions<AppSettings> settings,
                                                ILogger<GetMembershipCategoriesHandler> logger,
                                                IServiceScopeFactory serviceScopeFactory,
                                                ITaskBoardService taskBoardService) : QueryHandlerBase<GetMembershipCategoriesQuery, List<MembershipCategoryGroupDto>>
    {
        private const string __CACHE_KEY = "Member_Categories";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMembershipCategoriesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        private readonly ITaskBoardService _taskBoardService = taskBoardService ?? throw new ArgumentNullException(nameof(taskBoardService));

        public async override Task<List<MembershipCategoryGroupDto>> Handle(GetMembershipCategoriesQuery request, CancellationToken cancellationToken)
        {
            ICacheService? cacheService = null;

            using var scope = _serviceScopeFactory.CreateScope();
            cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cachedResults = await cacheService!.GetAsync<List<MembershipCategoryGroupDto>>(__CACHE_KEY).ConfigureAwait(false);
            if (cachedResults != null && cachedResults.Any())
            {
                _logger.LogInformation("Retrieving results from cache for Membership Categories...");
                return cachedResults;
            }

            var result = await this._taskBoardService.GetMembershipCategories();

            await cacheService.SetAsync(__CACHE_KEY!, result, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins)).ConfigureAwait(false);

            return result;
        }
    }
}
