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
using BOTGC.API.Models;

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

            services.AddSingleton<IDataProvider, IGDataProvider>();

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
            services.AddSingleton<IReportParser<SubscriptionPaymentDto>, IGSubscriptionPaymentsReportParser>();
            services.AddSingleton<IReportParser<MemberDetailsDto>, IGMemberDetailsReportParser>();
            services.AddSingleton<IReportParser<HandicapIndexPointDto>, IGHandicapIndexHistoryReportParser>();
            services.AddSingleton<IReportParser<TillOperatorDto>, IGTillOperatorReportParser>();
            services.AddSingleton<IReportParser<NewMemberLookupDto>, IGNewMembersReportParser>();
            services.AddSingleton<IReportParser<StockTakeEntryDto>, IGStockTakeReportParser>();
            services.AddSingleton<IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto>, IGLeaderboardReportParser>();
            services.AddSingleton<IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>, IGClubChampionshipLeaderboardReportParser>();
            
            services.AddSingleton<IQueueService<NewMemberApplicationDto>, MembershipApplicationQueueService>();
            services.AddSingleton<IQueueService<NewMemberApplicationResultDto>, NewMemberAddedQueueService>();
            services.AddSingleton<IQueueService<NewMemberPropertyUpdateDto>, MemberPropertyUpdateQueueService>();
            services.AddSingleton<IQueueService<WasteEntryCommandDto>, StockWastageQueueService>();

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
            services.AddHostedService<StockWastageQueueProcessor>();
            services.AddHostedService<WasteSheetDailyFlusher>();
            services.AddHostedService<StockLevelEnqueueScheduler>();
            
            // Register MediatR and scan for handlers in your assembly
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(Program).Assembly
                );
            });

            return services;
        }
    }
}
