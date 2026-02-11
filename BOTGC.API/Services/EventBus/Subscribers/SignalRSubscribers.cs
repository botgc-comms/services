using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

public sealed class JuniorProgressUpdateRealtimeSubscriber(
    ILogger<JuniorProgressUpdateRealtimeSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IHubContext<BOTGC.API.Hubs.EventsHub> hub)
    : SignalRSubscriberBase<LearningCompletedEvent, JuniorProgressUpdateRealtimeSubscriber>(logger, queues, options, json, hub)
{ }
