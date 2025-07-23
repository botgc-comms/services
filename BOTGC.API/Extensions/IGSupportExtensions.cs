using BOTGC.API.Services;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.BackgroundServices;
using BOTGC.API.Services.CompetitionProcessors;
using System.Net;
using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using StackExchange.Redis;

namespace BOTGC.API.Extensions
{
    public static class IGSupportExtensions
    {
        public static IServiceCollection AddIGSupport(this IServiceCollection services)
        {
            var cookieContainer = new CookieContainer();

            var httpHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };

            var httpClient = new HttpClient(httpHandler);

            services.AddSingleton<IDataService, IGDataService>();

            services.AddSingleton(cookieContainer);
            services.AddSingleton(httpClient);
            services.AddSingleton<IGSessionService>(); 
            services.AddHostedService(provider => provider.GetRequiredService<IGSessionService>());

            services.AddSingleton<IGLoginService>();
            services.AddSingleton<IReportParser<MemberDto>, IGMemberReportParser>();
            services.AddSingleton<IReportParser<RoundDto>, IGRoundReportParser>();
            services.AddSingleton<IReportParser<PlayerIdLookupDto>, IGPlayerIdLookupReportParser>();
            services.AddSingleton<IReportParser<ScorecardDto>, IGScorecardReportParser>();
            services.AddSingleton<IReportParser<MemberEventDto>, IGMemberEventsReportParser>();
            services.AddSingleton<IReportParser<TeeSheetDto>, IGTeeSheetReportParser>();
            services.AddSingleton<IReportParser<CompetitionDto>, IGCompetitionReportParser>();
            services.AddSingleton<IReportParser<CompetitionSettingsDto>, IGCompetitionSettingsReportParser>();
            services.AddSingleton<IReportParser<CompetitionSummaryDto>, IGCompetitionSummaryReportParser>();
            services.AddSingleton<IReportParser<SecurityLogEntryDto>, IGSecurityLogReportParser>();
            services.AddSingleton<IReportParser<MemberCDHLookupDto>, IGCDHLookupReportParser>();
            services.AddSingleton<IReportParser<NewMemberResponseDto>, IGNewMemberResponseReportParser>();
            services.AddSingleton<IReportParser<StockItemDto>, IGStockItemReportParser>();
            services.AddSingleton<IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto>, IGLeaderboardReportParser>();
            services.AddSingleton<IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>, IGClubChampionshipLeaderboardReportParser>();
            
            services.AddSingleton<IQueueService<NewMemberApplicationDto>, MembershipApplicationQueueService>();
            services.AddSingleton<IQueueService<NewMemberApplicationResultDto>, NewMemberAddedQueueService>();
            services.AddSingleton<IQueueService<NewMemberPropertyUpdateDto>, MemberPropertyUpdateQueueService>();

            services.AddTransient<JuniorEclecticCompetitionProcessor>();

            services.AddSingleton<ITeeTimeUsageTaskQueue, TeeTimeUsageTaskQueue>();
            services.AddSingleton<IStockAnalysisTaskQueue, StockLevelAnalysisTaskQueue>();
            services.AddSingleton<ICompetitionTaskQueue, CompetitionTaskQueue>();
            
            services.AddSingleton<ICompetitionProcessorResolver, CompetitionProcessorResolver>();

            services.AddSingleton<IMemberApplicationFormPdfGeneratorService, QuestPDFMemberApplicationFormGenerator>();

            services.AddSingleton<ITaskBoardService, MondayTaskBoardService>();

            services.AddHostedService<CompetitionBackgroundService>();
            services.AddHostedService<TeeTimeUsageBackgroundService>();
            services.AddHostedService<MembershipApplicationQueueProcessor>();
            services.AddHostedService<MemberPropertyUpdatesQueueProcessor>();
            services.AddHostedService<NewMemberAddedQueueProcessor>();
            services.AddHostedService<StockLevelAnalysisQueueProcessor>();

            return services;
        }
    }
}
