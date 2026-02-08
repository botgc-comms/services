using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetLatestJuniorProgressEventQueryHandler(
    IMemberEventWindowReader windowReader,
    JsonSerializerOptions json)
    : QueryHandlerBase<GetLatestJuniorProgressEventQuery, JuniorProgressChangedEvent?>
{
    private readonly IMemberEventWindowReader _windowReader = windowReader ?? throw new ArgumentNullException(nameof(windowReader));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    public override async Task<JuniorProgressChangedEvent?> Handle(GetLatestJuniorProgressEventQuery request, CancellationToken cancellationToken)
    {
        if (request.MemberId <= 0)
        {
            return null;
        }

        var take = request.Take;
        if (take <= 0) take = 250;
        if (take > 2000) take = 2000;

        var window = await _windowReader.GetLatestCategoryWindowAsync(request.MemberId, take, cancellationToken);

        if (window.EventsNewestFirst.Count == 0)
        {
            return null;
        }

        var progressKey = EventTypeKey.For(typeof(JuniorProgressChangedEvent));

        foreach (var e in window.EventsNewestFirst)
        {
            if (!EventTypeMatcher.IsMatch(e.EventClrType, progressKey))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(e.PayloadJson))
            {
                continue;
            }

            try
            {
                var evt = JsonSerializer.Deserialize<JuniorProgressChangedEvent>(e.PayloadJson, _json);
                if (evt is not null)
                {
                    return evt;
                }
            }
            catch
            {
            }
        }

        return null;
    }
}
