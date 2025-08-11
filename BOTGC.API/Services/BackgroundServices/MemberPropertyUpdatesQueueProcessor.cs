using Azure.Storage.Queues;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;

namespace BOTGC.API.Services.BackgroundServices
{
    public class MemberPropertyUpdatesQueueProcessor(IOptions<AppSettings> settings,
                                                     ILogger<MemberPropertyUpdatesQueueProcessor> logger,
                                                     IMediator mediator,
                                                     IQueueService<NewMemberPropertyUpdateDto> memberPropertyUpdateQueueService) : BackgroundService
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        private readonly ILogger<MemberPropertyUpdatesQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IQueueService<NewMemberPropertyUpdateDto> _memberPropertyUpdateQueueService = memberPropertyUpdateQueueService ?? throw new ArgumentNullException(nameof(memberPropertyUpdateQueueService));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;
            Exception lastError = default;

            if (!_settings.FeatureToggles.ProcessMembershipApplications)
            {
                _logger.LogInformation("Membership application processing is disabled. Exiting background service.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _memberPropertyUpdateQueueService.ReceiveMessagesAsync(maxMessages: 5, cancellationToken: stoppingToken);

                foreach (var message in messages)
                {
                    var propertyUpdate = message.Payload;

                    if (propertyUpdate == null)
                    {
                        _logger.LogWarning("Failed to deserialize property update instructions for message {MessageId}.", message.Message.MessageId);
                        await _memberPropertyUpdateQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        if (message.Message.DequeueCount > maxAttempts)
                        {
                            await _memberPropertyUpdateQueueService.DeadLetterEnqueueAsync(propertyUpdate, message.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                            await _memberPropertyUpdateQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            var query = new SetMemberPropertiesQuery()
                            {
                                Property = propertyUpdate.Property,
                                MemberId = propertyUpdate.MemberId,
                                Value = propertyUpdate.Value
                            };

                            await _mediator.Send(query, stoppingToken);

                            await _memberPropertyUpdateQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;

                            _logger.LogError(ex,
                                "Error updating property {Property} for member {MemberId}.",
                                propertyUpdate.Property.GetDisplayName(),
                                propertyUpdate.MemberId);

                            // Exponential backoff before retry
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.Message.DequeueCount)), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(ex,
                            "Unexpected error processing property update for member {MemberId}.",
                            propertyUpdate.MemberId);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
