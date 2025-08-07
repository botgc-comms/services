using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices
{
    public class MembershipApplicationQueueProcessor : BackgroundService
    {
        private const string __CACHE_NEWMEMBERAPPLICATION = "NewMemberApplication_{applicationId}";

        private readonly AppSettings _settings;
        private readonly ILogger<MembershipApplicationQueueProcessor> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IQueueService<NewMemberApplicationResultDto> _membershipApplicationQueueService;
        private readonly IQueueService<NewMemberPropertyUpdateDto> _memberPropertyUpdateQueueService;
        private readonly IQueueService<NewMemberApplicationDto> _newMemberApplicationQueueService;

        private readonly IDistributedLockManager _distributedLockManager;

        public MembershipApplicationQueueProcessor(
            IOptions<AppSettings> settings,
            ILogger<MembershipApplicationQueueProcessor> logger,
            IServiceScopeFactory serviceScopeFactory,
            IDistributedLockManager distributedLockManager,
            IQueueService<NewMemberApplicationDto> newMemberApplicationQueueService,
            IQueueService<NewMemberApplicationResultDto> membershipApplicationQueueService,
            IQueueService<NewMemberPropertyUpdateDto> memberPropertyUpdateQueueService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));

            _newMemberApplicationQueueService = newMemberApplicationQueueService ?? throw new ArgumentNullException(nameof(newMemberApplicationQueueService));
            _membershipApplicationQueueService = membershipApplicationQueueService ?? throw new ArgumentNullException(nameof(membershipApplicationQueueService));
            _memberPropertyUpdateQueueService = memberPropertyUpdateQueueService ?? throw new ArgumentNullException(nameof(memberPropertyUpdateQueueService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;
            Exception? lastError = null; 

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _newMemberApplicationQueueService.ReceiveMessagesAsync(maxMessages: 5, cancellationToken: stoppingToken);

                foreach (var message in messages)
                {
                    var newMember = message.Payload;

                    if (newMember == null)
                    {
                        _logger.LogWarning("Failed to deserialize new member application for message {MessageId}.", message.Message.MessageId);
                        await _newMemberApplicationQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    await using var distributedLock = await _distributedLockManager.AcquireLockAsync($"Lock:Application:{newMember.ApplicationId}", cancellationToken: stoppingToken);

                    if (!distributedLock.IsAcquired)
                    {
                        _logger.LogInformation(
                            "Lock not acquired for ApplicationId {ApplicationId}, skipping processing.",
                            newMember.ApplicationId);

                        continue;
                    }

                    try
                    {
                        var cacheKey = __CACHE_NEWMEMBERAPPLICATION.Replace("{applicationId}", newMember.ApplicationId);

                        using var scope = _serviceScopeFactory.CreateScope();
                        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        
                        var cachedResult = await cacheService.GetAsync<NewMemberApplicationResultDto>(cacheKey);
                        if (cachedResult != null)
                        {
                            _logger.LogInformation("Duplicate application detected for ApplicationId {ApplicationId}, skipping.", newMember.ApplicationId);
                            await _newMemberApplicationQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        if (message.Message.DequeueCount > maxAttempts)
                        {
                            var failureResult = new NewMemberApplicationResultDto
                            {
                                ApplicationId = newMember.ApplicationId,
                                Application = newMember 
                            };

                            await _membershipApplicationQueueService.EnqueueAsync(failureResult, stoppingToken);
                            await _newMemberApplicationQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        NewMemberApplicationResultDto? memberCreated = null;

                        try
                        {
                            var query = new SubmitNewMemberApplicactionQuery() { Application = newMember };
                            memberCreated = await mediator.Send(query, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                            _logger.LogError(ex, "Error submitting new member application for ApplicationId {ApplicationId}.", newMember.ApplicationId);
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.Message.DequeueCount)), stoppingToken);
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
                                await _membershipApplicationQueueService.EnqueueAsync(memberCreated, stoppingToken);

                                if (!string.IsNullOrEmpty(memberCreated.ApplicationId) && memberCreated.MemberId.HasValue)
                                {
                                    await _memberPropertyUpdateQueueService.EnqueueAsync(new NewMemberPropertyUpdateDto
                                    {
                                        Property = MemberProperties.APPLICATIONID,
                                        MemberId = memberCreated.MemberId.Value,
                                        Value = memberCreated.ApplicationId
                                    }, stoppingToken);
                                }

                                if (!string.IsNullOrEmpty(memberCreated.ApplicationId) && memberCreated.MemberId.HasValue && memberCreated.Application != null && memberCreated.Application?.ReferrerId != null)
                                {
                                    await _memberPropertyUpdateQueueService.EnqueueAsync(new NewMemberPropertyUpdateDto
                                    {
                                        Property = MemberProperties.REFERRERID,
                                        MemberId = memberCreated.MemberId.Value,
                                        Value = memberCreated.Application.ReferrerId
                                    }, stoppingToken);
                                }

                                await _newMemberApplicationQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            }

                            _logger.LogInformation("Successfully added new member with ID {MemberId}.", memberCreated.MemberId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding new member.");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
