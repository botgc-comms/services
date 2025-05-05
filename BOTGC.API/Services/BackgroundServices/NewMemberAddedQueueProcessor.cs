using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BOTGC.API.Services.BackgroundServices
{
    public class NewMemberAddedQueueProcessor : BackgroundService
    {
        private readonly ILogger<NewMemberAddedQueueProcessor> _logger;
        private readonly IDistributedLockManager _distributedLockManager;

        private readonly IMemberApplicationFormPdfGeneratorService _pdfGeneratorService;
        private readonly ITaskBoardService _taskBoardService;
        private readonly IQueueService<NewMemberApplicationResultDto> _newMemberAddedQueueService;

        public NewMemberAddedQueueProcessor(
            ILogger<NewMemberAddedQueueProcessor> logger,
            IDistributedLockManager distributedLockManager,
            IMemberApplicationFormPdfGeneratorService pdfGeneratorService,
            ITaskBoardService taskBoardService,
            IQueueService<NewMemberApplicationResultDto> newMemberAddedQueueService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));

            _pdfGeneratorService = pdfGeneratorService ?? throw new ArgumentNullException(nameof(pdfGeneratorService));
            _taskBoardService = taskBoardService ?? throw new ArgumentNullException(nameof(taskBoardService));
            _newMemberAddedQueueService = newMemberAddedQueueService ?? throw new ArgumentNullException(nameof(newMemberAddedQueueService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _newMemberAddedQueueService.ReceiveMessagesAsync(
                    maxMessages: 5,
                    cancellationToken: stoppingToken);

                foreach (var message in messages)
                {
                    var resultDto = message.Payload;

                    if (resultDto == null)
                    {
                        _logger.LogWarning("Failed to deserialize new member result message {MessageId}.", message.Message.MessageId);
                        await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    if (resultDto.ApplicationId == null)
                    {
                        _logger.LogWarning("New application message was missing required data", message.Message.MessageId);
                        await _newMemberAddedQueueService.DeadLetterEnqueueAsync(resultDto, message.Message.DequeueCount, DateTime.UtcNow, stoppingToken);
                        await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    await using var distributedLock = await _distributedLockManager.AcquireLockAsync(
                        $"Lock:NewMemberAdded:{resultDto.Application.ApplicationId}",
                        cancellationToken: stoppingToken);

                    if (!distributedLock.IsAcquired)
                    {
                        _logger.LogInformation(
                            "Lock not acquired for ApplicationId {ApplicationId}, skipping processing.",
                            resultDto.Application.ApplicationId);

                        continue;
                    }

                    try
                    {
                        if (message.Message.DequeueCount > maxAttempts)
                        {
                            await _newMemberAddedQueueService.DeadLetterEnqueueAsync(resultDto, message.Message.DequeueCount, DateTime.UtcNow, stoppingToken);
                            await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            // Generate the PDF
                            var pdfBytes = _pdfGeneratorService.GeneratePdf(resultDto.Application);

                            // Check for existing ticket
                            var taskItemId = await _taskBoardService.FindExistingApplicationItemIdAsync(resultDto.Application.ApplicationId);

                            if (taskItemId == null)
                            {
                                // Create the task item
                                taskItemId = await _taskBoardService.CreateMemberApplicationAsync(resultDto.Application);
                            }

                            if (taskItemId != null)
                            {
                                // Attach the PDF to the task
                                var fileName = $"MembershipApplication_{resultDto.Application.ApplicationId}.pdf";
                                await _taskBoardService.AttachFile(taskItemId, pdfBytes, fileName);

                                _logger.LogInformation("Successfully processed and attached PDF for ApplicationId {ApplicationId}.", resultDto.Application.ApplicationId);

                                // Remove the message from the queue
                                await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error processing new member result for ApplicationId {ApplicationId}.",
                                resultDto.Application.ApplicationId);

                            // Exponential backoff before retry
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.Message.DequeueCount)), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Unexpected error processing new member result for ApplicationId {ApplicationId}.",
                            resultDto.Application.ApplicationId);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
