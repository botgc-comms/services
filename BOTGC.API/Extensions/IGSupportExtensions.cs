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
using BOTGC.API.Services.Queries;
using BOTGC.API.Models;
using BOTGC.API.Services.Behaviours;
using MediatR;
using static BOTGC.API.Models.BottleCalibrationEntity;

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

            services.AddAzureTableStore<BottleCalibrationEntity>("BottleCalibration");
            services.AddAzureTableStore<CompetitionPayoutHeaderEntity>("CompetitionPayoutHeader");
            services.AddAzureTableStore<CompetitionPayoutWinnerEntity>("CompetitionPayoutWinner");


            services.AddSingleton(cookieContainer);
            services.AddSingleton(httpClient);
            services.AddSingleton<IGSessionService>(); 
            services.AddHostedService(provider => provider.GetRequiredService<IGSessionService>());

            services.AddSingleton<IDataProvider, IGDataProvider>();
            services.AddSingleton<IGLoginService>();

            services.AddIGReportParsers();

            services.AddSingleton<IQueueService<NewMemberApplicationDto>, MembershipApplicationQueueService>();
            services.AddSingleton<IQueueService<NewMemberApplicationResultDto>, NewMemberAddedQueueService>();
            services.AddSingleton<IQueueService<NewMemberPropertyUpdateDto>, MemberPropertyUpdateQueueService>();
            services.AddSingleton<IQueueService<WasteEntryCommandDto>, StockWastageQueueService>();
            services.AddSingleton<IQueueService<ProcessStockTakeCommand>, StockTakeQueueService>();
            services.AddSingleton<IQueueService<StockTakeCompletedCommand>, StockTakeCompletedQueueService>();

            services.AddTransient<JuniorEclecticCompetitionProcessor>();

            services.AddSingleton<ITeeTimeUsageTaskQueue, TeeTimeUsageTaskQueue>();
            services.AddSingleton<IStockAnalysisTaskQueue, StockLevelAnalysisTaskQueue>();
            services.AddSingleton<ICompetitionTaskQueue, CompetitionTaskQueue>();
            services.AddSingleton<IPrizeConfigProvider, CompetitionPrizeConfigProvider>();
            services.AddSingleton<ICompetitionPayoutCalculator, CompetitionPayoutCalculator>(); 

            services.AddSingleton<ICompetitionProcessorResolver, CompetitionProcessorResolver>();

            services.AddSingleton<IMemberApplicationFormPdfGeneratorService, QuestPDFMemberApplicationFormGenerator>();

            services.AddSingleton<ITaskBoardService, MondayTaskBoardService>();
            services.AddSingleton<IBottleVolumeService, BottleVolumeService>();
            services.AddSingleton<IBottleWeightDataSource, BottleWeightService>();
            services.AddSingleton<ICompetitionPayoutStore, CompetitionPayoutService>();

            services.AddHostedService<CompetitionBackgroundService>();
            services.AddHostedService<TeeTimeUsageBackgroundService>();
            services.AddHostedService<MembershipApplicationQueueProcessor>();
            services.AddHostedService<MemberPropertyUpdatesQueueProcessor>();
            services.AddHostedService<NewMemberAddedQueueProcessor>();
            services.AddHostedService<StockLevelAnalysisQueueProcessor>();
            services.AddHostedService<StockTakeQueueProcessor>();
            services.AddHostedService<StockTakeCompletedQueueProcessor>();
            services.AddHostedService<StockWastageQueueProcessor>();
            services.AddHostedService<WasteSheetDailyFlusher>();
            services.AddHostedService<StockTakeDailyFlusher>();
            services.AddHostedService<StockLevelEnqueueScheduler>();
            
            // Register MediatR and scan for handlers in your assembly
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(Program).Assembly
                );
            });

            // Order matters.
            services.AddTransient<IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>, CacheStockTakeSheetBehaviour>();
            services.AddTransient<IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>, EnrichBottleCalibrationBehaviour>();
            services.AddTransient<IPipelineBehavior<CreatePurchaseOrderFromDraftCommand, bool>, GetStockItemsAndTradeUnitsBehaviour>();

            return services;
        }
    }

    public static class RegisterReportParsersExtensions
    {
        public static IServiceCollection AddIGReportParsers(this IServiceCollection services)
        {
            services.AddSingleton<IGLoginService>();

            services.Scan(s => s
                .FromAssemblies(typeof(RegisterReportParsersExtensions).Assembly)
                .AddClasses(c => c.AssignableTo(typeof(IReportParser<>)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
                .AddClasses(c => c.AssignableTo(typeof(IReportParserWithMetadata<,>)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
            );

            return services;
        }
    }
}
