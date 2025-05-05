using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BOTGC.API.Services.BackgroundServices
{
    public class MemberPropertyUpdatesQueueProcessor : BackgroundService
    {
        private readonly AppSettings _settings;
        
        private readonly QueueClient _queueClient;
        
        private readonly ILogger<MemberPropertyUpdatesQueueProcessor> _logger;
        private readonly IDataService _reportService;
        private readonly IQueueService<NewMemberPropertyUpdateDto> _memberPropertyUpdateQueueService;

        public MemberPropertyUpdatesQueueProcessor(IOptions<AppSettings> settings,
                                                   ILogger<MemberPropertyUpdatesQueueProcessor> logger,
                                                   IDataService reportService,
                                                   IQueueService<NewMemberPropertyUpdateDto> memberPropertyUpdateQueueService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _memberPropertyUpdateQueueService = memberPropertyUpdateQueueService ?? throw new ArgumentNullException(nameof(memberPropertyUpdateQueueService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;

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
                            await _memberPropertyUpdateQueueService.DeadLetterEnqueueAsync(propertyUpdate, message.Message.DequeueCount, DateTime.UtcNow, stoppingToken);
                            await _memberPropertyUpdateQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            await _reportService.SetMemberProperty(propertyUpdate.Property, propertyUpdate.MemberId, propertyUpdate.Value);
                            await _memberPropertyUpdateQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
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
