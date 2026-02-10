using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services;
using BOTGC.API.Services.BackgroundServices;
using BOTGC.API.Services.Behaviours;
using BOTGC.API.Services.CompetitionProcessors;
using BOTGC.API.Services.EventBus.Subscribers;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
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

            services.AddAzureTableStore<EposAccountEntity>("EPOSAccounts");
            services.AddAzureTableStore<EposProductEntity>("EPOSProducts");
            services.AddAzureTableStore<EposVoucherEntity>("EPOSVouchers");
            services.AddAzureTableStore<EposVoucherCodeIndexEntity>("EPOSVoucherCodes");
            services.AddAzureTableStore<EposProShopInvoiceEntity>("EPOSVoucherInvoices");
            services.AddAzureTableStore<EposProShopInvoiceLineEntity>("EPOSVoucherInvoiceLines");
            services.AddAzureTableStore<EposAccountTransactionEntity>("EPOSAccountTransactions");

            services.AddAzureTableStore<LearningPackProgressEntity>("LearningPackProgress");
            services.AddAzureTableStore<LearningPackCatalogueEntity>("LearningPackCatalogue");

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
            services.AddSingleton<IQueueService<ProcessPrizeInvoiceCommand>, PrizeInvoiceQueueService>();
            services.AddSingleton<IQueueService<SendPrizeNotificationEmailCommand>, PrizeNotificationsQueueService>(); 
            services.AddSingleton<IQueueService<ProcessCompetitionWinningsBatchCompletedCommand>, NewCompetitionPrizesCalcualtedQueueService>();
            
            services.AddSingleton<IEposStore, EposStore>();

            services.AddTransient<JuniorEclecticCompetitionProcessor>();

            services.AddSingleton<ITeeTimeUsageTaskQueue, TeeTimeUsageTaskQueue>();
            services.AddSingleton<IStockAnalysisTaskQueue, StockLevelAnalysisTaskQueue>();
            services.AddSingleton<ICompetitionTaskQueue, CompetitionTaskQueue>();
            services.AddSingleton<IPrizeConfigProvider, CompetitionPrizeConfigProvider>();
            services.AddSingleton<ICompetitionPayoutCalculator, CompetitionPayoutCalculator>();
            services.AddSingleton<ICmsEncodingHelper, CmsEncodingHelper>();

            services.AddSingleton<ICompetitionProcessorResolver, CompetitionProcessorResolver>();

            services.AddSingleton<IMemberApplicationFormPdfGeneratorService, QuestPDFMemberApplicationFormGenerator>();
            services.AddSingleton<ICompetitionPrizeInvoicePdfGeneratorService, QuestPDFCompetitionPrizeInvoiceGenerator>();

            services.AddSingleton<ITaskBoardService, MondayTaskBoardService>();
            services.AddSingleton<IBottleVolumeService, BottleVolumeService>();
            services.AddSingleton<IBottleWeightDataSource, BottleWeightService>();
            services.AddSingleton<ICompetitionPayoutStore, CompetitionPayoutService>();
            services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
            services.AddSingleton<IBenefitsQrTokenService, BenefitsQrTokenService>();
            
            services.AddSingleton<ILearningPackProgressReadStore, TableStorageLearningPackProgressReadStore>();
            services.AddSingleton<ILearningPackRequirementResolver, TableStorageLearningPackRequirementResolver>();

            services.AddHostedService<EposProductSeedService>();
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
            services.AddHostedService<CompetitionResultsPageUpdateQueueProcessor>();
            services.AddHostedService<PrizeNotificationsQueueProcessor>();
            services.AddHostedService<PrizeInvoiceQueueProcessor>();

            // Register MediatR and scan for handlers in your assembly
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(Program).Assembly
                );
            });

            services.AddSingleton<IQueryCacheOptionsAccessor, QueryCacheOptionsAccessor>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryCacheOptionsBehaviour<,>));


            // Order matters.
            services.AddTransient<IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>, CacheStockTakeSheetBehaviour>();
            services.AddTransient<IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>, EnrichBottleCalibrationBehaviour>();
            services.AddTransient<IPipelineBehavior<CreatePurchaseOrderFromDraftCommand, bool>, GetStockItemsAndTradeUnitsBehaviour>();

            return services;
        }

        public static IServiceCollection RegisterEventDetectorsAndSubscribers(this IServiceCollection services, params Assembly[] scanAssemblies)
        {
            services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            services.AddAzureTableStore<DetectorStateEntity>("DetectorState");
            services.AddAzureTableStore<EventStreamEntity>("MemberEventStream");

            services.AddSingleton<IDetectorStateStore, TableDetectorStateStore>();
            services.AddSingleton<IMemberEventWindowReader, TableMemberEventWindowReader>();

            services.AddSingleton<IEventStore, TableEventStore>();
            services.AddSingleton<IEventPublisher, EventPublisher>();

            var assemblies = (scanAssemblies is { Length: > 0 } ? scanAssemblies : new[] { typeof(IGSupportExtensions).Assembly })
                .Append(typeof(IEvent).Assembly)
                .Distinct()
                .ToArray();

            services.AddSingleton<IEventTypeRegistry>(_ => new EventTypeRegistry(assemblies));

            services.AddSingleton<ISubscriberQueueFactory, SubscriberQueueFactory>();
            services.AddSingleton<IQueueService<EventEnvelope>, EventDispatchQueueService>();

            services.AddSingleton<IJuniorProgressEvaluator, JuniorProgressEvaluator>();
            services.AddSingleton<IJuniorProgressCategoryEvaluator, JuniorCadetProgressCategoryEvaluator>();
            services.AddSingleton<IJuniorProgressCategoryEvaluator, JuniorCourseCadetProgressCategoryEvaluator>();
            
            var allTypes = assemblies
                .SelectMany(a => a.DefinedTypes)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => t.AsType())
                .ToArray();

            var detectorTypes = allTypes
                .Where(t => typeof(IDetector).IsAssignableFrom(t))
                .ToArray();

            foreach (var detectorType in detectorTypes)
            {
                services.AddScoped(typeof(IDetector), detectorType);
            }

            services.AddSingleton<IDetectorNameRegistry>(_ => new DetectorNameRegistry(detectorTypes));

            var subscriberTypes = allTypes
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscriber<>)))
                .ToArray();

            foreach (var subscriberType in subscriberTypes)
            {
                services.AddSingleton(subscriberType);

                if (typeof(BackgroundService).IsAssignableFrom(subscriberType) || typeof(IHostedService).IsAssignableFrom(subscriberType))
                {
                    services.AddSingleton(typeof(IHostedService), sp => (IHostedService)sp.GetRequiredService(subscriberType));
                }
            }

            var pillarTypes = allTypes
                .Where(t => typeof(JuniorProgressPillarBase).IsAssignableFrom(t))
                .ToArray();

            foreach (var pillarType in pillarTypes)
            {
                services.AddSingleton(typeof(JuniorProgressPillarBase), pillarType);
            }

            services.AddSingleton<IJuniorCategoryProgressCalculator, JuniorCategoryProgressCalculator>();

            services.AddSingleton<ISubscriberCatalogue>(_ => new SubscriberCatalogue(subscriberTypes));

            services.AddSingleton<IMemberDetectorTriggerScheduler, MemberDetectorTriggerScheduler>();

            services.AddSingleton<IQueueService<DetectorTriggerCommand>, DetectorTriggerQueueService>();

            services.AddHostedService<EventDispatcher>();
            services.AddHostedService<DetectorSchedulerHostedService>();
            services.AddHostedService<DetectorTriggerQueueProcessor>();

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
