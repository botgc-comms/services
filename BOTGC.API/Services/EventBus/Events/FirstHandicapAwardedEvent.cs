using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("junior.firsthandicap.awarded", 1)]
public sealed class FirstHandicapAwardedEvent : IEvent
{
    public Guid EventId { get; set; }
    public int MemberId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public double CurrentHandicap { get; set; }
}
