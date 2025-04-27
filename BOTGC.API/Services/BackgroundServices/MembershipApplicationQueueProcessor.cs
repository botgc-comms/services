using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BOTGC.API.Services.BackgroundServices
{
    public class MembershipApplicationQueueProcessor : BackgroundService
    {
        private const string __CACHE_NEWMEMBERAPPLICATION = "NewMemberApplication_{applicationId}";

        private readonly AppSettings _settings;
        private readonly QueueClient _queueClient;
        private readonly ILogger<MembershipApplicationQueueProcessor> _logger;
        private readonly IDataService _reportService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IQueueService<NewMemberApplicationResultDto> _membershipApplicationQueueService;

        public MembershipApplicationQueueProcessor(IOptions<AppSettings> settings,
                                                   ILogger<MembershipApplicationQueueProcessor> logger,
                                                   IDataService reportService,
                                                   IServiceScopeFactory serviceScopeFactory,
                                                   IQueueService<NewMemberApplicationResultDto> membershipApplicationQueueService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _membershipApplicationQueueService = membershipApplicationQueueService ?? throw new ArgumentNullException(nameof(membershipApplicationQueueService));

            var connectionString = _settings.Queue?.ConnectionString;
            var queueName = AppConstants.MembershipApplicationQueueName;

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;

            while (!stoppingToken.IsCancellationRequested)
            {
                QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 5, visibilityTimeout: TimeSpan.FromSeconds(30), stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var newMember = JsonSerializer.Deserialize<NewMemberApplicationDto>(message.MessageText);

                        if (newMember != null)
                        {
                            try
                            {
                                var cacheKey = __CACHE_NEWMEMBERAPPLICATION.Replace("{applicationId}", newMember.ApplicationId);

                                using var scope = _serviceScopeFactory.CreateScope();
                                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                                var reportService = scope.ServiceProvider.GetRequiredService<IDataService>();

                                var cachedResult = await cacheService.GetAsync<NewMemberApplicationResultDto>(cacheKey);
                                if (cachedResult != null)
                                {
                                    _logger.LogInformation("Duplicate application detected for ApplicationId {ApplicationId}, skipping.", newMember.ApplicationId);
                                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                                    continue;
                                }

                                if (message.DequeueCount > maxAttempts)
                                {
                                    var failureResult = new NewMemberApplicationResultDto
                                    {
                                        ApplicationId = newMember.ApplicationId,
                                        Application = newMember
                                    };

                                    await _membershipApplicationQueueService.EnqueueAsync(failureResult);
                                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                                    continue;
                                }

                                NewMemberApplicationResultDto? memberCreated = null;

                                try
                                {
                                    memberCreated = await reportService.SubmitNewMemberApplicationAsync(newMember);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error submitting new member application for ApplicationId {ApplicationId}.", newMember.ApplicationId);
                                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.DequeueCount)), stoppingToken);
                                    continue;
                                }

                                if (memberCreated != null)
                                {
                                    try
                                    {
                                        await cacheService.SetAsync(cacheKey, memberCreated, TimeSpan.FromMinutes(_settings.Cache.Forever_TTL_Mins));
                                    }
                                    finally
                                    {
                                        await _membershipApplicationQueueService.EnqueueAsync(memberCreated);
                                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                                    }

                                    _logger.LogInformation("Successfully added new member with ID {MemberId}.", memberCreated.MemberId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error adding new member.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("New member application data is null.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing membership application message.");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
