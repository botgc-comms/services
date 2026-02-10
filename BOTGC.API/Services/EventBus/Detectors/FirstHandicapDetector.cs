using System.Globalization;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class FirstHandicapByNoHandicapReportDetectorState
{
    public bool SeenOnNoHandicapList { get; set; }
    public DateTime LastCheckedUtc { get; set; } = DateTime.UtcNow.AddDays(-14);
}

[DetectorName("first-handicap")]
[DetectorSchedule("0 22 * * *", runOnStartup: true)]
public sealed class FirstHandicapByNoHandicapReportDetector(
    IMediator mediator,
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IEventStore eventStore,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger<FirstHandicapByNoHandicapReportDetector> logger) 
        : MemberDetectorBase<FirstHandicapByNoHandicapReportDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    private readonly IEventStore _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));

    protected override async Task DetectAsync(MemberScope scopeKey, DetectorState<FirstHandicapByNoHandicapReportDetectorState> state, IDetectorEventSink sink, CancellationToken cancellationToken)
    {
        if (!scopeKey.PlayerId.HasValue)
        {
            Logger.LogWarning($"No player id was found in the scope key for detecting first handicap: memberId {scopeKey.MemberNumber}");
            return;
        }

        var playerId = scopeKey.PlayerId.Value;

        if (await _eventStore.ExistsAsync<FirstHandicapAwardedEvent>(playerId, cancellationToken))
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var lookbackFloorUtc = nowUtc.AddDays(-14);

        var fromUtc = state.Value.LastCheckedUtc < lookbackFloorUtc ? lookbackFloorUtc : state.Value.LastCheckedUtc;
        state.Value.LastCheckedUtc = nowUtc;

        var withoutHandicaps = await Mediator.Send(new GetMembersWithoutHandicapsQuery(), cancellationToken);
        var onNoHandicapListNow = withoutHandicaps.Any(m => m.PlayerId == playerId);

        if (!state.Value.SeenOnNoHandicapList)
        {
            if (onNoHandicapListNow)
            {
                state.Value.SeenOnNoHandicapList = true;
            }

            return;
        }

        if (onNoHandicapListNow)
        {
            return;
        }

        var member = await Mediator.Send(new GetHandicapHistoryByMemberQuery { MemberId = playerId }, cancellationToken);
        if (member is null) return;

        var handicapValue = member.CurrentHandicap;
        
        if (await _eventStore.ExistsAsync< FirstHandicapAwardedEvent>(playerId, cancellationToken))
        {
            return;
        }

        sink.Add(new FirstHandicapAwardedEvent {
            EventId = Guid.NewGuid(),
            MemberId = playerId,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CurrentHandicap = handicapValue
        });
    }
}
