﻿using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
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
            Exception? lastError = null;

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

                        await _newMemberAddedQueueService.DeadLetterEnqueueAsync(resultDto, message.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
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

                    if (message.Message.DequeueCount > maxAttempts)
                    {
                        await _newMemberAddedQueueService.DeadLetterEnqueueAsync(resultDto, message.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                        await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        // Generate the PDF
                        var pdfBytes = _pdfGeneratorService.GeneratePdf(resultDto.Application);

                        var taskItemId = await _taskBoardService.FindExistingApplicationItemIdAsync(resultDto.ApplicationId);

                        if (taskItemId == null)
                        {
                            taskItemId = await _taskBoardService.CreateMemberApplicationAsync(resultDto);
                        }

                        if (taskItemId != null)
                        {
                            var fileName = $"MembershipApplication_{resultDto.Application.ApplicationId}.pdf";
                            await _taskBoardService.AttachFile(taskItemId, pdfBytes, fileName);

                            _logger.LogInformation("Successfully processed and attached PDF for ApplicationId {ApplicationId}.", resultDto.Application.ApplicationId);

                            await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(ex, "Error processing ApplicationId {ApplicationId}.", resultDto.Application.ApplicationId);

                        if (message.Message.DequeueCount >= maxAttempts)
                        {
                            await _newMemberAddedQueueService.DeadLetterEnqueueAsync(resultDto, message.Message.DequeueCount, DateTime.UtcNow, ex, stoppingToken);
                            await _newMemberAddedQueueService.DeleteMessageAsync(message.Message.MessageId, message.Message.PopReceipt, stoppingToken);
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, message.Message.DequeueCount)), stoppingToken);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
