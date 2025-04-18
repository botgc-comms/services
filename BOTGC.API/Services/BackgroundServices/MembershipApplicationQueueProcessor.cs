using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using System.Text.Json;

namespace BOTGC.API.Services.BackgroundServices;

public class MembershipApplicationQueueProcessor
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<MembershipApplicationQueueProcessor> _logger;
    private readonly IDataService _reportService;

    public MembershipApplicationQueueProcessor(AppSettings appSettings,
                                               ILogger<MembershipApplicationQueueProcessor> logger,
                                               IDataService reportService)
    {
        _logger = logger;

        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));

        var connectionString = appSettings.Queue?.ConnectionString;
        var queueName = AppConstants.MembershipApplicationQueueName;

        _queueClient = new QueueClient(connectionString, queueName);
        _queueClient.CreateIfNotExists();
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 5, visibilityTimeout: TimeSpan.FromSeconds(30), cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    var newMember = JsonSerializer.Deserialize<NewMemberApplicationDto>(message.MessageText);

                    if (newMember != null)
                    {
                        try
                        {
                            var createdMemberId = await _reportService.SubmitNewMemberApplicationAsync(newMember);
                            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

                            _logger.LogInformation("Successfully added new member with ID {MemberId}.", createdMemberId);
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

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}