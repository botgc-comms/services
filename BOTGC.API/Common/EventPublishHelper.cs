using BOTGC.API.Interfaces;

namespace BOTGC.API.Common;

public sealed class EventPublishHelper(IEventPublisher publisher) : IEventPublishHelper
{
    private readonly IEventPublisher _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));

    public Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : class, IEvent
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        return _publisher.PublishAsync(evt, cancellationToken);
    }
}
